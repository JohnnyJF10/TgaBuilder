using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TgaBuilderLib.FileHandling
{
    public class AsyncFileLoader : IAsyncFileLoader
    {
        public int LoadedWidth { get; private set; }
        public int LoadedHeight { get; private set; }
        public int LoadedStride => LoadedWidth * (LoadedPixelFormat.BitsPerPixel / 8);
        public PixelFormat LoadedPixelFormat { get; private set; }

        public HashSet<string> SupportedExtensions 
            => new(StringComparer.OrdinalIgnoreCase)
            {
                ".dds", ".tga", ".png", ".jpg", ".jpeg", ".bmp"
            };

        public async Task<WriteableBitmap> LoadAndResizeAsync(
            string filePath,
            int targetWidth,
            int targetHeight,
            BitmapScalingMode scalingMode)
            => await Task.Run(() =>
            {
                string extension = Path.GetExtension(filePath).ToLowerInvariant();

                WriteableBitmap sourceBitmap;

                if (extension == ".dds" || extension == ".tga")
                {
                    using var image = Pfim.Pfimage.FromFile(filePath);

                    PixelFormat pixelFormat = image.Format switch
                    {
                        Pfim.ImageFormat.Rgba32 => PixelFormats.Bgra32,
                        Pfim.ImageFormat.Rgb24 => PixelFormats.Bgr24,
                        _ => throw new NotSupportedException($"Image format {image.Format} is not supported.")
                    };

                    var wb = new WriteableBitmap(image.Width, image.Height, 96, 96, pixelFormat, null);
                    wb.WritePixels(
                        new Int32Rect(0, 0, image.Width, image.Height),
                        image.Data,
                        image.Stride,
                        0
                    );

                    sourceBitmap = wb;
                }
                else
                {
                    // Use WPF decoder for BMP, PNG, JPG, JPEG etc.
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze(); // for thread safety

                    sourceBitmap = new WriteableBitmap(bitmap);
                }

                // Now resize it
                var rect = new Rect(0, 0, targetWidth, targetHeight);
                var drawingVisual = new DrawingVisual();

                using (var dc = drawingVisual.RenderOpen())
                {
                    RenderOptions.SetBitmapScalingMode(drawingVisual, scalingMode);
                    dc.DrawImage(sourceBitmap, rect);
                }

                var resized = new RenderTargetBitmap(targetWidth, targetHeight, 96, 96, PixelFormats.Pbgra32);
                resized.Render(drawingVisual);

                return new WriteableBitmap(resized);
            });

        public byte[] LoadCore(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (extension == ".dds" || extension == ".tga")
            {
                using var image = Pfim.Pfimage.FromFile(filePath);

                PixelFormat pixelFormat = image.Format switch
                {
                    Pfim.ImageFormat.Rgba32 => PixelFormats.Bgra32,
                    Pfim.ImageFormat.Rgb24 => PixelFormats.Bgr24,
                    _ => throw new NotSupportedException($"Image format {image.Format} is not supported.")
                };

                LoadedWidth = image.Width;
                LoadedHeight = image.Height;
                LoadedPixelFormat = pixelFormat;

                return image.Data;
            }
            else
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                LoadedWidth = bitmap.PixelWidth;
                LoadedHeight = bitmap.PixelHeight;
                LoadedPixelFormat = bitmap.Format;

                var wb = new WriteableBitmap(bitmap);
                int stride = wb.BackBufferStride;
                byte[] pixels = new byte[LoadedHeight * stride];
                wb.CopyPixels(pixels, stride, 0);

                return pixels;
            }
        }
    }
}
