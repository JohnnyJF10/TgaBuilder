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

        // Fallback: if no seeds were found (e.g. image too small for MarkerRadius, or
        // all values are identical), return a single tile that covers every pixel so that
        // the selection pipeline has at least one segment to work with.
        // This check is placed before FinalFill to avoid a no-op pass over unlabeled pixels.
        if (seedCandidates.Count == 0)
        {
            for (int i = 0; i < labels.Length; i++)
                labels[i] = 1;
            return 1;
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




    private struct LineSegment
    {
        public int FixedCoord; // Y-coordinate for horizontal, X-coordinate for vertical
        public int Start;      // X-start for horizontal, Y-start for vertical
        public int End;        // X-end for horizontal, Y-end for vertical
    }

    private int OrthogonalLineSegmentation(float[] filtered, int[] labels)
    {
        Array.Clear(labels, 0, labels.Length);
        int labelCounter = 1;

        List<LineSegment> horizontalSegments = new List<LineSegment>();
        List<LineSegment> verticalSegments = new List<LineSegment>();

        // --- Step 1: Scan Row-by-Row for Horizontal Joints (Vertical Valleys) ---
        for (int y = 1; y < Height - 1; y++)
        {
            int rowIdx = y * Width;
            int segmentStart = -1;

            for (int x = 0; x < Width; x++)
            {
                bool isValley = true;
                float val = filtered[rowIdx + x];

                int yMin = Math.Max(0, y - MarkerRadius);
                int yMax = Math.Min(Height - 1, y + MarkerRadius);
                for (int ny = yMin; ny <= yMax; ny++)
                {
                    if (ny == y) continue;
                    if (filtered[ny * Width + x] < val)
                    {
                        isValley = false;
                        break;
                    }
                }

                if (isValley)
                {
                    if (segmentStart == -1) segmentStart = x;
                }
                else
                {
                    if (segmentStart != -1)
                    {
                        int length = x - segmentStart;
                        if (length >= MarkerRadius)
                        {
                            horizontalSegments.Add(new LineSegment { FixedCoord = y, Start = segmentStart, End = x - 1 });
                        }
                        segmentStart = -1;
                    }
                }
            }
            if (segmentStart != -1 && (Width - segmentStart) >= MarkerRadius)
            {
                horizontalSegments.Add(new LineSegment { FixedCoord = y, Start = segmentStart, End = Width - 1 });
            }
        }

        // --- Step 2: Scan Column-by-Column for Vertical Joints (Horizontal Valleys) ---
        for (int x = 1; x < Width - 1; x++)
        {
            int segmentStart = -1;

            for (int y = 0; y < Height; y++)
            {
                bool isValley = true;
                float val = filtered[y * Width + x];

                int xMin = Math.Max(0, x - MarkerRadius);
                int xMax = Math.Min(Width - 1, x + MarkerRadius);
                for (int nx = xMin; nx <= xMax; nx++)
                {
                    if (nx == x) continue;
                    if (filtered[y * Width + nx] < val)
                    {
                        isValley = false;
                        break;
                    }
                }

                if (isValley)
                {
                    if (segmentStart == -1) segmentStart = y;
                }
                else
                {
                    if (segmentStart != -1)
                    {
                        int length = y - segmentStart;
                        if (length >= MarkerRadius)
                        {
                            verticalSegments.Add(new LineSegment { FixedCoord = x, Start = segmentStart, End = y - 1 });
                        }
                        segmentStart = -1;
                    }
                }
            }
            if (segmentStart != -1 && (Height - segmentStart) >= MarkerRadius)
            {
                verticalSegments.Add(new LineSegment { FixedCoord = x, Start = segmentStart, End = Height - 1 });
            }
        }

        // --- Step 3: Connect & Artificially Extend Joints (Handling T-Intersections) ---
        for (int i = 0; i < horizontalSegments.Count; i++)
        {
            var h = horizontalSegments[i];

            int bestStart = h.Start;
            int minDistStart = MarkerRadius + 1;
            foreach (var v in verticalSegments)
            {
                if (Math.Abs(v.FixedCoord - h.Start) <= MarkerRadius && h.FixedCoord >= v.Start - MarkerRadius && h.FixedCoord <= v.End + MarkerRadius)
                {
                    int dist = Math.Abs(v.FixedCoord - h.Start);
                    if (dist < minDistStart) { minDistStart = dist; bestStart = v.FixedCoord; }
                }
            }
            h.Start = (minDistStart <= MarkerRadius) ? bestStart : ((h.Start <= MarkerRadius) ? 0 : Math.Max(0, h.Start - MarkerRadius));

            int bestEnd = h.End;
            int minDistEnd = MarkerRadius + 1;
            foreach (var v in verticalSegments)
            {
                if (Math.Abs(v.FixedCoord - h.End) <= MarkerRadius && h.FixedCoord >= v.Start - MarkerRadius && h.FixedCoord <= v.End + MarkerRadius)
                {
                    int dist = Math.Abs(v.FixedCoord - h.End);
                    if (dist < minDistEnd) { minDistEnd = dist; bestEnd = v.FixedCoord; }
                }
            }
            h.End = (minDistEnd <= MarkerRadius) ? bestEnd : ((h.End >= Width - 1 - MarkerRadius) ? Width - 1 : Math.Min(Width - 1, h.End + MarkerRadius));

            horizontalSegments[i] = h;
        }

        for (int i = 0; i < verticalSegments.Count; i++)
        {
            var v = verticalSegments[i];

            int bestStart = v.Start;
            int minDistStart = MarkerRadius + 1;
            foreach (var h in horizontalSegments)
            {
                if (Math.Abs(h.FixedCoord - v.Start) <= MarkerRadius && v.FixedCoord >= h.Start - MarkerRadius && v.FixedCoord <= h.End + MarkerRadius)
                {
                    int dist = Math.Abs(h.FixedCoord - v.Start);
                    if (dist < minDistStart) { minDistStart = dist; bestStart = h.FixedCoord; }
                }
            }
            v.Start = (minDistStart <= MarkerRadius) ? bestStart : ((v.Start <= MarkerRadius) ? 0 : Math.Max(0, v.Start - MarkerRadius));

            int bestEnd = v.End;
            int minDistEnd = MarkerRadius + 1;
            foreach (var h in horizontalSegments)
            {
                if (Math.Abs(h.FixedCoord - v.End) <= MarkerRadius && v.FixedCoord >= h.Start - MarkerRadius && v.FixedCoord <= h.End + MarkerRadius)
                {
                    int dist = Math.Abs(h.FixedCoord - v.End);
                    if (dist < minDistEnd) { minDistEnd = dist; bestEnd = h.FixedCoord; }
                }
            }
            v.End = (minDistEnd <= MarkerRadius) ? bestEnd : ((v.End >= Height - 1 - MarkerRadius) ? Height - 1 : Math.Min(Height - 1, v.End + MarkerRadius));

            verticalSegments[i] = v;
        }

        // --- Step 4: Construct a Structural Grid (Allowing Real Joints to Sclice Borders) ---
        byte[] jointType = new byte[Width * Height];
        const byte NONE = 0;
        const byte HORIZONTAL = 1;
        const byte VERTICAL = 2;
        const byte INTERSECTION = 3;
        const byte BORDER = 4;

        // Seed raw canvas frame edges
        for (int x = 0; x < Width; x++) { jointType[x] = BORDER; jointType[(Height - 1) * Width + x] = BORDER; }
        for (int y = 0; y < Height; y++) { jointType[y * Width] = BORDER; jointType[y * Width + (Width - 1)] = BORDER; }

        // Map horizontal joints (overwriting pure border strings where they terminate)
        foreach (var h in horizontalSegments)
        {
            for (int x = h.Start; x <= h.End; x++)
            {
                int idx = h.FixedCoord * Width + x;
                if (jointType[idx] == BORDER) jointType[idx] = HORIZONTAL;
                else jointType[idx] = (jointType[idx] == VERTICAL) ? INTERSECTION : HORIZONTAL;
            }
        }

        // Map vertical joints
        foreach (var v in verticalSegments)
        {
            for (int y = v.Start; y <= v.End; y++)
            {
                int idx = y * Width + v.FixedCoord;
                if (jointType[idx] == BORDER) jointType[idx] = VERTICAL;
                else jointType[idx] = (jointType[idx] == HORIZONTAL || jointType[idx] == INTERSECTION) ? INTERSECTION : VERTICAL;
            }
        }

        // --- Step 5: Flood-Fill Core Tile Blocks ---
        Queue<(int x, int y)> queue = new Queue<(int x, int y)>();
        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { -1, 1, 0, 0 };

        for (int y = 0; y < Height; y++)
        {
            int rowOffset = y * Width;
            for (int x = 0; x < Width; x++)
            {
                if (jointType[rowOffset + x] == NONE && labels[rowOffset + x] == 0)
                {
                    queue.Enqueue((x, y));
                    labels[rowOffset + x] = labelCounter;

                    while (queue.Count > 0)
                    {
                        var (cx, cy) = queue.Dequeue();

                        for (int d = 0; d < 4; d++)
                        {
                            int nx = cx + dx[d];
                            int ny = cy + dy[d];

                            if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                            {
                                int nIdx = ny * Width + nx;
                                if (jointType[nIdx] == NONE && labels[nIdx] == 0)
                                {
                                    labels[nIdx] = labelCounter;
                                    queue.Enqueue((nx, ny));
                                }
                            }
                        }
                    }
                    labelCounter++;
                }
            }
        }

        // --- Step 6: Inward-Aware Dilation (Eliminates Boundary Bleed / Antennas) ---
        bool changed = true;
        int pass = 0;

        while (changed && pass < 2)
        {
            changed = false;
            for (int y = 0; y < Height; y++)
            {
                int rowOffset = y * Width;
                for (int x = 0; x < Width; x++)
                {
                    int idx = rowOffset + x;
                    if (labels[idx] != 0) continue;

                    byte type = jointType[idx];
                    int chosenLabel = 0;

                    if (pass == 0)
                    {
                        if (type == VERTICAL)
                        {
                            if (x > 0 && labels[idx - 1] != 0) chosenLabel = labels[idx - 1];
                            else if (x < Width - 1 && labels[idx + 1] != 0) chosenLabel = labels[idx + 1];
                        }
                        else if (type == HORIZONTAL)
                        {
                            if (y > 0 && labels[idx - Width] != 0) chosenLabel = labels[idx - Width];
                            else if (y < Height - 1 && labels[idx + Width] != 0) chosenLabel = labels[idx + Width];
                        }
                        else if (type == BORDER)
                        {
                            // FIXED: Canvas frame borders look strictly INWARD to pull their true row/col label
                            if (x == 0 && labels[idx + 1] != 0) chosenLabel = labels[idx + 1];        // Left edge looks right
                            else if (x == Width - 1 && labels[idx - 1] != 0) chosenLabel = labels[idx - 1]; // Right edge looks left
                            else if (y == 0 && labels[idx + Width] != 0) chosenLabel = labels[idx + Width]; // Top edge looks down
                            else if (y == Height - 1 && labels[idx - Width] != 0) chosenLabel = labels[idx - Width]; // Bottom edge looks up
                        }
                        else if (type == INTERSECTION)
                        {
                            if (y > 0 && labels[idx - Width] != 0) chosenLabel = labels[idx - Width];
                            else if (y < Height - 1 && labels[idx + Width] != 0) chosenLabel = labels[idx + Width];
                            else if (x > 0 && labels[idx - 1] != 0) chosenLabel = labels[idx - 1];
                            else if (x < Width - 1 && labels[idx + 1] != 0) chosenLabel = labels[idx + 1];
                        }
                    }
                    else
                    {
                        // Pass 1: Global relaxation backup sweep
                        if (y > 0 && labels[idx - Width] != 0) chosenLabel = labels[idx - Width];
                        else if (y < Height - 1 && labels[idx + Width] != 0) chosenLabel = labels[idx + Width];
                        else if (x > 0 && labels[idx - 1] != 0) chosenLabel = labels[idx - 1];
                        else if (x < Width - 1 && labels[idx + 1] != 0) chosenLabel = labels[idx + 1];
                    }

                    if (chosenLabel != 0)
                    {
                        labels[idx] = chosenLabel;
                        changed = true;
                    }
                }
            }

            if (!changed && pass == 0)
            {
                pass = 1;
                changed = true;
            }
        }

        return labelCounter - 1;
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

    private int YXProjectionSegmentation(float[] filtered, int[] labels)
    {
        int labelCounter = 1;

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

        List<int> verticalSplits = FindValleys(colSum, MarkerRadius);

        if (!verticalSplits.Contains(0)) verticalSplits.Insert(0, 0);
        if (!verticalSplits.Contains(Width)) verticalSplits.Add(Width);
        verticalSplits.Sort();

        // --- Step 2: Local Horizontal Projection per Column Slice ---
        for (int i = 0; i < verticalSplits.Count - 1; i++)
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

            List<int> horizontalSplits = FindValleys(localRowSum, MarkerRadius);

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

    private List<int> FindValleys(float[] profile, int radius)
    {
        List<int> valleys = new List<int>();
        for (int i = radius; i < profile.Length - radius; i++)
        {
            bool isMin = true;
            for (int j = -radius; j <= radius; j++)
            {
                if (j == 0) continue;
                if (profile[i + j] < profile[i]) // If a neighbor is darker, it is not a minimum
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
