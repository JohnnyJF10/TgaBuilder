using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace THelperLib.UndoRedo
{
    public class CropAction : IUndoableAction
    {
        public CropAction(
        byte[] croppedPixels,
        int oldWidth,
        int oldHeight,
        int newWidth,
        int newHeight,
        ArrayPool<byte> byteArrayPool,
        Action<int, int, byte[]> undoCallback,
        Action<int, int> redoCallback,
        bool ownsMemory = true)
        {
            CroppedPixels = croppedPixels;
            _oldWidth = oldWidth;
            _oldHeight = oldHeight;
            _newWidth = newWidth;
            _newHeight = newHeight;
            this.ownsMemory = ownsMemory;
            _undoCallback = undoCallback;
            _redoCallback = redoCallback;
            _returnArrayCallback = b => byteArrayPool.Return(b);
        }

        int _oldWidth;
        int _oldHeight;
        int _newWidth;
        int _newHeight;

        public byte[] CroppedPixels { get; private set; }

        private readonly bool ownsMemory;

        private Action<int, int, byte[]> _undoCallback;
        private Action<int, int> _redoCallback;
        private Action<byte[]> _returnArrayCallback;

        public long SizeInBytes => (CroppedPixels?.Length ?? 0);

        public void Undo()
        {
            _undoCallback(_oldWidth, _oldHeight, CroppedPixels);
        }

        public void Redo()
        {
            _redoCallback(_newWidth, _newHeight);
        }

        public void ReturnData()
        {
            if (!ownsMemory) return;

            if (CroppedPixels != null) _returnArrayCallback(CroppedPixels);

            CroppedPixels = null!;

            _undoCallback = (_, _, _) => { };
            _redoCallback = (_, _) => { };
            _returnArrayCallback = _ => { };
        }
    }
}
