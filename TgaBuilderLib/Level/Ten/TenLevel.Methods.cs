﻿using System.IO;
using TgaBuilderLib.Utils;

namespace TgaBuilderLib.Level
{
    public partial class TenLevel
    {
        private byte[] DecompressTen(BinaryReader reader, uint compressedSize)
        {
            byte[] levelData = reader.ReadBytes((int)compressedSize);
            levelData = ZLib.DecompressData(levelData);
            return levelData;
        }

        public (int x, int y, int width, int height) GetBoundingBox4(int[] corners)
        {
            int minX = Math.Min(Math.Min(corners[0], corners[2]), Math.Min(corners[4], corners[6]));
            int maxX = Math.Max(Math.Max(corners[0], corners[2]), Math.Max(corners[4], corners[6]));
            int minY = Math.Min(Math.Min(corners[1], corners[3]), Math.Min(corners[5], corners[7]));
            int maxY = Math.Max(Math.Max(corners[1], corners[3]), Math.Max(corners[5], corners[7]));

            return (minX, minY, maxX - minX, maxY - minY);
        }

        public (int x, int y, int width, int height) GetBoundingBox3(int[] corners)
        {
            int minX = Math.Min(Math.Min(corners[0], corners[2]), corners[4]);
            int maxX = Math.Max(Math.Max(corners[0], corners[2]), corners[4]);
            int minY = Math.Min(Math.Min(corners[1], corners[3]), corners[5]);
            int maxY = Math.Max(Math.Max(corners[1], corners[3]), corners[5]);

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

            repackedHeight = yOffset;

            return positions.ToList();
        }

        private long ReadLEB128(BinaryReader reader, bool signedValue)
        {
            long result = 0;
            int shift = 0;
            byte currentByte;

            do
            {
                currentByte = reader.ReadByte();
                result |= (long)(currentByte & 0x7F) << shift;
                shift += 7;
            }
            while ((currentByte & 0x80) != 0);

            if (signedValue && shift < 64 && (currentByte & 0x40) != 0)
            {
                result |= -1L << shift;
            }

            return result;
        }

        private string ReadString(BinaryReader reader)
        {
            long numBytes = ReadLEB128(reader, signedValue: false);

            if (numBytes <= 0)
                return string.Empty;

            byte[] stringBytes = reader.ReadBytes((int)numBytes);

            return System.Text.Encoding.UTF8.GetString(stringBytes);
        }

        private bool IsPowerOfTwo(int x) => (x & (x - 1)) == 0;

        private int NextHigherMultiple(int value, int multiple)
            => multiple == 0 
            ? value : value % multiple == 0 
            ? value : value + multiple - value % multiple;
    }


}
