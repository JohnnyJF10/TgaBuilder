
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.UndoRedo
{
    public interface IUndoRedoManager
    {
        // Status
        bool CanRedo { get; }
        bool CanUndo { get; }
        bool IsRenting { get; }
        long CurrentMemoryBytes { get; }
        long MaxMemoryBytes { get; }

        public void TakeStatusSnapshot();
        public bool IsTargetDirty();


        // Undo/Redo Actions
        void ClearAllOutOfMemory();

        void ClearAllNewFile();

        void Undo();

        void Redo();


        // Push Actions
        void PushBitmapEditAction(
            byte[] oldPixels,
            byte[] newPixels,
            PixelRect region,
            Action<PixelRect, byte[]> placingCallback);

        void PushRegionFlipAction(
            PixelRect rectangle,
            Action<PixelRect> flippingCallback);

        void PushRegionMoveAction(
            (int X, int Y) origPos,
            (int X, int Y) targetPos,
            int tileSize,
            Action<(int X, int Y), (int X, int Y), int> movingCallback);

        void PushRegionRotateAction(
            PixelRect rectangle,
            Action<PixelRect, bool> rotatingCallback);

        void PushResizeLargerAction(
            int oldWidth,
            int newWidth,
            int oldHeight,
            int newHeight,
            Action<int, int> resizeLargerCallback,
            Action<int, int> resizeSmallerCallback);

        void PushResizeSmallerAction(
            byte[] croppedPixels,
            int oldWidth,
            int newWidth,
            int oldHeight,
            int newHeight,
            Action<int, int> resizeSmallerCallback,
            Action<int, int, byte[]> resizeLargerCallback);

        void PushResizeSortedAction(
            int oldWidth,
            int newWidth,
            int oldHeight,
            int newHeight,
            int pickerSize,
            Action<int, int, int> resizeSortedCallback);


        // Memory Management
        byte[] RentUndoRedoArray();

        bool TryBeginRenting(
            int totalSizeInBytes,
            int arraysNeeded = 2);
    }
}