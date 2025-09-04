using System.Buffers;

using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.UndoRedo
{
    public class BitmapEditAction : IUndoableAction
    {
        public BitmapEditAction(
            PixelRect region,
            byte[] oldPixels,
            byte[] newPixels,
            ArrayPool<byte> byteArrayPool,
            Action<PixelRect, byte[]> placingCallback,
            bool ownsMemory = true)
        {
            Region = region;
            OldPixels = oldPixels;
            NewPixels = newPixels;
            this.ownsMemory = ownsMemory;
            _placingCallback = placingCallback;
            _returnArrayCallback = b => byteArrayPool.Return(b);
        }


        public PixelRect Region { get; }
        public byte[] OldPixels { get; private set; }
        public byte[] NewPixels { get; private set; }

        private readonly bool ownsMemory;

        private  Action<PixelRect, byte[]> _placingCallback;
        private  Action<byte[]> _returnArrayCallback;

        public long SizeInBytes => (OldPixels?.Length ?? 0) + (NewPixels?.Length ?? 0);

        public void Undo()
        {
            _placingCallback(Region, OldPixels);
        }

        public void Redo()
        {
            _placingCallback(Region, NewPixels);
        }

        public void ReturnData()
        {
            if (!ownsMemory) return;

            if (OldPixels != null) _returnArrayCallback(OldPixels);
            if (NewPixels != null) _returnArrayCallback(NewPixels);

            _placingCallback = (_, _) => { };
            _returnArrayCallback = _ => { };
        }
    }
}
