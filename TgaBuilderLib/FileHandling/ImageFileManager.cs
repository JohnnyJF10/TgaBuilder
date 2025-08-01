﻿using Microsoft.VisualBasic.FileIO;
using Pfim;
using System.Buffers;
using System.Drawing.PSD;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.BitmapBytesIO;
using TgaBuilderLib.Enums;
using TgaBuilderLib.Level;
using ResizeMode = TgaBuilderLib.Enums.ResizeMode;

namespace TgaBuilderLib.FileHandling
{

    public class ImageFileManager : IImageFileManager
    {
        public ImageFileManager(
            Func<string, int, bool, LevelBase> trLevelFactory,
            Func<string, int, LevelBase> tenLevelFactory,
            IBitmapBytesIO bitmapIO)
        {
            _trLevelFactory = trLevelFactory;
            _tenLevelFactory = tenLevelFactory;
            _bitmapIO = bitmapIO;
        }

        private readonly Func<string, int, bool, LevelBase> _trLevelFactory;
        private readonly Func<string, int, LevelBase> _tenLevelFactory;

        private readonly IBitmapBytesIO _bitmapIO;

        private LevelBase? _loadedLevel;

        public ResultStatus ResultInfo { get; private set; } = ResultStatus.Success;

        public bool TrImportRepackingSelected { get; set; }
        public int TrImportHorPageNum { get; set; } = 1;


        public void LoadImageFile(
            string fileName, 
            ResizeMode mode = ResizeMode.SourceResize,
            CancellationToken? cancellationToken = null)
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
                    _bitmapIO.FromPfim(fileName, mode, cancellationToken);
                    return;

                case FileTypes.PSD:
                    _bitmapIO.FromPsd(fileName, mode, cancellationToken);
                    return;

                case FileTypes.PHD:
                case FileTypes.TR2:
                case FileTypes.TR4:
                case FileTypes.TRC:
                case FileTypes.TEN:

                    bool isTen = false;

                    if (fileType is FileTypes.TEN or FileTypes.TRC)
                        isTen = CheckIfTen(fileName, fileType);

                    _loadedLevel = isTen
                        ? GetNewTenLevel(
                            fileName: fileName,
                            trTexturePanelHorPagesNum: TrImportHorPageNum)
                        : GetNewTrLevel(
                            fileName: fileName,
                            trTexturePanelHorPagesNum: TrImportHorPageNum,
                            useTrTextureRepacking: TrImportRepackingSelected);

                    _loadedLevel.LoadLevel(cancellationToken);

                    if (!_loadedLevel.BitmapSpaceSufficient)
                        ResultInfo = ResultStatus.BitmapAreaNotSufficient;

                    return;


                case FileTypes.BMP:
                case FileTypes.PNG:
                case FileTypes.JPG:
                case FileTypes.JPEG:
                    _bitmapIO.FromUsual(fileName, mode, cancellationToken);
                    return;

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

            if (IsTga(extension))
                _bitmapIO.ToTga(bitmap);

            else if (IsUsual(extension))
                _bitmapIO.ToUsual(bitmap, extension);

            else
                throw new NotSupportedException($"Unsupported file format: {extension}");
        }

        public void WriteImageFile(string fileName, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            if (_bitmapIO.LoadedBytes is null)
                throw new InvalidOperationException("No image data loaded. Please load an image first.");

            string extension = Path.GetExtension(fileName).TrimStart('.').ToLower();

            if (IsTga(extension))
                _bitmapIO.WriteTga(fileName, cancellationToken);

            else if (IsUsual(extension))
                _bitmapIO.WriteUsual(fileName, cancellationToken);

            else
                throw new NotSupportedException($"Unsupported file format: {extension}");

            _bitmapIO.ClearLoadedData();
        }


        public WriteableBitmap GetDestinationConfirmBitmap(WriteableBitmap inputBitmap)
            => _bitmapIO.FromOtherBitmap(inputBitmap);

        public WriteableBitmap GetLoadedBitmap()
        {
            WriteableBitmap? wb = null;
            if (_loadedLevel is not null)
            {
                wb = _loadedLevel.GetResultBitmap();

                _loadedLevel.ClearAllData();

                _loadedLevel = null;
            }
            else if (_bitmapIO.LoadedBytes is not null)
            {
                wb = _bitmapIO.GetLoadedBitmap();

                _bitmapIO.ClearLoadedData();
            }

            if (wb is null)
                throw new InvalidOperationException("No bitmap data loaded or available.");

            return wb;
        }

        public void ClearLoadedData()
        {
            _bitmapIO.ClearLoadedData();
            _loadedLevel?.ClearAllData();
            _loadedLevel = null;
        }

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

        private LevelBase GetNewTrLevel(string fileName, int trTexturePanelHorPagesNum, bool useTrTextureRepacking)
            => _trLevelFactory.Invoke(fileName, trTexturePanelHorPagesNum, useTrTextureRepacking);

        private LevelBase GetNewTenLevel(string fileName, int trTexturePanelHorPagesNum)
            => _tenLevelFactory.Invoke(fileName, trTexturePanelHorPagesNum);

        private bool IsTga(string extension)
            => extension == "tga";

        private bool IsUsual(string extension)
            => extension is "png" or "jpg" or "jpeg" or "bmp";
    }
}
