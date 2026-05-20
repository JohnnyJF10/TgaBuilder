using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{
    // Creates a colored debug map from label data and stores analysis dimensions.

    public int GetLabelAtPixel(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return 0;

        if (_labels.Length == 0)
            return 0;

        int index = y * Width + x;
        return _labels[index];
    }
    public byte[] GetLabelMap()
    {
        int[] currentLabels = new int[Width * Height];
        int labelCount = _labels[0];

        for (int i = 0; i < _labels.Length; i++)
        {
            int label = _labels[i];

            currentLabels[i] = label;

            if (label > labelCount)
                labelCount = label;
        }

        int stride = Width * TRANSITIONS_BPP;

        // Create target array
        byte[] map = new byte[Width * Height * TRANSITIONS_BPP];

        // Generate colors deterministically
        uint[] colors = new uint[labelCount + 1];
        Random rnd = new Random(42);

        for (int i = 1; i <= labelCount; i++)
        {
            byte r = (byte)rnd.Next(0, 256);
            byte g = (byte)rnd.Next(0, 256);
            byte b = (byte)rnd.Next(0, 256);

            // ARGB (same as before)
            colors[i] = (uint)(255 << 24 | r << 16 | g << 8 | b);
        }

        unsafe
        {
            fixed (byte* pDebug = map)
            fixed (int* pLabels = currentLabels)
            {
                for (int y = 0; y < Height; y++)
                {
                    int rowOffset = y * stride;
                    int labelRow = y * Width;

                    for (int x = 0; x < Width; x++)
                    {
                        int label = pLabels[labelRow + x];
                        int offset = rowOffset + x * TRANSITIONS_BPP;

                        uint color = (label == 0) ? 0xFF000000 : colors[label];

                        // Write BGRA
                        pDebug[offset + 0] = (byte)(color & 0xFF);          // B
                        pDebug[offset + 1] = (byte)((color >> 8) & 0xFF);   // G
                        pDebug[offset + 2] = (byte)((color >> 16) & 0xFF);  // R
                        pDebug[offset + 3] = 255;                           // A
                    }
                }
            }
        }
        return map;
    }

    // Gets a debug map highlighting the tile at (xPos, yPos) with the specified color, or gray if no color is provided.
    public byte[] GetTileIndicator(int tileIndex)
    {
        int[] currentLabels = new int[Width * Height];
        int labelCount = _labels[0];

        for (int i = 0; i < _labels.Length; i++)
        {
            int label = _labels[i];

            currentLabels[i] = label;

            if (label > labelCount)
                labelCount = label;
        }

        int requestedLabel = tileIndex;

        int stride = Width * TRANSITIONS_BPP;

        // Create target array
        byte[] map = new byte[Width * Height * TRANSITIONS_BPP];


        byte r = _systemAccentColor.R;
        byte g = _systemAccentColor.G;
        byte b = _systemAccentColor.B;

        // ARGB (same as before)
        uint indicatorColorArgb = (uint)(255 << 24 | r << 16 | g << 8 | b);

        unsafe
        {
            fixed (byte* pDebug = map)
            fixed (int* pLabels = currentLabels)
            {
                for (int y = 0; y < Height; y++)
                {
                    int rowOffset = y * stride;
                    int labelRow = y * Width;

                    for (int x = 0; x < Width; x++)
                    {
                        int label = pLabels[labelRow + x];

                        if (requestedLabel != label)
                            continue;

                        int offset = rowOffset + x * TRANSITIONS_BPP;

                        // Write BGRA
                        pDebug[offset + 0] = (byte)(indicatorColorArgb & 0xFF);          // B
                        pDebug[offset + 1] = (byte)((indicatorColorArgb >> 8) & 0xFF);   // G
                        pDebug[offset + 2] = (byte)((indicatorColorArgb >> 16) & 0xFF);  // R
                        pDebug[offset + 3] = 128;                                        // A
                    }
                }
            }
        }
        return map;
    }
}
