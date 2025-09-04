
using System.IO;

using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;
using TgaBuilderWpfUi.Wrappers;

namespace TgaBuilderWpfUi.Services
{
    internal class MediaFactory : IMediaFactory
    {
        public IWriteableBitmap CloneBitmap(IReadableBitmap source)
        {
            if (source is not BitmapSourceWrapper wrapper)
                throw new ArgumentException("Bitmap is not a BitmapSource", nameof(source));

            var writeableBitmap = new WriteableBitmap(wrapper.InnerBitmapSource);

            return new WriteableBitmapWrapper(writeableBitmap);
        }

        public IReadableBitmap CreateBitmapFromRaw(int pixelWidth, int pixelHeight, bool hasAlpha, Array pixels, int stride)
        {
            BitmapSource bitmapSource = BitmapSource.Create(
                pixelWidth:     pixelWidth,
                pixelHeight:    pixelHeight,
                dpiX:           96, 
                dpiY:           96, 
                pixelFormat:    hasAlpha ? PixelFormats.Bgra32 : PixelFormats.Rgb24,
                palette:        null, 
                pixels:         pixels,
                stride:         stride);

            return new BitmapSourceWrapper(bitmapSource);
        }

        public IWriteableBitmap CreateEmptyBitmap(int width, int height, bool hasAlpha)
        {
            WriteableBitmap writeableBitmap = new WriteableBitmap(
                pixelWidth:     width,
                pixelHeight:    height,
                dpiX:           96,
                dpiY:           96,
                pixelFormat:    hasAlpha ? PixelFormats.Bgra32 : PixelFormats.Rgb24,
                palette:        null);

            return new WriteableBitmapWrapper(writeableBitmap);
        }

        public IWriteableBitmap CreateRescaledBitmap(
            IWriteableBitmap source, 
            int newWidth, 
            int newHeight, 
            TgaBuilderLib.Enums.BitmapScalingMode scalingMode = TgaBuilderLib.Enums.BitmapScalingMode.Linear)
        {
            if (source is not WriteableBitmapWrapper wrapper)
                throw new ArgumentException("Bitmap is not a WriteableBitmap", nameof(source));

            var wpfScalingMode = scalingMode switch
            {
                TgaBuilderLib.Enums.BitmapScalingMode.NearestNeighbor => BitmapScalingMode.NearestNeighbor,
                TgaBuilderLib.Enums.BitmapScalingMode.Linear => BitmapScalingMode.Linear,
                TgaBuilderLib.Enums.BitmapScalingMode.Fant => BitmapScalingMode.Fant,
                _ => BitmapScalingMode.Linear,
            };

            var wb = wrapper.InnerWriteableBitmap;

            var drawingVisual = new DrawingVisual();

            var rect = new System.Windows.Rect(0, 0, newWidth, newHeight);

            using (var dc = drawingVisual.RenderOpen())
            {
                RenderOptions.SetBitmapScalingMode(
                    target: drawingVisual,
                    bitmapScalingMode: wpfScalingMode);
                dc.DrawImage(wb, rect);
            }

            var resized = new RenderTargetBitmap(
                pixelWidth:     newWidth,
                pixelHeight:    newHeight,
                dpiX:           96,
                dpiY:           96,
                pixelFormat:    PixelFormats.Bgra32);

            resized.Render(drawingVisual);

            return new WriteableBitmapWrapper(new WriteableBitmap(resized));
        }

        public IWriteableBitmap LoadBitmap(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            var uri = new Uri(filePath, UriKind.Absolute);

            var bitmapImage = new BitmapImage(uri);

            var writeableBitmap = new WriteableBitmap(bitmapImage);

            return new WriteableBitmapWrapper(writeableBitmap);
        }

        public IWriteableBitmap LoadBitmap(Stream stream)
        {
            var bitmapImage = new BitmapImage();

            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();

            var writeableBitmap = new WriteableBitmap(bitmapImage);

            return new WriteableBitmapWrapper(writeableBitmap);
        }

        public IReadableBitmap LoadReadableBitmap(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            var uri = new Uri(filePath, UriKind.Absolute);

            var bitmapImage = new BitmapImage(uri);

            return new BitmapSourceWrapper(bitmapImage);
        }

        public IReadableBitmap LoadReadableBitmap(Stream stream)
        {
            var bitmapImage = new BitmapImage();

            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();

            return new BitmapSourceWrapper(bitmapImage);
        }
    }
}
