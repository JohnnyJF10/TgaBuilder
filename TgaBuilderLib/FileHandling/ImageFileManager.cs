using Pfim;
using System.Drawing.PSD;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Level;

namespace TgaBuilderLib.FileHandling
{
    public enum ResizeMode
    {
        SourceResize,
        TargetResize,
    }

    public class ImageFileManager : IImageFileManager
    {
        public ImageFileManager(ITgaWriter tgaWriter) 
        {
            _tgaWriter = tgaWriter;
        }

        private const int MAX_SIZE = 32768;
        private const int TR_PAGE_SIZE = 256;
        private const int MAX_TARGET_WIDTH = 16 * TR_PAGE_SIZE;


        private readonly ITgaWriter _tgaWriter;

        public ResultStatus ResultInfo { get; private set; } = ResultStatus.Success;

        public bool TrImportRepackingSelected { get; set; }
        public int TrImportHorPageNum { get; set; } = 1;


        public WriteableBitmap OpenImageFile(
            string fileName, 
            PixelFormat? targetFormat = null, 
            ResizeMode mode = ResizeMode.SourceResize)
        {
            string extension = Path.GetExtension(fileName).TrimStart('.').ToLower();

            ResultInfo = ResultStatus.Success;

            if (!Enum.TryParse(typeof(FileTypes), extension, true, out var fileTypeObj))
                throw new NotSupportedException($"Filetype '{extension}' is not supported.");

            var fileType = (FileTypes)fileTypeObj;

            switch (fileType)
            {
                case FileTypes.TGA:
                case FileTypes.DDS:
                    return PfimageToWriteableBitmap(fileName, targetFormat, mode);

                case FileTypes.PSD:
                    return MergePsdLayersAndGetBitmap(fileName, targetFormat, mode);

                case FileTypes.PHD:
                case FileTypes.TR2:
                case FileTypes.TR4:
                    WriteableBitmap? trRes;
                    using (TrLevel trLevel = new TrLevel(
                        fileName: fileName,
                        trTexturePanelHorPagesNum: TrImportHorPageNum,
                        useTrTextureRepacking: TrImportRepackingSelected))
                    {
                        trRes = trLevel.ResultBitmap;

                        if (!trLevel.BitmapSpaceSufficient)
                            ResultInfo = ResultStatus.BitmapAreaNotSufficient;
                    }
                    return trRes;

                case FileTypes.TEN:
                    var tenLevel = new TenLevel(fileName, TrImportHorPageNum);
                    var tenRes = tenLevel.ResultBitmap;

                    if (!tenLevel.BitmapSpaceSufficient)
                        ResultInfo = ResultStatus.BitmapAreaNotSufficient;

                    tenLevel = null;
                    return tenRes;

                case FileTypes.TRC:

                    using (BinaryReader reader = new BinaryReader( new FileStream(fileName, FileMode.Open, FileAccess.Read)))
                    {
                        uint version = reader.ReadUInt32();
                        if (version == 0x00345254)
                            goto case FileTypes.TR4;
                        else
                            goto case FileTypes.TEN;
                    }

                case FileTypes.BMP:
                        case FileTypes.PNG:
                        case FileTypes.JPG:
                        case FileTypes.JPEG:
                            return UsualImageFiletoWriteableBitmap(fileName, targetFormat, mode);

                        default:
                            throw new NotSupportedException($"Filetype '{extension}' is not supported.");
                        }
        }

