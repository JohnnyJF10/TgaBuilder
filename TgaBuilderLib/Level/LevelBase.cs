using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TgaBuilderLib.Level
{
    public abstract class LevelBase
    {
        protected const int ORIGINAL_PAGE_SIZE = 256;
        protected const int MAX_SUPPORTED_TEX_SIZE = 512;
        protected const int ATLAS_MAX_HEIGHT = 32768;
        protected const int ORIGINAL_PAGE_PIXEL_COUNT = ORIGINAL_PAGE_SIZE * ORIGINAL_PAGE_SIZE;
        protected const int IMPORT_BPP = 4;

        protected readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;

        protected int targetPanelWidth;
        protected int targetPanelHeight;

        protected List<(int x, int y)> RepackedTexturePositions = new();

        public byte[]? TargetAtlas;


        public bool BitmapSpaceSufficient { get; protected set; } = true;


        protected abstract void ReadLevel(string fileName);
        protected abstract void RepackAtlas();
        protected abstract void PlaceTile(int idx);


        public abstract void ClearTempData();

        public void ClearAllData()
        {
            ClearTempData();
            if (TargetAtlas != null)
            {
                _bytePool.Return(TargetAtlas);
                TargetAtlas = null;
            }
        }

        protected (int x, int y, int width, int height) GetBoundingBox4(int[] corners)
        {
            int minX = Math.Min(Math.Min(corners[0], corners[2]), Math.Min(corners[4], corners[6]));
            int maxX = Math.Max(Math.Max(corners[0], corners[2]), Math.Max(corners[4], corners[6]));
            int minY = Math.Min(Math.Min(corners[1], corners[3]), Math.Min(corners[5], corners[7]));
            int maxY = Math.Max(Math.Max(corners[1], corners[3]), Math.Max(corners[5], corners[7]));

            return (minX, minY, maxX - minX, maxY - minY);
        }

        protected (int x, int y, int width, int height) GetBoundingBox3(int[] corners)
        {
            int minX = Math.Min(Math.Min(corners[0], corners[2]), corners[4]);
            int maxX = Math.Max(Math.Max(corners[0], corners[2]), corners[4]);
            int minY = Math.Min(Math.Min(corners[1], corners[3]), corners[5]);
            int maxY = Math.Max(Math.Max(corners[1], corners[3]), corners[5]);

            return (minX, minY, maxX - minX, maxY - minY);
        }

        protected List<(int x, int y)> GetRepackedPositions(List<(int width, int height)> textureSizes)
        {
            List<(int x, int y)> posList = new();

            // Sorting textures by height and width. ToDo: Centralized List Preprocessing
            var sortedTextures = textureSizes
                .Select((size, index) => (size, index))
                .OrderByDescending(t => t.size.height)
                .ThenByDescending(t => t.size.width)
                .ToList();

            // For Sorting Back
            var positions = new (int x, int y)[textureSizes.Count];

            int yOffset = 0;

            // Rowbased Packing
            while (sortedTextures.Count > 0)
            {
                int rowHeight = 0;
                int xOffset = 0;

                for (int i = 0; i < sortedTextures.Count;)
                {
                    var (size, originalIndex) = sortedTextures[i];
                    int width = size.width;
                    int height = size.height;

                    if (xOffset + width <= targetPanelWidth && xOffset % width == 0 && yOffset % height == 0)
                    {
                        // Placing
                        positions[originalIndex] = (xOffset, yOffset);
                        xOffset += width;
                        rowHeight = Math.Max(rowHeight, height);
                        sortedTextures.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                    if (xOffset >= targetPanelWidth)
                    {
                        break;
                    }
                }

                if (rowHeight == 0)
                {
                    // Security exit if no texture could be placed in this row to avoid infinite loop
                    throw new Exception("Unable to place any texture. Check requirements.");
                }

                yOffset += rowHeight;
            }

            targetPanelHeight = NextHigherMultiple(yOffset, ORIGINAL_PAGE_SIZE); 

            return positions.ToList();
        }

        protected Stream DecompressStream(Stream baseStream, uint compressedSize)
        {
            var limitedStream = new SubStream(baseStream, compressedSize);
            return new InflaterInputStream(limitedStream);
        }

        public WriteableBitmap GetResultBitmap()
        {
            if (TargetAtlas == null)
                throw new ArgumentNullException(nameof(TargetAtlas), "TargetAtlas byte array cannot be null.");
            int bitsPerPixel = PixelFormats.Bgra32.BitsPerPixel;
            int stride = (targetPanelWidth * bitsPerPixel + 7) / 8;
            int actualHeight = Math.Min(targetPanelHeight, ATLAS_MAX_HEIGHT);
            int expectedSize = stride * targetPanelHeight;
            int croppedSize = stride * actualHeight;

            if (TargetAtlas.Length < expectedSize)
                throw new ArgumentException("The byte array is smaller than the expected size for the given width and height.");

            if (targetPanelHeight > ATLAS_MAX_HEIGHT)
                BitmapSpaceSufficient = false;

            // WriteableBitmap with capped height
            WriteableBitmap writeableBitmap = new WriteableBitmap(targetPanelWidth, actualHeight, 96, 96, PixelFormats.Bgra32, null);

            writeableBitmap.Lock();

            // Only visible area
            IntPtr backBuffer = writeableBitmap.BackBuffer;
            Marshal.Copy(
                source: TargetAtlas, 
                startIndex:     0, 
                destination:    backBuffer, 
                length:         croppedSize);

            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, targetPanelWidth, actualHeight));
            writeableBitmap.Unlock();

            return writeableBitmap;
        }


        protected bool IsPowerOfTwo(int x) => (x & (x - 1)) == 0;

        protected int NextPowerOfTwo(int n)
        {
            n |= (n >> 1);
            n |= (n >> 2);
            n |= (n >> 4);
            n |= (n >> 8);
            n |= (n >> 16);
            return n - (n >> 1);
        }

        protected int NextHigherMultiple(int value, int multiple)
            => multiple == 0
            ? value : value % multiple == 0
            ? value : value + multiple - value % multiple;
    }
}
