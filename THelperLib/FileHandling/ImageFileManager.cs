using Pfim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.IO;
using THelperLib.Tr;
using System.Runtime.InteropServices;
using THelperLib.Abstraction;
using Microsoft.VisualBasic.FileIO;
using System.Drawing.PSD;
using THelperLib.Ten;
using System.Diagnostics;

namespace THelperLib.FileHandling
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
        private const int MAX_TARGET_WIDTH = 1024;
        private const int TR_PAGE_SIZE = 256;

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
                case FileTypes.TRC:
                    var trLevel = new TrLevel(
                        fileName:                   fileName,
                        trTexturePanelHorPagesNum:  TrImportHorPageNum,
                        useTrTextureRepacking:      TrImportRepackingSelected);

                    var trRes = trLevel.ResultBitmap;

                    if (!trLevel.BitmapSpaceSufficient)
                        ResultInfo = ResultStatus.BitmapAreaNotSufficient;

                    trLevel = null;
                    return trRes;

                case FileTypes.TEN:
                    var tenLevel = new TenLevel(fileName, TrImportHorPageNum);
                    var tenRes = tenLevel.ResultBitmap;

                    if (!tenLevel.BitmapSpaceSufficient)
                        ResultInfo = ResultStatus.BitmapAreaNotSufficient;

                    tenLevel = null;
                    return tenRes;

                case FileTypes.BMP:
                case FileTypes.PNG:
                case FileTypes.JPG:
                case FileTypes.JPEG:
                    return UsualImageFiletoWriteableBitmap(fileName, targetFormat, mode);

                default:
                    throw new NotSupportedException($"Filetype '{extension}' is not supported.");
            }
        }

        public void WriteImageFileFromBytes(string fileName, byte[] destinationData, int pixelWidth, int pixelHeight)
            => _tgaWriter.WriteTrTgaFromBytes(fileName, destinationData, pixelWidth, pixelHeight);

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
                        //if (bitmap.Format != PixelFormats.Bgra32)
                        //    bitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
                        //
                        //if (bitmap.CanFreeze && !bitmap.IsFrozen)
                        //    bitmap.Freeze();

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
            if (targetFormat is null)
                targetFormat = PixelFormats.Rgb24;

            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file does not exist.", filePath);

            if (targetFormat != PixelFormats.Bgra32 && targetFormat != PixelFormats.Rgb24)
                throw new NotSupportedException("Only PixelFormats.Bgra32 and PixelFormats.Rgb24 are supported for conversion.");

            BitmapImage originalImage = new BitmapImage();
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                originalImage.BeginInit();
                originalImage.CacheOption = BitmapCacheOption.OnLoad;
                originalImage.StreamSource = stream;
                originalImage.EndInit();
                originalImage.Freeze();
            }

            int originalWidth = originalImage.PixelWidth;
            int originalHeight = originalImage.PixelHeight;

            int paddedWidth = RoundUpToNextMultiple(originalWidth, TR_PAGE_SIZE);
            int paddedHeight = RoundUpToNextMultiple(originalHeight, TR_PAGE_SIZE);

            paddedWidth = Math.Clamp(
                value: paddedWidth,
                min: TR_PAGE_SIZE,
                max: mode == ResizeMode.TargetResize ? MAX_TARGET_WIDTH : MAX_SIZE);

            if (paddedWidth != originalWidth || paddedHeight != originalHeight)
                ResultInfo = ResultStatus.RezisingRequired;

            // Converting to desired format
            FormatConvertedBitmap converted = new FormatConvertedBitmap();
            converted.BeginInit();
            converted.Source = originalImage;
            converted.DestinationFormat = targetFormat ?? PixelFormats.Rgb24;
            converted.EndInit();

            int bytesPerPixel = targetFormat == PixelFormats.Bgra32 ? 4 : 3;
            int stride = paddedWidth * bytesPerPixel;

            WriteableBitmap paddedBitmap = new WriteableBitmap(
                pixelWidth: paddedWidth,
                pixelHeight: paddedHeight,
                dpiX: 96,
                dpiY: 96,
                pixelFormat: targetFormat ?? PixelFormats.Rgb24,
                palette: null);

            byte fillAlpha = targetFormat == PixelFormats.Bgra32 ? (byte)255 : (byte)0;
            byte[] blackPixels = new byte[paddedHeight * stride];
            for (int i = 0; i < blackPixels.Length; i += bytesPerPixel)
            {
                blackPixels[i + 0] = 0; // B
                blackPixels[i + 1] = 0; // G
                blackPixels[i + 2] = 0; // R
                if (bytesPerPixel == 4)
                    blackPixels[i + 3] = fillAlpha; // A
            }

            paddedBitmap.WritePixels(
                new Int32Rect(0, 0, paddedWidth, paddedHeight),
                blackPixels, stride, 0
            );

            // Copy original image pixels to padded bitmap
            CroppedBitmap croppedSource = new CroppedBitmap(converted, new Int32Rect(0, 0, originalWidth, originalHeight));
            int srcStride = originalWidth * bytesPerPixel;
            byte[] srcPixels = new byte[originalHeight * srcStride];
            croppedSource.CopyPixels(srcPixels, srcStride, 0);

            if (originalWidth > paddedWidth)
                originalWidth = paddedWidth;

            paddedBitmap.WritePixels(
                new Int32Rect(0, 0, originalWidth, originalHeight),
                srcPixels, srcStride, 0);

            return paddedBitmap;
        }
        private WriteableBitmap PfimageToWriteableBitmap(
            string filePath,
            PixelFormat? targetFormat = null,
            ResizeMode mode = ResizeMode.SourceResize)
        {
            using (var stream = File.OpenRead(filePath))
            using (var image = Pfimage.FromStream(stream))
            {
                PixelFormat formatToUse = targetFormat ?? PixelFormats.Rgb24;

                if (!File.Exists(filePath))
                    throw new FileNotFoundException("The specified file does not exist.", filePath);

                if (formatToUse != PixelFormats.Bgra32 && formatToUse != PixelFormats.Rgb24)
                    throw new NotSupportedException("Only PixelFormats.Bgra32 and PixelFormats.Rgb24 are supported for conversion.");

                int bytesPerPixel = formatToUse == PixelFormats.Bgra32 ? 4 : 3;

                int originalWidth = image.Width;
                int originalHeight = image.Height;

                int paddedWidth = RoundUpToNextMultiple(originalWidth, TR_PAGE_SIZE);
                int paddedHeight = RoundUpToNextMultiple(originalHeight, TR_PAGE_SIZE);

                paddedWidth = Math.Clamp(
                    value: paddedWidth,
                    min: TR_PAGE_SIZE,
                    max: mode == ResizeMode.TargetResize ? MAX_TARGET_WIDTH : MAX_SIZE);

                if (paddedWidth != originalWidth || paddedHeight != originalHeight)
                    ResultInfo = ResultStatus.RezisingRequired;

                int paddedStride = paddedWidth * bytesPerPixel;
                byte[] paddedPixels = new byte[paddedHeight * paddedStride];

                for (int i = 0; i < paddedPixels.Length; i += bytesPerPixel)
                {
                    paddedPixels[i + 0] = 0; // B
                    paddedPixels[i + 1] = 0; // G
                    paddedPixels[i + 2] = 0; // R
                    if (bytesPerPixel == 4)
                        paddedPixels[i + 3] = 255; // A
                }


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
                            // RGBA (Pfim) → BGRA (WPF)
                            paddedPixels[dstIndex + 0] = image.Data[srcIndex + 2]; // B
                            paddedPixels[dstIndex + 1] = image.Data[srcIndex + 1]; // G
                            paddedPixels[dstIndex + 2] = image.Data[srcIndex + 0]; // R
                            paddedPixels[dstIndex + 3] = image.Data[srcIndex + 3]; // A
                        }
                        else if (formatToUse == PixelFormats.Rgb24 && image.Format == ImageFormat.Rgb24)
                        {
                            // RGB fits
                            paddedPixels[dstIndex + 0] = image.Data[srcIndex + 2]; // B
                            paddedPixels[dstIndex + 1] = image.Data[srcIndex + 1]; // G
                            paddedPixels[dstIndex + 2] = image.Data[srcIndex + 0]; // R
                        }
                        else if (formatToUse == PixelFormats.Rgb24 && image.Format == ImageFormat.Rgba32)
                        {
                            // RGBA (Pfim) → RGB (WPF): Alpha ignored
                            paddedPixels[dstIndex + 0] = image.Data[srcIndex + 2]; // B
                            paddedPixels[dstIndex + 1] = image.Data[srcIndex + 1]; // G
                            paddedPixels[dstIndex + 2] = image.Data[srcIndex + 0]; // R
                        }
                        else
                        {
                            throw new NotSupportedException($"Conversion from {image.Format} to {formatToUse} is not supported.");
                        }
                    }
                }

                WriteableBitmap bitmap = new WriteableBitmap(paddedWidth, paddedHeight, 96, 96, formatToUse, null);
                bitmap.WritePixels(
                    new Int32Rect(0, 0, paddedWidth, paddedHeight),
                    paddedPixels,
                    paddedStride,
                    0
                );

                return bitmap;
            }
        }

        private WriteableBitmap MergePsdLayersAndGetBitmap(
            string psdFilePath,
            PixelFormat? targetFormat = null,
            ResizeMode mode = ResizeMode.SourceResize)
        {
            PixelFormat formatToUse = targetFormat ?? PixelFormats.Rgb24;

            if (!File.Exists(psdFilePath))
                throw new FileNotFoundException("The specified file does not exist.", psdFilePath);

            if (formatToUse != PixelFormats.Bgra32 && formatToUse != PixelFormats.Rgb24)
                throw new NotSupportedException("Only PixelFormats.Bgra32 and PixelFormats.Rgb24 are supported for conversion.");

            var psd = new PsdFile();
            psd.Load(psdFilePath);

            int bytesPerPixel = formatToUse == PixelFormats.Bgra32 ? 4 : 3;

            int originalWidth = psd.Columns;
            int originalHeight = psd.Rows;

            int paddedWidth = RoundUpToNextMultiple(originalWidth, TR_PAGE_SIZE);
            int paddedHeight = RoundUpToNextMultiple(originalHeight, TR_PAGE_SIZE);

            paddedWidth = Math.Clamp(
                value: paddedWidth,
                min: TR_PAGE_SIZE,
                max: mode == ResizeMode.TargetResize ? MAX_TARGET_WIDTH : MAX_SIZE);

            if (paddedWidth != originalWidth || paddedHeight != originalHeight)
                ResultInfo = ResultStatus.RezisingRequired;

            int paddedStride = paddedWidth * bytesPerPixel;
            byte[] paddedPixels = new byte[paddedHeight * paddedStride];
            for (int i = 0; i < paddedPixels.Length; i += bytesPerPixel)
            {
                paddedPixels[i + 0] = 0; // B
                paddedPixels[i + 1] = 0; // G
                paddedPixels[i + 2] = 0; // R
                if (bytesPerPixel == 4)
                    paddedPixels[i + 3] = 255; // A
            }

            var data = psd.ImageData;
            int channelCount = data.Length;

            var wb = new WriteableBitmap(paddedWidth, paddedHeight, 96, 96, formatToUse, null);

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

                    float alpha = a / 255f;

                    if (formatToUse == PixelFormats.Bgra32)
                    {
                        paddedPixels[globalIndex + 0] = b; // B
                        paddedPixels[globalIndex + 1] = g; // G
                        paddedPixels[globalIndex + 2] = r; // R
                        paddedPixels[globalIndex + 3] = (byte)(a * alpha); // A
                    }
                    else if (formatToUse == PixelFormats.Rgb24)
                    {
                        paddedPixels[globalIndex + 0] = r; // R
                        paddedPixels[globalIndex + 1] = g; // G
                        paddedPixels[globalIndex + 2] = b; // B
                    }
                    else
                    {
                        throw new NotSupportedException($"Conversion from PSD to {formatToUse} is not supported.");
                    }
                }
            }

            wb.WritePixels(new Int32Rect(0, 0, paddedWidth, paddedHeight), paddedPixels, paddedStride, 0);
            return wb;
        }

        private int RoundUpToNextMultiple(int number, int multiple)
        {
            if (multiple == 0) return number;
            return (number + multiple - 1) / multiple * multiple;
        }
    }
}