        public void WriteImageFileFromBitmap(string fileName, BitmapSource bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap), "Bitmap cannot be null.");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            string extension = Path.GetExtension(fileName).TrimStart('.').ToLower();
            switch (extension)
            {
                case "tga":
                    _tgaWriter.WriteTrTgaFromBitmap(fileName, bitmap);
                    break;

                case "png":
                case "jpg":
                case "jpeg":
                case "bmp":
                    using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                    {
                        BitmapEncoder encoder = extension switch
                        {
                            "png" => new PngBitmapEncoder(),
                            "jpg" or "jpeg" => new JpegBitmapEncoder(),
                            "bmp" => new BmpBitmapEncoder(),
                            _ => throw new NotSupportedException($"Unsupported file format: {extension}")
                        };

                        encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        encoder.Save(fileStream);
                    }
                    break;
                default:
                    throw new NotSupportedException($"Unsupported file format: {extension}");
            }
        }


        private WriteableBitmap UsualImageFiletoWriteableBitmap(
            string filePath,
            PixelFormat? targetFormat = null,
            ResizeMode mode = ResizeMode.SourceResize)
        {
            PixelFormat formatToUse = targetFormat ?? PixelFormats.Rgb24;

            ValidateImageInput(filePath, formatToUse);

            BitmapImage originalImage = GetInputBitmapFromFile(filePath);

            int bytesPerPixel = formatToUse == PixelFormats.Bgra32 ? 4 : 3;

            int originalWidth = originalImage.PixelWidth;
            int originalHeight = originalImage.PixelHeight;

            int paddedWidth = CalculatePaddedWidth(originalWidth, mode);
            int paddedHeight = CalculatePaddedHeight(originalHeight, mode);

            int stride = paddedWidth * bytesPerPixel;

            FormatConvertedBitmap converted = FormatConvertBitmap(formatToUse, originalImage);

            byte[] blackPixels = CreateBlackPixelBuffer(
                width:          paddedWidth,
                height:         paddedHeight,
                bytesPerPixel:  bytesPerPixel,
                alpha:          (byte)(formatToUse == PixelFormats.Bgra32 ? 255 : 0));

            WriteableBitmap paddedBitmap = new WriteableBitmap(
                pixelWidth:     paddedWidth,
                pixelHeight:    paddedHeight,
                dpiX:           96,
                dpiY:           96,
                pixelFormat:    formatToUse,
                palette:        null);

            paddedBitmap.WritePixels(
                sourceRect: new Int32Rect(0, 0, paddedWidth, paddedHeight),
                pixels: blackPixels,
                stride: stride,
                offset: 0);

            CroppedBitmap croppedSource = new CroppedBitmap(
                source:     converted, 
                sourceRect: new Int32Rect(0, 0, originalWidth, originalHeight));

            int srcStride = originalWidth * bytesPerPixel;

            byte[] srcPixels = new byte[originalHeight * srcStride];

            croppedSource.CopyPixels(srcPixels, srcStride, 0);

            paddedBitmap.WritePixels(
                sourceRect: new Int32Rect(0, 0, originalWidth, originalHeight), 
                pixels:     srcPixels, 
                stride:     srcStride, 
                offset:     0);

            return paddedBitmap;
        }

        private WriteableBitmap PfimageToWriteableBitmap(
            string filePath,
            PixelFormat? targetFormat = null,
            ResizeMode mode = ResizeMode.SourceResize)
        {
            using var stream = File.OpenRead(filePath);
            using var image = Pfimage.FromStream(stream);

            PixelFormat formatToUse = targetFormat ?? PixelFormats.Rgb24;

            ValidateImageInput(filePath, formatToUse);

            int bytesPerPixel = formatToUse == PixelFormats.Bgra32 ? 4 : 3;

            int originalWidth = image.Width;
            int originalHeight = image.Height;

            int paddedWidth = CalculatePaddedWidth(originalWidth, mode);
            int paddedHeight = CalculatePaddedHeight(originalHeight, mode);

            int paddedStride = paddedWidth * bytesPerPixel;

            byte[] paddedPixels = CreateBlackPixelBuffer(paddedWidth, paddedHeight, bytesPerPixel);

            bool sourceIsRgba = image.Format == ImageFormat.Rgba32;

            for (int y = 0; y < originalHeight; y++)
            {
                int srcOffset = y * image.Stride;
                int dstOffset = y * paddedStride;

                for (int x = 0; x < originalWidth; x++)
                {
                    int srcIndex = srcOffset + x * image.BitsPerPixel / 8;
                    int dstIndex = dstOffset + x * bytesPerPixel;

                    if (formatToUse == PixelFormats.Bgra32 && sourceIsRgba)
                    {
                        paddedPixels[dstIndex + 0] = image.Data[srcIndex + 2]; // B
                        paddedPixels[dstIndex + 1] = image.Data[srcIndex + 1]; // G
                        paddedPixels[dstIndex + 2] = image.Data[srcIndex + 0]; // R
                        paddedPixels[dstIndex + 3] = image.Data[srcIndex + 3]; // A
                    }
                    else if (formatToUse == PixelFormats.Rgb24)
                    {
                        paddedPixels[dstIndex + 0] = image.Data[srcIndex + 2];
                        paddedPixels[dstIndex + 1] = image.Data[srcIndex + 1];
                        paddedPixels[dstIndex + 2] = image.Data[srcIndex + 0];
                    }
                    else
                    {
                        throw new NotSupportedException($"Conversion from {image.Format} to {formatToUse} is not supported.");
                    }
                }
            }

            WriteableBitmap bitmap = new WriteableBitmap(
                pixelWidth:     paddedWidth,
                pixelHeight:    paddedHeight,
                dpiX:           96,
                dpiY:           96,
                pixelFormat:    formatToUse,
                palette:        null);

            bitmap.WritePixels(
                sourceRect: new Int32Rect(0, 0, paddedWidth, paddedHeight), 
                pixels:     paddedPixels, 
                stride:     paddedStride, 
                offset:     0);

            return bitmap;
        }


        private WriteableBitmap MergePsdLayersAndGetBitmap(
            string psdFilePath,
            PixelFormat? targetFormat = null,
            ResizeMode mode = ResizeMode.SourceResize)
        {
            PixelFormat formatToUse = targetFormat ?? PixelFormats.Rgb24;

            ValidateImageInput(psdFilePath, formatToUse);

            var psd = new PsdFile();
            psd.Load(psdFilePath);

            int bytesPerPixel = formatToUse == PixelFormats.Bgra32 ? 4 : 3;

            int originalWidth = psd.Columns;
            int originalHeight = psd.Rows;

            int paddedWidth = CalculatePaddedWidth(originalWidth, mode);
            int paddedHeight = CalculatePaddedHeight(originalHeight, mode);

            int paddedStride = paddedWidth * bytesPerPixel;

            byte[] paddedPixels = CreateBlackPixelBuffer(
                width:          paddedWidth, 
                height:         paddedHeight, 
                bytesPerPixel:  bytesPerPixel);

            var data = psd.ImageData;
            int channelCount = data.Length;

            for (int y = 0; y < originalHeight; y++)
            {
                for (int x = 0; x < originalWidth; x++)
                {
                    int layerIndex = y * originalWidth + x;
                    int globalIndex = (y * paddedWidth + x) * bytesPerPixel;

                    byte a = channelCount == 4 ? data[3][layerIndex] : (byte)255;
                    byte r = data[0][layerIndex];
                    byte g = data[1][layerIndex];
                    byte b = data[2][layerIndex];

                    if (formatToUse == PixelFormats.Bgra32)
                    {
                        paddedPixels[globalIndex + 0] = b;
                        paddedPixels[globalIndex + 1] = g;
                        paddedPixels[globalIndex + 2] = r;
                        paddedPixels[globalIndex + 3] = a;
                    }
                    else if (formatToUse == PixelFormats.Rgb24)
                    {
                        paddedPixels[globalIndex + 0] = r;
                        paddedPixels[globalIndex + 1] = g;
                        paddedPixels[globalIndex + 2] = b;
                    }
                }
            }

            WriteableBitmap wb = new WriteableBitmap(
                pixelWidth:     paddedWidth,
                pixelHeight:    paddedHeight,
                dpiX:           96,
                dpiY:           96,
                pixelFormat:    formatToUse,
                palette:        null);

            wb.WritePixels(
                sourceRect: new Int32Rect(0, 0, paddedWidth, paddedHeight), 
                pixels:     paddedPixels, 
                stride:     paddedStride, 
                offset:     0);

            return wb;
        }

        private FormatConvertedBitmap FormatConvertBitmap(PixelFormat formatToUse, BitmapImage originalImage)
        {
            FormatConvertedBitmap converted = new FormatConvertedBitmap();
            converted.BeginInit();
            converted.Source = originalImage;
            converted.DestinationFormat = formatToUse;
            converted.EndInit();
            return converted;
        }

        private BitmapImage GetInputBitmapFromFile(string filePath)
        {
            BitmapImage originalImage = new BitmapImage();
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                originalImage.BeginInit();
                originalImage.CacheOption = BitmapCacheOption.OnLoad;
                originalImage.StreamSource = stream;
                originalImage.EndInit();
                originalImage.Freeze();
            }

            return originalImage;
        }

        private void ValidateImageInput(string filePath, PixelFormat format)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file does not exist.", filePath);

            if (format != PixelFormats.Bgra32 && format != PixelFormats.Rgb24)
                throw new NotSupportedException("Only PixelFormats.Bgra32 and PixelFormats.Rgb24 are supported for conversion.");
        }

        private int CalculatePaddedWidth(int width, ResizeMode mode)
        {
            int paddedWidth = RoundUpToNextMultiple(width, TR_PAGE_SIZE);

            paddedWidth = Math.Clamp(
                value:  paddedWidth,
                min:    TR_PAGE_SIZE,
                max:    mode == ResizeMode.TargetResize ? MAX_TARGET_WIDTH : MAX_SIZE);

            if (paddedWidth != width)
                ResultInfo = ResultStatus.RezisingRequired;

            return paddedWidth;
        }

        private int CalculatePaddedHeight(int height, ResizeMode mode)
        {
            int paddedHeight = RoundUpToNextMultiple(height, TR_PAGE_SIZE);

            paddedHeight = Math.Clamp(
                value:  paddedHeight,
                min:    TR_PAGE_SIZE,
                max:    MAX_SIZE);

            if (paddedHeight != height)
                ResultInfo = ResultStatus.RezisingRequired;

            return paddedHeight;
        }

        private byte[] CreateBlackPixelBuffer(int width, int height, int bytesPerPixel, byte alpha = 255)
        {
            int stride = width * bytesPerPixel;
            byte[] buffer = new byte[height * stride];

            for (int i = 0; i < buffer.Length; i += bytesPerPixel)
            {
                buffer[i + 0] = 0; // B
                buffer[i + 1] = 0; // G
                buffer[i + 2] = 0; // R
                if (bytesPerPixel == 4)
                    buffer[i + 3] = alpha; // A
            }

            return buffer;
        }

        private int RoundUpToNextMultiple(int number, int multiple)
            => multiple == 0 ? number : (number + multiple - 1) / multiple * multiple;
    }
}
