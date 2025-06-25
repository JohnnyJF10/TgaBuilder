using System.Buffers;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace THelperLib.UndoRedo
{
    public class UndoRedoManager : IUndoRedoManager
    {
        private enum State
        {
            Acting,
            Renting
        }

        public UndoRedoManager(long maxMemoryBytes = 10 * 1024 * 1024)
        {
            _byteArrayPool = ArrayPool<byte>.Shared;
            _maxMemoryBytes = maxMemoryBytes;
            _state = State.Acting;
        }

        private Stack<IUndoableAction> _undoStack = new();
        private Stack<IUndoableAction> _redoStack = new();

        private readonly ArrayPool<byte> _byteArrayPool;
        private readonly long _maxMemoryBytes;

        private State _state;
        private int _currentRentSize;
        private int _arraysToRent;
        private int _arraysRented;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public bool IsRenting => _state == State.Renting;

        public long MaxMemoryBytes => _maxMemoryBytes;
        public long CurrentMemoryBytes
        {
            get
            {
                long sum = 0;
                foreach (var action in _undoStack)
                    sum += action.SizeInBytes;
                foreach (var action in _redoStack)
                    sum += action.SizeInBytes;
                return sum;
            }
        }

        /// <summary>
        /// Starts the renting process for the specified size, if possible.
        /// </summary>
        /// <param name="totalSizeInBytes">Total size in bytes needed for the arrays.</param>
        /// <param name="arraysNeeded">Count of arrays needed (1 or 2).</param>
        public bool TryBeginRenting(int totalSizeInBytes, int arraysNeeded = 2)
        {
            if (arraysNeeded != 1 && arraysNeeded != 2)
                throw new ArgumentOutOfRangeException(nameof(arraysNeeded), "Only 1 or 2 arrays are supported.");

            totalSizeInBytes = NextHigherPowerOfTwo(totalSizeInBytes);

            if (_state == State.Renting)
                throw new InvalidOperationException("Already in renting state.");

            if (totalSizeInBytes > _maxMemoryBytes)
            {
                Debug.WriteLine($"Requested rent size {totalSizeInBytes} exceeds MaxMemoryBytes {_maxMemoryBytes}.");
                return false;
            }

            EnsureMemoryAvailable(totalSizeInBytes);

            _currentRentSize = totalSizeInBytes;
            _arraysToRent = arraysNeeded;
            _arraysRented = 0;
            _state = State.Renting;
            Debug.WriteLine($"Entering Renting mode with total size {totalSizeInBytes} bytes for {arraysNeeded} array(s).");
            return true;
        }

        /// <summary>
        /// Returns one array of the rented arrays. The caller needs to call several times to get all arrays.
        /// </summary>
        public byte[] RentUndoRedoArray()
        {
            if (_state != State.Renting)
                throw new InvalidOperationException("Not in renting mode. Call TryBeginRenting first.");

            if (_arraysRented >= _arraysToRent)
                throw new InvalidOperationException("All arrays have already been rented.");

            int size;
            if (_arraysToRent == 1)
                size = _currentRentSize;
            else
                size = _currentRentSize / 2;

            _arraysRented++;
            var array = _byteArrayPool.Rent(size);

            Debug.WriteLine($"Renting array {_arraysRented}/{_arraysToRent}, size {size} bytes.");

            if (_arraysRented == _arraysToRent)
            {
                _state = State.Acting;
                _currentRentSize = 0;
                _arraysToRent = 0;
                _arraysRented = 0;
                Debug.WriteLine("All arrays rented. Returning to Acting mode.");
            }

            return array;
        }

        public void PushBitmapEditAction(byte[] oldPixels, byte[] newPixels, Int32Rect region,
            Action<Int32Rect, byte[]> placingCallback)
        {
            if (_state != State.Acting)
                throw new InvalidOperationException("Can only push an edit action during acting mode.");

            var action = new BitmapEditAction(
                region: region,
                oldPixels: oldPixels,
                newPixels: newPixels,
                byteArrayPool: _byteArrayPool,
                placingCallback: placingCallback);
            Push(action);

            Debug.WriteLine("Pushed bitmap edit action.");
        }

        public void PushResizeSmallerAction(
            byte[] croppedPixels,
            int oldWidth, int newWidth,
            int oldHeight, int newHeight,
            Action<int, int> resizeSmallerCallback,
            Action<int, int, byte[]> reziseLargerCallback)
        {
            if (_state != State.Acting)
                throw new InvalidOperationException("Can only push an action during acting mode.");

            var action = new CropAction(
                croppedPixels: croppedPixels,
                oldWidth: oldWidth,
                oldHeight: oldHeight,
                newWidth: newWidth,
                newHeight: newHeight,
                byteArrayPool: _byteArrayPool,
                undoCallback: reziseLargerCallback,
                redoCallback: resizeSmallerCallback);

            Push(action);
            Debug.WriteLine("Pushed region resize smaller action.");
        }

        public void PushResizeLargerAction(
            int oldWidth, int newWidth,
            int oldHeight, int newHeight,
            Action<int, int, Color> resizeLargerCallback,
            Action<int, int> resizeSmallerCallback)
        {
            if (_state != State.Acting)
                throw new InvalidOperationException("Can only push an action during acting mode.");
            var action = new SingleAction(
                undo: () => resizeSmallerCallback(oldWidth, oldHeight),
                redo: () => resizeLargerCallback(newWidth, newHeight, Colors.Black));
            Push(action);
            Debug.WriteLine("Pushed region resize larger action.");
        }

        public void PushResizeSortedAction(
            int oldWidth, int newWidth,
            int oldHeight, int newHeight, int pickerSize,
            Action<int, int, int> resizeSortedCallback)
        {
            if (_state != State.Acting)
                throw new InvalidOperationException("Can only push an action during acting mode.");

            var action = new SingleAction(
                undo: () => resizeSortedCallback(oldWidth, oldHeight, pickerSize),
                redo: () => resizeSortedCallback(newWidth, newHeight, pickerSize));
            Push(action);

            Debug.WriteLine("Pushed region rotate action.");
        }

        public void PushRegionRotateAction(Int32Rect rectangle, Action<Int32Rect, bool> rotatingCallback)
        {
            if (_state != State.Acting)
                throw new InvalidOperationException("Can only push an action during acting mode.");

            var action = new SingleAction(
                undo: () => rotatingCallback(rectangle, true),
                redo: () => rotatingCallback(rectangle, false));
            Push(action);

            Debug.WriteLine("Pushed region rotate action.");
        }

        public void PushRegionFlipAction(Int32Rect rectangle, Action<Int32Rect> flippingCallback)
        {
            if (_state != State.Acting)
                throw new InvalidOperationException("Can only push an action during acting mode.");
            var action = new SingleAction(
                undo: () => flippingCallback(rectangle),
                redo: () => flippingCallback(rectangle));
            Push(action);
            Debug.WriteLine("Pushed region flip action.");
        }

        public void PushRegionMoveAction((int X, int Y) origPos, (int X, int Y) targetPos, int tileSize,
            Action<(int X, int Y), (int X, int Y), int> movingCallback)
        {
            if (_state != State.Acting)
                throw new InvalidOperationException("Can only push an action during acting mode.");
            var action = new SingleAction(
                undo: () => movingCallback(targetPos, origPos, tileSize),
                redo: () => movingCallback(origPos, targetPos, tileSize));
            Push(action);
            Debug.WriteLine("Pushed region move action.");
        }

        public void Undo()
        {
            if (!CanUndo) return;
            var action = _undoStack.Pop();
            action.Undo();
            _redoStack.Push(action);
        }

        public void Redo()
        {
            if (!CanRedo) return;
            var action = _redoStack.Pop();
            action.Redo();
            _undoStack.Push(action);
        }

        private void Push(IUndoableAction action)
        {
            _undoStack.Push(action);
            ClearRedoStack();
            Debug.WriteLine($"Pushed action. Undo stack size: {_undoStack.Count}, " +
                $"Current memory usage: {CurrentMemoryBytes} bytes, Usage: {100.0 * CurrentMemoryBytes / _maxMemoryBytes}%");
        }

        private void EnsureMemoryAvailable(long requiredBytes)
        {
            var bytesCount = CurrentMemoryBytes;
            while (bytesCount + requiredBytes > _maxMemoryBytes && _undoStack.Count > 0)
            {
                var oldest = _undoStack.Last();

                RemoveBottomElement(_undoStack);

                bytesCount -= oldest.SizeInBytes;
                oldest.ReturnData();
                Debug.WriteLine($"Cleared action. Undo stack size: {_undoStack.Count}");
            }
            if (_undoStack.Count == 0)
            {
                Debug.WriteLine("Cleared all actions. Undo stack is empty.");
            }
        }

        private void ClearRedoStack()
        {
            while (_redoStack.Count > 0)
            {
                var action = _redoStack.Pop();
                action.ReturnData();
            }
        }

        public void ClearAll()
        {
            foreach (var action in _undoStack)
                action.ReturnData();
            foreach (var action in _redoStack)
                action.ReturnData();

            _undoStack.Clear();
            _redoStack.Clear();
            Debug.WriteLine("Cleared undo and redo stacks as unmonitored action was performed.");
        }

        private void RemoveBottomElement<T>(Stack<T> stack)
        {
            Stack<T> tempStack = new Stack<T>();

            while (stack.Count > 0)
                tempStack.Push(stack.Pop());

            if (tempStack.Count > 0)
                tempStack.Pop();

            while (tempStack.Count > 0)
                stack.Push(tempStack.Pop());
        }

        private int NextHigherPowerOfTwo(int n)
        {
            if (n < 1) return 1;
            n--;
            n |= (n >> 1);
            n |= (n >> 2);
            n |= (n >> 4);
            n |= (n >> 8);
            n |= (n >> 16);
            return n + 1;
        }
    }
}