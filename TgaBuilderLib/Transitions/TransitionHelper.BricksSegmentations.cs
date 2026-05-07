using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{
    /// <summary>
    /// Performs watershed segmentation on the filtered image and assigns labels to the regions.
    /// Returns the number of labels (seed candidates) found. 
    /// </summary>
    /// <param name="filtered">The filtered image data.</param>
    /// <param name="labels">The array to store the labels for each pixel.</param>
    /// <returns>The number of seed candidates found.</returns>
    private int WatershedSegmentation(float[] filtered, int[] labels)
    {
        // Seed candidates (keep all valid local maxima)
        var seedCandidates = new List<(int idx, float val)>(128);

        for (int y = MarkerRadius; y < Height - MarkerRadius; y++)
        {
            int row = y * Width;
            for (int x = MarkerRadius; x < Width - MarkerRadius; x++)
            {
                int idx = row + x;
                float val = filtered[idx];
                bool isMax = true;

                for (int iy = -MarkerRadius; iy <= MarkerRadius; iy++)
                {
                    int nRow = (y + iy) * Width;
                    for (int ix = -MarkerRadius; ix <= MarkerRadius; ix++)
                    {
                        if (ix == 0 && iy == 0) continue;
                        if (filtered[nRow + x + ix] >= val)
                        {
                            isMax = false;
                            break;
                        }
                    }
                    if (!isMax) break;
                }

                if (isMax)
                    seedCandidates.Add((idx, val));
            }
        }

        // Buckets
        Queue<int>[] buckets = new Queue<int>[256];
        for (int i = 0; i < 256; i++)
            buckets[i] = new Queue<int>(32);

        for (int i = 0; i < seedCandidates.Count; i++)
        {
            int label = i + 1;
            int idx = seedCandidates[i].idx;

            labels[idx] = label;

            // Removed TileSegment logic. Just enqueue neighbors.
            EnqueueNeighbors(idx, label, labels, filtered, buckets, 255);
        }

        // Watershed flood
        for (int b = 255; b >= 0; b--)
        {
            var q = buckets[b];
            while (q.Count > 0)
            {
                int idx = q.Dequeue();

                if (labels[idx] != 0) continue;

                int label = GetExistingNeighborLabel(idx, labels);
                if (label == 0) continue;

                labels[idx] = label;

                // Removed AddPixelToTile logic.
                EnqueueNeighbors(idx, label, labels, filtered, buckets, b);
            }
        }

        // Final fill
        FinalFill(labels);

        return seedCandidates.Count;
    }


    // Returns the first existing 4-neighbor label around the given pixel index.
    private int GetExistingNeighborLabel(int idx, int[] labels)
    {
        if (idx >= Width && labels[idx - Width] != 0) return labels[idx - Width];
        if (idx < labels.Length - Width && labels[idx + Width] != 0) return labels[idx + Width];
        if (idx % Width > 0 && labels[idx - 1] != 0) return labels[idx - 1];
        if (idx % Width < Width - 1 && labels[idx + 1] != 0) return labels[idx + 1];
        return 0;
    }

    // Fills remaining unlabeled pixels by attaching them to adjacent labeled regions.
    private void FinalFill(int[] labels)
    {
        // A single Z-order style scan is enough to bind final gaps to neighbors
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] == 0)
            {
                int l = GetExistingNeighborLabel(i, labels);
                if (l != 0)
                {
                    labels[i] = l;
                }
            }
        }
    }

    // Enqueues valid 4-neighbor pixels into intensity buckets for flood expansion.
    private void EnqueueNeighbors(int idx, int label, int[] labels, float[] blur, Queue<int>[] buckets, int maxB)
    {
        // Up, down, left, right
        int[] neighbors = { idx - Width, idx + Width, idx - 1, idx + 1 };
        for (int i = 0; i < 4; i++)
        {
            int nIdx = neighbors[i];
            if (nIdx >= 0 && nIdx < labels.Length)
            {
                // Check horizontal boundary
                if (i >= 2 && (nIdx / Width != idx / Width)) continue;

                if (labels[nIdx] == 0)
                {
                    int b = (int)blur[nIdx];
                    if (b > maxB) b = maxB;
                    buckets[b].Enqueue(nIdx);
                }
            }
        }
    }


    /// <summary>
    /// Segments the input image into labeled regions using XY projection and assigns unique labels to each segment.    
    /// Returns the number of labels (segments) found.
    /// </summary>
    /// <remarks>This method uses horizontal and vertical projection profiles to detect valleys and segment
    /// the image into rectangular regions. Each region is assigned a unique integer label. The method assumes that the
    /// image dimensions (Width and Height) and the marker radius (MarkerRadius) are set appropriately before
    /// calling.</remarks>
    /// <param name="filtered">A one-dimensional array of filtered pixel values representing the image to be segmented. The array must have a
    /// length equal to Width × Height.</param>
    /// <param name="labels">A one-dimensional array that receives the label for each pixel. Must be the same length as the filtered array.
    /// Each element will be set to the label of the corresponding segment.</param>
    /// <returns>The total number of unique segments identified and labeled in the image.</returns>

    private int XYProjectionSegmentation(float[] filtered, int[] labels)
    {
        int labelCounter = 1;

        // --- Step 1: Global Horizontal Projection ---
        float[] rowSum = new float[Height];
        for (int y = 0; y < Height; y++)
        {
            int rowIdx = y * Width;
            for (int x = 0; x < Width; x++)
            {
                rowSum[y] += filtered[rowIdx + x];
            }
        }

        List<int> horizontalSplits = FindValleys(rowSum, MarkerRadius);

        if (!horizontalSplits.Contains(0)) horizontalSplits.Insert(0, 0);
        if (!horizontalSplits.Contains(Height)) horizontalSplits.Add(Height);
        horizontalSplits.Sort();

        // --- Step 2: Local Vertical Projection per Row ---
        for (int i = 0; i < horizontalSplits.Count - 1; i++)
        {
            int yStart = horizontalSplits[i];
            int yEnd = horizontalSplits[i + 1];
            int rowHeight = yEnd - yStart;

            if (rowHeight <= 0) continue;

            float[] localColSum = new float[Width];
            for (int x = 0; x < Width; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    localColSum[x] += filtered[y * Width + x];
                }
            }

            List<int> verticalSplits = FindValleys(localColSum, MarkerRadius);

            if (!verticalSplits.Contains(0)) verticalSplits.Insert(0, 0);
            if (!verticalSplits.Contains(Width)) verticalSplits.Add(Width);
            verticalSplits.Sort();

            // --- Step 3: Fast Array Labeling ---
            for (int j = 0; j < verticalSplits.Count - 1; j++)
            {
                int xStart = verticalSplits[j];
                int xEnd = verticalSplits[j + 1];

                for (int y = yStart; y < yEnd; y++)
                {
                    int rowOffset = y * Width;
                    for (int x = xStart; x < xEnd; x++)
                    {
                        labels[rowOffset + x] = labelCounter;
                    }
                }
                labelCounter++;
            }
        }
        return labelCounter - 1;
    }

    private List<int> FindValleys(float[] profile, int radius)
    {
        List<int> valleys = new List<int>();
        for (int i = radius; i < profile.Length - radius; i++)
        {
            bool isMin = true;
            for (int j = -radius; j <= radius; j++)
            {
                if (j == 0) continue;
                if (profile[i + j] < profile[i]) // Falls ein Nachbar dunkler ist, kein Minimum
                {
                    isMin = false;
                    break;
                }
            }
            if (isMin) valleys.Add(i);
        }
        return valleys;
    }
}
