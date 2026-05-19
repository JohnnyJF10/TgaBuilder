using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{
    private int WatershedSegmentation(float[] filtered, int[] labels)
    {
        // 1. Find ALL local maxima using a tight 3x3 neighborhood (radius = 1)
        var seedCandidates = new List<(int idx, float val)>(256);

        // Stay 1 pixel away from edges to safely read the 3x3 neighborhood
        for (int y = 1; y < Height - 1; y++)
        {
            int row = y * Width;
            for (int x = 1; x < Width - 1; x++)
            {
                int idx = row + x;
                float val = filtered[idx];
                bool isMax = true;

                // Check the 8 surrounding neighbors
                for (int iy = -1; iy <= 1; iy++)
                {
                    int nRow = (y + iy) * Width;
                    for (int ix = -1; ix <= 1; ix++)
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
                {
                    seedCandidates.Add((idx, val));
                }
            }
        }

        // Fallback: If no seeds were found at all, handle it early
        if (seedCandidates.Count == 0)
        {
            for (int i = 0; i < labels.Length; i++)
                labels[i] = 1;
            return 1;
        }

        // 2. Sort candidates by intensity (highest peaks first) and truncate to MarkerCount
        // This gives you the 1 - 500 adjustment range you are looking for.
        var topSeeds = seedCandidates
            .OrderByDescending(s => s.val)
            .Take(MarkerCount)
            .ToList();

        // 3. Initialize Intensity Buckets
        Queue<int>[] buckets = new Queue<int>[256];
        for (int i = 0; i < 256; i++)
            buckets[i] = new Queue<int>(32);

        // 4. Label the top seeds and queue their neighbors
        for (int i = 0; i < topSeeds.Count; i++)
        {
            int label = i + 1;
            int idx = topSeeds[i].idx;

            labels[idx] = label;
            EnqueueNeighbors(idx, label, labels, filtered, buckets, 255);
        }

        // 5. Watershed flood expansion
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
                EnqueueNeighbors(idx, label, labels, filtered, buckets, b);
            }
        }

        // 6. Clean up unlabelled gaps
        FinalFill(labels);

        return topSeeds.Count;
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
        int[] neighbors = { idx - Width, idx + Width, idx - 1, idx + 1 };
        for (int i = 0; i < 4; i++)
        {
            int nIdx = neighbors[i];
            if (nIdx >= 0 && nIdx < labels.Length)
            {
                if (i >= 2 && (nIdx / Width != idx / Width)) continue;

                if (labels[nIdx] == 0)
                {
                    int b = (int)blur[nIdx];
                    if (b > maxB) b = maxB;
                    if (b < 0) b = 0; // Guard against negative blur values
                    buckets[b].Enqueue(nIdx);
                }
            }
        }
    }


    private int XYProjectionSegmentation(float[] filtered, int[] labels)
    {
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

        // Find all raw local minima/valleys on the horizontal axis
        var globalHorizontalValleys = FindValleysWithProminence(rowSum);

        // Calculate how many total horizontal cuts we should ideally make.
        // For XY, the global horizontal cut is the primary division. We estimate the split distribution.
        // We want H_segments * V_segments ≈ MarkerCount. Let's find an optimal balance.
        int idealHsegments = (int)Math.Max(1, Math.Round(Math.Sqrt(MarkerCount * (double)Height / Width)));
        int targetHValleys = Math.Max(0, idealHsegments - 1);

        // Take the most prominent horizontal valleys
        List<int> horizontalSplits = globalHorizontalValleys
            .OrderByDescending(v => v.prominence)
            .Take(targetHValleys)
            .Select(v => v.index)
            .ToList();

        if (!horizontalSplits.Contains(0)) horizontalSplits.Insert(0, 0);
        if (!horizontalSplits.Contains(Height)) horizontalSplits.Add(Height);
        horizontalSplits.Sort();

        int actualRows = horizontalSplits.Count - 1;
        int labelCounter = 1;

        // --- Step 2: Local Vertical Projection per Row ---
        for (int i = 0; i < actualRows; i++)
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

            var localVerticalValleys = FindValleysWithProminence(localColSum);

            // Dynamically figure out how many columns this specific row needs 
            // to help hit the global MarkerCount goal as closely as possible.
            int targetVsegments = (int)Math.Max(1, Math.Round((double)MarkerCount / actualRows));
            int targetVValleys = Math.Max(0, targetVsegments - 1);

            List<int> verticalSplits = localVerticalValleys
                .OrderByDescending(v => v.prominence)
                .Take(targetVValleys)
                .Select(v => v.index)
                .ToList();

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

    private int YXProjectionSegmentation(float[] filtered, int[] labels)
    {
        // --- Step 1: Global Vertical Projection ---
        float[] colSum = new float[Width];
        for (int y = 0; y < Height; y++)
        {
            int rowIdx = y * Width;
            for (int x = 0; x < Width; x++)
            {
                colSum[x] += filtered[rowIdx + x];
            }
        }

        var globalVerticalValleys = FindValleysWithProminence(colSum);

        // Estimate baseline column counts based on aspect ratio to distribute markers evenly
        int idealVsegments = (int)Math.Max(1, Math.Round(Math.Sqrt(MarkerCount * (double)Width / Height)));
        int targetVValleys = Math.Max(0, idealVsegments - 1);

        List<int> verticalSplits = globalVerticalValleys
            .OrderByDescending(v => v.prominence)
            .Take(targetVValleys)
            .Select(v => v.index)
            .ToList();

        if (!verticalSplits.Contains(0)) verticalSplits.Insert(0, 0);
        if (!verticalSplits.Contains(Width)) verticalSplits.Add(Width);
        verticalSplits.Sort();

        int actualCols = verticalSplits.Count - 1;
        int labelCounter = 1;

        // --- Step 2: Local Horizontal Projection per Column ---
        for (int i = 0; i < actualCols; i++)
        {
            int xStart = verticalSplits[i];
            int xEnd = verticalSplits[i + 1];
            int colWidth = xEnd - xStart;

            if (colWidth <= 0) continue;

            float[] localRowSum = new float[Height];
            for (int y = 0; y < Height; y++)
            {
                int rowIdx = y * Width;
                for (int x = xStart; x < xEnd; x++)
                {
                    localRowSum[y] += filtered[rowIdx + x];
                }
            }

            var localHorizontalValleys = FindValleysWithProminence(localRowSum);

            int targetHsegments = (int)Math.Max(1, Math.Round((double)MarkerCount / actualCols));
            int targetHValleys = Math.Max(0, targetHsegments - 1);

            List<int> horizontalSplits = localHorizontalValleys
                .OrderByDescending(v => v.prominence)
                .Take(targetHValleys)
                .Select(v => v.index)
                .ToList();

            if (!horizontalSplits.Contains(0)) horizontalSplits.Insert(0, 0);
            if (!horizontalSplits.Contains(Height)) horizontalSplits.Add(Height);
            horizontalSplits.Sort();

            // --- Step 3: Fast Array Labeling ---
            for (int j = 0; j < horizontalSplits.Count - 1; j++)
            {
                int yStart = horizontalSplits[j];
                int yEnd = horizontalSplits[j + 1];

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

    // Finds local minima using a tight radius=1, and measures valley depth 
    // (prominence) relative to its immediate neighboring peaks.
    private List<(int index, float prominence)> FindValleysWithProminence(float[] profile)
    {
        var valleys = new List<(int index, float prominence)>();
        if (profile.Length < 3) return valleys;

        for (int i = 1; i < profile.Length - 1; i++)
        {
            // Check if it's a raw local minimum (radius = 1)
            if (profile[i] <= profile[i - 1] && profile[i] <= profile[i + 1])
            {
                if (profile[i] == profile[i - 1] && profile[i] == profile[i + 1])
                    continue; // Skip flat plateaus to avoid spamming splits

                // Find nearest peak or boundary to the left
                float leftPeak = profile[i];
                for (int l = i - 1; l >= 0; l--)
                {
                    if (profile[l] > leftPeak) leftPeak = profile[l];
                    else if (l < i - 1 && profile[l] < profile[l + 1]) break; // Hit another valley
                }

                // Find nearest peak or boundary to the right
                float rightPeak = profile[i];
                for (int r = i + 1; r < profile.Length; r++)
                {
                    if (profile[r] > rightPeak) rightPeak = profile[r];
                    else if (r > i + 1 && profile[r] < profile[r - 1]) break; // Hit another valley
                }

                // Prominence represents how "deep" this drop-off is compared to its surroundings
                float prominence = Math.Min(leftPeak, rightPeak) - profile[i];
                valleys.Add((i, prominence));
            }
        }
        return valleys;
    }
}
