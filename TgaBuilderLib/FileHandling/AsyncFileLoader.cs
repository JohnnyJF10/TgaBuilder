using System.IO;
using System.Threading;
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

        public byte[] LoadCore(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (extension == ".dds" || extension == ".tga")
            {
                using var image = Pfim.Pfimage.FromFile(filePath);

                LoadedWidth = image.Width;
                LoadedHeight = image.Height;
                LoadedHasAlpha = image.Format == Pfim.ImageFormat.Rgba32;

                if (!LoadedHasAlpha && image.Format != Pfim.ImageFormat.Rgb24)
                    throw new NotSupportedException($"Image format {image.Format} is not supported.");

                if (image.Format == Pfim.ImageFormat.Rgba32)
                    return image.Data;
                else
                {
                    var LoadedBytes = new byte[LoadedHeight * LoadedWidth * 3];

                    for (int y = 0; y < LoadedHeight; y++)
                    {
                        int srcOffset = y * image.Stride;
                        int dstOffset = y * LoadedStride;

                        for (int x = 0; x < LoadedWidth; x++)
                        {
                            int srcIndex = srcOffset + x * image.BitsPerPixel / 8;
                            int dstIndex = dstOffset + x * 3;

                            LoadedBytes[dstIndex + 0] = image.Data[srcIndex + 2]; // R
                            LoadedBytes[dstIndex + 1] = image.Data[srcIndex + 1]; // G
                            LoadedBytes[dstIndex + 2] = image.Data[srcIndex + 0]; // B
                        }
                    }
                    return LoadedBytes;
                }
            }
            else
            {
                var wb = _mediaFactory.LoadBitmap(filePath);

                LoadedWidth = wb.PixelWidth;
                LoadedHeight = wb.PixelHeight;
                LoadedHasAlpha = wb.HasAlpha;

                int stride = wb.BackBufferStride;
                byte[] pixels = new byte[LoadedHeight * stride];
                wb.CopyPixels(pixels, stride, 0);

                return pixels;
            }
        }
    }
}
