using System.Buffers;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Level
{
    public partial class TrLevel : Level
    {


        private bool _isNg;
        private long _textureInfosStreamPosition;

        private List<ushort> _rectTexIndices = new();
        private List<ushort> _triagTexIndices = new();
        private List<ushort> _animTexIndices = new();

        private List<(int x, int y, int width, int height)> _relevantTextureInfos = new();

        private int _numPages;

        private byte[]? _atlas8;

        private byte[]? _rawAtlas;


        public TrVersion Version { get; private set; }


        public TrLevel(string fileName,            
            int trTexturePanelHorPagesNum = 2,
            bool useTrTextureRepacking = false)
        {
            targetPanelWidth = trTexturePanelHorPagesNum * ORIGINAL_PAGE_SIZE;

            ReadLevel(fileName);

            if (Version == TrVersion.TRC)
                useTrTextureRepacking = false; // not supported currently

            if (useTrTextureRepacking)
            {
                RepackedTexturePositions = GetRepackedPositions(_relevantTextureInfos.Select(x => (x.width, x.height)).ToList());
                RepackAtlas();
            }
            else
            {
                int resHeight = ORIGINAL_PAGE_SIZE * _numPages / trTexturePanelHorPagesNum;
                targetPanelHeight = NextHigherMultiple(resHeight, ORIGINAL_PAGE_SIZE);

                TargetAtlas = targetPanelWidth == ORIGINAL_PAGE_SIZE
                    ? _rawAtlas
                    : RearrangeImagePages(
                        inputData:      _rawAtlas!,
                        originalWidth:  ORIGINAL_PAGE_SIZE,
                        blocksPerRow:   trTexturePanelHorPagesNum);
            }

            ResultBitmap = CreateWriteableBitmapFromByteArray(
                byteArray:      TargetAtlas!,
                width:          targetPanelWidth,
                height:         targetPanelHeight,
                pixelFormat:    PixelFormats.Bgra32);
        }


        protected override void RepackAtlas()
        {
            if (RepackedTexturePositions.Count != _relevantTextureInfos.Count) 
                throw new ArgumentException("The number of original and repack tiles must be the same.");

            TargetAtlas = new byte[targetPanelHeight * targetPanelWidth * IMPORT_BPP];

            for (int i = 0; i < _relevantTextureInfos.Count; i++)
            {
                PlaceTile(i);
            }
        }

        protected override void PlaceTile(int idx)
        {
            var oldInfo = _relevantTextureInfos[idx];
            var newPos = RepackedTexturePositions[idx];

            int sourceIndex = (oldInfo.y * ORIGINAL_PAGE_SIZE + oldInfo.x) * IMPORT_BPP;
            int destinationIndex = (newPos.y * targetPanelWidth + newPos.x) * IMPORT_BPP;

            int width = oldInfo.width;
            int height = oldInfo.height;

            for (int i = 0; i < height; i++)
            {
                if (destinationIndex + height * IMPORT_BPP > TargetAtlas!.Length 
                    || sourceIndex + height * IMPORT_BPP > _rawAtlas!.Length)
                    continue;
                Array.Copy(_rawAtlas, sourceIndex, TargetAtlas, destinationIndex, width * IMPORT_BPP);
                destinationIndex += targetPanelWidth * IMPORT_BPP;
                sourceIndex += ORIGINAL_PAGE_SIZE * IMPORT_BPP;
            }
        }

        private byte[] RearrangeImagePages(
            byte[] inputData,
            int originalWidth,
            int blocksPerRow)
        {
            if (originalWidth != 256)
                throw new ArgumentException("Original width must be 256 pixels.");

            const int blockWidth = 256;
            const int blockHeight = 256;

            int totalPixels = inputData.Length / IMPORT_BPP;
            int originalHeight = totalPixels / originalWidth;

            if (originalHeight % blockHeight != 0)
                throw new ArgumentException("Height must be a multiple of 256.");

            int actualBlocks = originalHeight / blockHeight;

            // Calculate rows, rounded
            int blockRows = (int)Math.Ceiling((double)actualBlocks / blocksPerRow);
            int totalBlocks = blockRows * blocksPerRow; 

            int newWidth = blockWidth * blocksPerRow;
            int newHeight = blockHeight * blockRows;

            byte[] outputData = new byte[newWidth * newHeight * IMPORT_BPP]; 

            for (int block = 0; block < actualBlocks; block++)
            {
                int blockRow = block / blocksPerRow;
                int blockCol = block % blocksPerRow;

                for (int y = 0; y < blockHeight; y++)
                {
                    for (int x = 0; x < blockWidth; x++)
                    {
                        int srcX = x;
                        int srcY = block * blockHeight + y;
                        int srcIndex = (srcY * originalWidth + srcX) * IMPORT_BPP;

                        int dstX = blockCol * blockWidth + x;
                        int dstY = blockRow * blockHeight + y;
                        int dstIndex = (dstY * newWidth + dstX) * IMPORT_BPP;

                        Buffer.BlockCopy(inputData, srcIndex, outputData, dstIndex, IMPORT_BPP);
                    }
                }
            }
            return outputData;
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
