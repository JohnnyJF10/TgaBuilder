namespace TgaBuilderLib.Tr
{
    public partial class TrLevel
    {
        internal ushort UFixed16ToUShort(ushort ufixed16)
		{
            int fractionalPart = (ufixed16 & 0xFF) >= 0x7F ? 1 : 0;
            int integerPart = ufixed16 >> 8;
            
            ushort result = (ushort)(integerPart + fractionalPart);
            return result;
        }

		public (int x, int y, int width, int height) GetBoundingBox4(ushort[] shorts)
        {
            for (int i = 0; i < 8; i++)
                shorts[i] = UFixed16ToUShort(shorts[i]);

            int minX = Math.Min(Math.Min(shorts[0], shorts[2]), Math.Min(shorts[4], shorts[6]));
            int maxX = Math.Max(Math.Max(shorts[0], shorts[2]), Math.Max(shorts[4], shorts[6]));
            int minY = Math.Min(Math.Min(shorts[1], shorts[3]), Math.Min(shorts[5], shorts[7]));
            int maxY = Math.Max(Math.Max(shorts[1], shorts[3]), Math.Max(shorts[5], shorts[7]));
            return (minX, minY, maxX - minX, maxY - minY);
        }

        public (int x, int y, int width, int height) GetBoundingBox3(ushort[] shorts)
        {
            for (int i = 0; i < 6; i++)
                shorts[i] = UFixed16ToUShort(shorts[i]);

            int minX = Math.Min(Math.Min(shorts[0], shorts[2]), shorts[4]);
            int maxX = Math.Max(Math.Max(shorts[0], shorts[2]), shorts[4]);
            int minY = Math.Min(Math.Min(shorts[1], shorts[3]), shorts[5]);
            int maxY = Math.Max(Math.Max(shorts[1], shorts[3]), shorts[5]);

            return (minX, minY, maxX - minX, maxY - minY);
        }

        private List<(int x, int y)> RepackAtlas(List<(int width, int height)> textureSizes)
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

                    if (xOffset + width <= targetTrTexturePanelWidth && xOffset % width == 0 && yOffset % height == 0)
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
                    if (xOffset >= targetTrTexturePanelWidth)
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

            numPagesRepacked = yOffset / targetTrTexturePanelWidth 
                + (yOffset % targetTrTexturePanelWidth == 0 ? 0 : 1);

            return positions.ToList();
        }

        private int AlignTo(int value, int align)
            => (value + align - 1) / align * align;

        private bool IsPowerOfTwo(int x) => (x & (x - 1)) == 0;

        private int NextPowerOfTwo(int n)
        {
            n |= (n >> 1);
            n |= (n >> 2);
            n |= (n >> 4);
            n |= (n >> 8);
            n |= (n >> 16);
            return n - (n >> 1);
        }

        private int NextHigherMultiple(int value, int multiple)
            => multiple == 0
            ? value : value % multiple == 0
            ? value : value + multiple - value % multiple;
    }
}
