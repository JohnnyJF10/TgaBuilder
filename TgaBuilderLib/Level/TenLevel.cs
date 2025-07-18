using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TgaBuilderLib.Level
{
    public partial class TenLevel : Level
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

            ResultBitmap = CreateWriteableBitmapFromByteArray(
                byteArray: TargetAtlas!,
                width: targetPanelWidth,
                height: targetPanelHeight,
                pixelFormat: PixelFormats.Bgra32);

            ReturnRentedArrays();
        }

        protected override void RepackAtlas()
        {
            if (RepackedTexturePositions.Count != _roomsTextureInfos.Count)
                throw new ArgumentException("The number of original and repack tiles must be the same.");

            TargetAtlas = new byte[targetPanelHeight * targetPanelWidth * 4];

            for (int i = 0; i < _roomsTextureInfos.Count; i++)
            {
                PlaceTile(i);
            }
        }

        protected override void PlaceTile(int idx)
        {
            int pageIdx = _roomsTextureInfos[idx].page;
            int sourceIndex = (_roomsTextureInfos[idx].y * _texDimsList[pageIdx].width + _roomsTextureInfos[idx].x) * 4;
            int destinationIndex = (RepackedTexturePositions[idx].y * targetPanelWidth + RepackedTexturePositions[idx].x) * 4;
            int width = _roomsTextureInfos[idx].width;
            int height = _roomsTextureInfos[idx].height;

            for (int i = 0; i < height; i++)
            {
                if (destinationIndex + height * 3 > TargetAtlas!.Length) continue;
                if (sourceIndex + height * 3 > _texPagesList[pageIdx].Length) continue;

                Array.Copy(_texPagesList[pageIdx], sourceIndex, TargetAtlas, destinationIndex, width * 4);
                destinationIndex += targetPanelWidth * 4;
                sourceIndex += _texDimsList[pageIdx].width * 4;
            }
        }

        private byte[] GetRentedPixelArrayFromPng(byte[] pngBytes, int width, int height)
        {
            var bitmapImage = new BitmapImage();
            using (var ms = new MemoryStream(pngBytes))
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
            byte[] pixelData = _bytePool.Rent(height * stride);

            wb.CopyPixels(pixelData, stride, 0);

            return pixelData; // Format: BGRA (Blue, Green, Red, Alpha)
        }

        private void ReturnRentedArrays()
        {
            foreach (var page in _texPagesList)
            {
                if (page.Length == 0) 
                    continue; 

                _bytePool.Return(page);
            }
        }
    }
}
