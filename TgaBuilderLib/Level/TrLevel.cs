using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Level
{
    public partial class TrLevel : LevelBase
    {
        private readonly ITrngDecrypter? _trngDecrypter;

        private bool _isNg;
        private long _textureInfosStreamPosition;

        private List<ushort> _rectTexIndices = new();
        private List<ushort> _triagTexIndices = new();
        private List<ushort> _animTexIndices = new();

        private List<(int x, int y, int width, int height)> _relevantTextureInfos = new();

        private int _numPages;

        private byte[]? _paletteTr1;
        private byte[]? _atlasTr1;

        private byte[]? _rawAtlasCompressed;
        private byte[]? _rawAtlas;


        public TrVersion Version { get; private set; }


        public TrLevel(string fileName,            
            int trTexturePanelHorPagesNum = 2,
            bool useTrTextureRepacking = false,
            ITrngDecrypter? trngDecrypter = null)
        {
            _trngDecrypter = trngDecrypter;

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

                if (targetPanelWidth == ORIGINAL_PAGE_SIZE)
                    CopyRawToTarget();
                else
                    RearrangeImagePages();
            }
            ClearTempData();
        }


        protected override void RepackAtlas()
        {
            if (RepackedTexturePositions.Count != _relevantTextureInfos.Count) 
                throw new ArgumentException("The number of original and repack tiles must be the same.");

            int atlasSize = targetPanelHeight * targetPanelWidth * IMPORT_BPP;
            TargetAtlas = ArrayPool<byte>.Shared.Rent(atlasSize);
            Array.Clear(TargetAtlas, 0, atlasSize);

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

        private void CopyRawToTarget()
        {
            if (_rawAtlas is null)
                throw new InvalidOperationException("Raw atlas is not initialized.");

            TargetAtlas = _bytePool.Rent(_rawAtlas.Length);

            Buffer.BlockCopy(
                src:        _rawAtlas, 
                srcOffset:  0, 
                dst:        TargetAtlas, 
                dstOffset:  0, 
                count:      _rawAtlas.Length);
        }

        private void RearrangeImagePages()        
        {
            if (_rawAtlas is null)
                throw new InvalidOperationException("Raw atlas is not initialized.");

            int blocksPerRow = targetPanelWidth / ORIGINAL_PAGE_SIZE;

            int originalHeight = _numPages * ORIGINAL_PAGE_SIZE;

            if (originalHeight % ORIGINAL_PAGE_SIZE != 0)
                throw new ArgumentException("Height must be a multiple of 256.");

            int atlasSize = targetPanelWidth * targetPanelHeight * IMPORT_BPP;
            TargetAtlas = _bytePool.Rent(atlasSize);
            Array.Clear(TargetAtlas, 0, atlasSize);

            for (int block = 0; block < _numPages; block++)
            {
                int blockRow = block / blocksPerRow;
                int blockCol = block % blocksPerRow;

                for (int y = 0; y < ORIGINAL_PAGE_SIZE; y++)
                {
                    for (int x = 0; x < ORIGINAL_PAGE_SIZE; x++)
                    {
                        int srcX = x;
                        int srcY = block * ORIGINAL_PAGE_SIZE + y;
                        int srcIndex = (srcY * ORIGINAL_PAGE_SIZE + srcX) * IMPORT_BPP;

                        int dstX = blockCol * ORIGINAL_PAGE_SIZE + x;
                        int dstY = blockRow * ORIGINAL_PAGE_SIZE + y;
                        int dstIndex = (dstY * targetPanelWidth + dstX) * IMPORT_BPP;

                        Buffer.BlockCopy(
                            src:        _rawAtlas, 
                            srcOffset:  srcIndex, 
                            dst:        TargetAtlas, 
                            dstOffset:  dstIndex, 
                            count:      IMPORT_BPP);
                    }
                }
            }
        }

        public override void ClearTempData()
        {
            if (_paletteTr1 is not null)
            {
                _bytePool.Return(_paletteTr1);
                _paletteTr1 = null;
            }

            if (_atlasTr1 is not null)
            {
                _bytePool.Return(_atlasTr1);
                _atlasTr1 = null;
            }

            if (_rawAtlasCompressed is not null)
            {
                _bytePool.Return(_rawAtlasCompressed);
                _rawAtlasCompressed = null;
            }

            if (_rawAtlas is not null)
            {
                _bytePool.Return(_rawAtlas);
                _rawAtlas = null;
            }
        }
    }
}
