using System;
using System.Buffers;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TgaBuilderLib.Level
{
    public partial class TenLevel : LevelBase
    {
        private List<byte[]> _texPagesList = new();
        private List<(int width, int height, int size)> _texDimsList = new();

        private List<(int page, int x, int y, int width, int height)> _roomsTextureInfos = new();

        public TenVersion Version { get; private set; }

        public TenLevel(string fileName,
            int trTexturePanelHorPagesNum = 2)
        {
            targetPanelWidth = trTexturePanelHorPagesNum * ORIGINAL_PAGE_SIZE;

            ReadLevel(fileName);

            _roomsTextureInfos = _roomsTextureInfos.Distinct().ToList();

            RepackedTexturePositions = GetRepackedPositions(_roomsTextureInfos.Select(d => (d.width, d.height)).ToList());

            RepackAtlas();

            ClearTempData();

        }

        protected override void RepackAtlas()
        {
            if (RepackedTexturePositions.Count != _roomsTextureInfos.Count)
                throw new ArgumentException("The number of original and repack tiles must be the same.");

            int atlasSize = targetPanelHeight * targetPanelWidth * IMPORT_BPP;
            TargetAtlas = _bytePool.Rent(atlasSize);
            Array.Clear(TargetAtlas, 0, atlasSize);

            for (int i = 0; i < _roomsTextureInfos.Count; i++)
            {
                PlaceTile(i);
            }
        }

        protected override void PlaceTile(int idx)
        {
            var oldInfo = _roomsTextureInfos[idx];
            var page = _texPagesList[oldInfo.page];
            var pageInfo = _texDimsList[oldInfo.page];
            var newPos = RepackedTexturePositions[idx];

            int sourceIndex = (oldInfo.y * pageInfo.width + oldInfo.x) * IMPORT_BPP;
            int destinationIndex = (newPos.y * targetPanelWidth + newPos.x) * IMPORT_BPP;
            int width = oldInfo.width;
            int height = oldInfo.height;

            for (int i = 0; i < height; i++)
            {
                if (destinationIndex + height * 3 > TargetAtlas!.Length)
                    continue;

                if (sourceIndex + height * 3 > page.Length)
                    continue;

                Array.Copy(
                    sourceArray: page,
                    sourceIndex: sourceIndex,
                    destinationArray: TargetAtlas,
                    destinationIndex: destinationIndex,
                    length: width * IMPORT_BPP);

                destinationIndex += targetPanelWidth * IMPORT_BPP;
                sourceIndex += pageInfo.width * IMPORT_BPP;
            }
        }

        private byte[] GetRentedPixelArrayFromPng(byte[] pngBytes, int width, int height, int size)
        {
            var bitmapImage = new BitmapImage();
            using (var ms = new MemoryStream(pngBytes, 0, size, writable: false, publiclyVisible: true))
            {
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            width = bitmapImage.PixelWidth;
            height = bitmapImage.PixelHeight;

            var wb = new WriteableBitmap(bitmapImage);

            int stride = width * 4; // BGRA32
            byte[] pixelData = new byte[height * stride];

            wb.CopyPixels(pixelData, stride, 0);

            return pixelData; // Format: BGRA (Blue, Green, Red, Alpha)
        }

        public override void ClearTempData() {}
    }
}