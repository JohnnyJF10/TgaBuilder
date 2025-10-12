using Avalonia.Media.Imaging;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;

namespace TgaBuilderAvaloniaUi.Wrappers
{
    internal class BitmapWrapper : IReadableBitmap
    {

        public BitmapWrapper(Bitmap bitmap)
        {
            _innerBitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
        }
        private readonly Bitmap _innerBitmap;

        public int PixelWidth => _innerBitmap.PixelSize.Width;

        public int PixelHeight => _innerBitmap.PixelSize.Height;

        public bool HasAlpha => _innerBitmap.Format!.Value.BitsPerPixel == 32;

        public int Size => (int)(_innerBitmap.Size.Width * _innerBitmap.Size.Height * (HasAlpha ? 4 : 3));

        internal Bitmap InnerBitmap => _innerBitmap;


        public void CopyPixels(PixelRect sourceRect, Array pixels, int stride, int offset)
        {
            var rect = new Avalonia.PixelRect(sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height);

            if (pixels is byte[] bytes)
            {
                unsafe
                {
                    fixed (byte* p = bytes)
                    {
                        IntPtr dest = (IntPtr)(p + offset);
                        _innerBitmap.CopyPixels(rect, dest, bytes.Length - offset, stride);
                    }
                }
            }
            else
            {
                throw new ArgumentException("Only byte[], int[], ushort[] are supported", nameof(pixels));
            }
        }

        public void CopyPixels(Array pixels, int stride, int offset)
        {
            var rect = new PixelRect(0, 0, _innerBitmap.PixelSize.Width, _innerBitmap.PixelSize.Height);

            CopyPixels(rect, pixels, stride, offset);
        }

        public void Save(Stream stream, EncoderType encoderType = EncoderType.Png)
        {
            _innerBitmap.Save(stream);
        }

        public void Save(string filePath, EncoderType encoderType = EncoderType.Png)
        {
            _innerBitmap.Save(filePath);
        }

        public MemoryStream ToMemoryStream(EncoderType encoderType = EncoderType.Png)
        {
            var ms = new MemoryStream();
            Save(ms, encoderType);
            ms.Position = 0;
            return ms;
        }
    }
}
