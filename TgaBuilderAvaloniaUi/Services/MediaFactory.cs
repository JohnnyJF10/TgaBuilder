using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderAvaloniaUi.Wrappers;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;

namespace TgaBuilderAvaloniaUi.Services
{
    internal class MediaFactory : IMediaFactory
    {
        public IWriteableBitmap CloneBitmap(IReadableBitmap source)
        {
            if (source is not BitmapWrapper wrapper)
                throw new ArgumentException("Bitmap is not a BitmapSource", nameof(source));

            var bitmap = wrapper.InnerBitmap;   

            return new WriteableBitmapWrapper(bitmap);
        }

        public IReadableBitmap CreateBitmapFromRaw(int pixelWidth, int pixelHeight, bool hasAlpha, Array pixels, int stride)
        {
            if (pixels is not byte[] bytes)
                throw new ArgumentException("Only byte[] is supported", nameof(pixels));

            Bitmap bitmap;

            unsafe
            {
                fixed (byte* p = bytes)
                {
                    IntPtr ptr = (IntPtr)p;

                    bitmap = new(
                        format:         hasAlpha ? PixelFormats.Bgra8888 : PixelFormats.Rgb24,
                        alphaFormat:    AlphaFormat.Unpremul,
                        data:           ptr,
                        size:           new PixelSize(pixelWidth, pixelHeight),
                        dpi:            new Vector(96, 96),
                        stride:         stride);
                }
            }

            return new BitmapWrapper(bitmap);
        }

        public IWriteableBitmap CreateEmptyBitmap(int width, int height, bool hasAlpha)
        {
            WriteableBitmap writeableBitmap = new WriteableBitmap(
                size:           new PixelSize(width, height),
                dpi:            new Vector(96, 96),
                format:         hasAlpha ? PixelFormats.Bgra8888 : PixelFormats.Rgb24,
                alphaFormat:    AlphaFormat.Unpremul);

            return new WriteableBitmapWrapper(writeableBitmap);
        }

        public IWriteableBitmap CreateRescaledBitmap(
            IWriteableBitmap source,
            int newWidth,
            int newHeight,
            BitmapScalingMode scalingMode = BitmapScalingMode.Linear)
        {
            if (source is not WriteableBitmapWrapper wrapper)
                throw new ArgumentException("Bitmap is not a WriteableBitmap", nameof(source));

            var bitmap = wrapper.InnerBitmap;

            var interpolationMode = scalingMode switch
            {
                BitmapScalingMode.NearestNeighbor   => BitmapInterpolationMode.None,
                BitmapScalingMode.Linear            => BitmapInterpolationMode.MediumQuality,
                BitmapScalingMode.Fant              => BitmapInterpolationMode.HighQuality,
                _ => BitmapInterpolationMode.None,
            };

            var scaled = bitmap.CreateScaledBitmap(
                destinationSize:    new PixelSize(newWidth, newHeight),
                interpolationMode:  interpolationMode);


            return new WriteableBitmapWrapper(scaled);
        }

        public IWriteableBitmap LoadBitmap(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            var bitmap = new Bitmap(filePath);

            return new WriteableBitmapWrapper(bitmap);

        }

        public IWriteableBitmap LoadBitmap(Stream stream)
        {
            var bitmap = new Bitmap(stream);

            return new WriteableBitmapWrapper(bitmap);
        }

        public IReadableBitmap LoadReadableBitmap(string filePath)
        {
            var bitmap = new Bitmap(filePath);

            return new BitmapWrapper(bitmap);
        }

        public IReadableBitmap LoadReadableBitmap(Stream stream)
        {
            var bitmap = new Bitmap(stream);

            return new BitmapWrapper(bitmap);
        }
    }
}
