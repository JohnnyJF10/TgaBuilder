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

    // The label map picking and the hover indicator follow manual moves/rotations, so they read
    // from the current occupancy map once it has been built; before that they fall back to the raw
    // segmentation. The colored debug map (GetLabelMap) deliberately keeps using _labels.
    private int[] CurrentLabels =>
        (_selectionBuilt && _manualLabels.Length == _labels.Length) ? _manualLabels : _labels;

    public int GetLabelAtPixel(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return 0;

        if (!_labelsBuilt)
            return 0;

        int index = y * Width + x;
        return CurrentLabels[index];
    }
    public byte[] GetLabelMap()
    {
        if (!_labelsBuilt)
            return Array.Empty<byte>();

        int totalPixels = Width * Height;

        // Find the highest label by scanning _labels directly (no throwaway copy).
        int labelCount = 0;
        for (int i = 0; i < totalPixels; i++)
        {
            int label = _labels[i];
            if (label > labelCount)
                labelCount = label;
        }

        int stride = Width * TRANSITIONS_BPP;

        // Reuse the owned label-map buffer. Every pixel is written below (label 0 → opaque black,
        // otherwise the deterministic color), so no clear is needed.
        byte[] map = _scratchLabelMap;

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
            fixed (int* pLabels = _labels)
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
        if (!_labelsBuilt)
            return Array.Empty<byte>();

        int totalPixels = Width * Height;
        int requestedLabel = tileIndex;

        int stride = Width * TRANSITIONS_BPP;

        // Reuse the owned label-map buffer. Only the matching-label pixels are written below, so
        // it must be cleared first to keep non-matching pixels transparent (matching the former
        // freshly allocated, zero-initialized array).
        byte[] map = _scratchLabelMap;
        Array.Clear(map, 0, totalPixels * TRANSITIONS_BPP);


        byte r = _systemAccentColor.R;
        byte g = _systemAccentColor.G;
        byte b = _systemAccentColor.B;

        // ARGB (same as before)
        uint indicatorColorArgb = (uint)(255 << 24 | r << 16 | g << 8 | b);

        // Highlight the tile at its current location: a moved tile lights up at its new position
        // and a vacated hole stays empty, reflecting the manual adjustments.
        int[] sourceLabels = CurrentLabels;

        unsafe
        {
            fixed (byte* pDebug = map)
            fixed (int* pLabels = sourceLabels)
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
