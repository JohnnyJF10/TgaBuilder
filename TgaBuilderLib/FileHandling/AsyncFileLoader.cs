using System.IO;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.FileHandling
{
    public class AsyncFileLoader : IAsyncFileLoader
    {
        public AsyncFileLoader(
            IMediaFactory mediaFactory)
        {
            _mediaFactory = mediaFactory;
        }

        private readonly IMediaFactory _mediaFactory;

        public int LoadedWidth { get; private set; }
        public int LoadedHeight { get; private set; }
        public int LoadedStride => LoadedWidth * (LoadedHasAlpha ? 4 : 3);
        public bool LoadedHasAlpha { get; private set; }

        public HashSet<string> SupportedExtensions 
            => new(StringComparer.OrdinalIgnoreCase)
            {
                ".dds", ".tga", ".png", ".jpg", ".jpeg", ".bmp"
            };

        public async Task<IWriteableBitmap> LoadAndResizeAsync(
            string filePath,
            int targetWidth,
            int targetHeight,
            BitmapScalingMode scalingMode)
            => await Task.Run(() =>
            {
                string extension = Path.GetExtension(filePath).ToLowerInvariant();

                IWriteableBitmap sourceBitmap;

                if (extension == ".dds" || extension == ".tga")
                {
                    using var image = Pfim.Pfimage.FromFile(filePath);

                    bool hasAlpha = image.Format == Pfim.ImageFormat.Rgba32;

                    if (!hasAlpha && image.Format != Pfim.ImageFormat.Rgb24)
                        throw new NotSupportedException($"Image format {image.Format} is not supported.");

                    var wb = _mediaFactory.CreateEmptyBitmap(image.Width, image.Height, hasAlpha);

                    wb.WritePixels(
                        new PixelRect(0, 0, image.Width, image.Height),
                        image.Data,
                        image.Stride);

                    sourceBitmap = wb;
                }
                else
                {
                    sourceBitmap = _mediaFactory.LoadBitmap(filePath);
                }
                return _mediaFactory.CreateRescaledBitmap(sourceBitmap, targetWidth, targetHeight);
            });

        public byte[] LoadCore(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (extension == ".dds" || extension == ".tga")
            {
                using var image = Pfim.Pfimage.FromFile(filePath);

                bool hasAlpha = image.Format == Pfim.ImageFormat.Rgba32;

                if (!hasAlpha && image.Format != Pfim.ImageFormat.Rgb24)
                    throw new NotSupportedException($"Image format {image.Format} is not supported.");

                LoadedWidth = image.Width;
                LoadedHeight = image.Height;
                LoadedHasAlpha = hasAlpha;

                return image.Data;
            }
            else
            {
                var wb = _mediaFactory.LoadBitmap(filePath);
                int stride = wb.BackBufferStride;
                byte[] pixels = new byte[LoadedHeight * stride];
                wb.CopyPixels(pixels, stride, 0);

                LoadedWidth = wb.PixelWidth;
                LoadedHeight = wb.PixelHeight;
                LoadedHasAlpha = wb.HasAlpha;

                return pixels;
            }
        }
    }
}
