using Microsoft.VisualBasic.FileIO;
using Pfim;
using System.Drawing.PSD;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapIO;
using TgaBuilderLib.Level;
using ResizeMode = TgaBuilderLib.Abstraction.ResizeMode;

namespace TgaBuilderLib.FileHandling
{

    public class ImageFileManager : IImageFileManager
    {
        public ImageFileManager(IBitmapIO bitmapIO)
        {
            _bitmapIO = bitmapIO;
        }


        private readonly IBitmapIO _bitmapIO;

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
                    return _bitmapIO.FromPfim(fileName, targetFormat, mode);

                case FileTypes.PSD:
                    return _bitmapIO.FromPsd(fileName, targetFormat, mode);

                case FileTypes.PHD:
                case FileTypes.TR2:
                case FileTypes.TR4:
                case FileTypes.TRC:
                case FileTypes.TEN:
                    WriteableBitmap? trRes;

                    bool isTen = false;

                    if (fileType is FileTypes.TEN or FileTypes.TRC)
                        isTen = CheckIfTen(fileName, fileType);

                    using (Level.Level trLevel = isTen
                        ? new TenLevel(
                            fileName: fileName,
                            trTexturePanelHorPagesNum: TrImportHorPageNum)
                        : new TrLevel(
                            fileName: fileName,
                            trTexturePanelHorPagesNum: TrImportHorPageNum,
                            useTrTextureRepacking: TrImportRepackingSelected))
                    {
                        trRes = trLevel.ResultBitmap;

                        if (!trLevel.BitmapSpaceSufficient)
                            ResultInfo = ResultStatus.BitmapAreaNotSufficient;
                    }
                    return trRes;


                case FileTypes.BMP:
                case FileTypes.PNG:
                case FileTypes.JPG:
                case FileTypes.JPEG:
                    return _bitmapIO.FromUsual(fileName, targetFormat, mode);

                default:
                    throw new NotSupportedException($"Filetype '{extension}' is not supported.");
                }
        }

        public void SaveImageFile(string fileName, BitmapSource bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap), "Bitmap cannot be null.");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            string extension = Path.GetExtension(fileName).TrimStart('.').ToLower();
            switch (extension)
            {
                case "tga":
                    _bitmapIO.ToTga(fileName, bitmap);
                    break;

                case "png":
                case "jpg":
                case "jpeg":
                case "bmp":
                    _bitmapIO.ToUsual(fileName, extension, bitmap);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported file format: {extension}");
            }
        }


        public WriteableBitmap GetDestinationConfirmBitmap(WriteableBitmap inputBitmap)
            => _bitmapIO.FromOtherBitmap(inputBitmap);


        private bool CheckIfTen(string fileName, FileTypes fileType)
        {
            if (fileType == FileTypes.TEN)
                return true;

            using (BinaryReader reader = new(new FileStream(fileName, FileMode.Open, FileAccess.Read)))
            {
                uint version = reader.ReadUInt32();
                if (version == 0x00345254)
                    return false;
                else
                    return true;
            }
        }
    }
}
