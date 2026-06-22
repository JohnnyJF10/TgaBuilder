namespace TrLynxLib.Transitions;

public partial class TransitionHelper
{
    private int Watershed(float[] filtered, int[] labels)
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


    private int BrickFit(float[] filtered, int[] labels, float angle = 0f)
    {
        float rad = angle * (float)Math.PI / 180f;
        float cosA = (float)Math.Cos(rad);
        float sinA = (float)Math.Sin(rad);

        // --- Calculate bounding box in the rotated coordinate system ---
        float[] cornersV = { 0, (Width - 1) * sinA, (Height - 1) * cosA, (Width - 1) * sinA + (Height - 1) * cosA };
        float[] cornersU = { 0, (Width - 1) * cosA, -(Height - 1) * sinA, (Width - 1) * cosA - (Height - 1) * sinA };

        float vMin = cornersV[0], vMax = cornersV[0];
        float uMin = cornersU[0], uMax = cornersU[0];
        for (int i = 1; i < 4; i++)
        {
            if (cornersV[i] < vMin) vMin = cornersV[i];
            if (cornersV[i] > vMax) vMax = cornersV[i];
            if (cornersU[i] < uMin) uMin = cornersU[i];
            if (cornersU[i] > uMax) uMax = cornersU[i];
        }

        int vLength = (int)Math.Ceiling(vMax - vMin) + 2;
        int uLength = (int)Math.Ceiling(uMax - uMin) + 2;

        // --- Step 1: Global Angled Projection & Precalculation ---
        int[] vBins = new int[Width * Height];
        int[] uBins = new int[Width * Height];
        float[] vSum = new float[vLength];
        int[] vCount = new int[vLength]; // Track pixel count per bin

        for (int y = 0; y < Height; y++)
        {
            int rowOffset = y * Width;
            for (int x = 0; x < Width; x++)
            {
                int idx = rowOffset + x;

                float v = x * sinA + y * cosA;
                float u = x * cosA - y * sinA;

                int vBin = (int)Math.Round(v - vMin);
                int uBin = (int)Math.Round(u - uMin);

                vBins[idx] = vBin;
                uBins[idx] = uBin;

                vSum[vBin] += filtered[idx];
                vCount[vBin]++;
            }
        }

        // Normalize primary projection (Average instead of Sum)
        for (int i = 0; i < vLength; i++)
        {
            if (vCount[i] > 0)
            {
                vSum[i] /= vCount[i];
            }
            else if (i > 0)
            {
                // Fill any micro-gaps caused by rotation rounding
                vSum[i] = vSum[i - 1];
            }
        }

        List<int> primarySplits = FindValleys(vSum, MarkerRadius);
        if (!primarySplits.Contains(0)) primarySplits.Insert(0, 0);
        if (!primarySplits.Contains(vLength)) primarySplits.Add(vLength);
        primarySplits.Sort();

        int numBands = primarySplits.Count - 1;

        int[] vBinToBand = new int[vLength];
        for (int i = 0; i < vLength; i++) vBinToBand[i] = -1;

        for (int i = 0; i < numBands; i++)
        {
            for (int v = primarySplits[i]; v < primarySplits[i + 1]; v++)
            {
                if (v >= 0 && v < vLength) vBinToBand[v] = i;
            }
        }

        // --- Step 2: Local Orthogonal Projection per Band ---
        float[][] uSums = new float[numBands][];
        int[][] uCounts = new int[numBands][];
        for (int i = 0; i < numBands; i++)
        {
            uSums[i] = new float[uLength];
            uCounts[i] = new int[uLength]; // Track local pixel count
        }

        for (int idx = 0; idx < filtered.Length; idx++)
        {
            int band = vBinToBand[vBins[idx]];
            if (band >= 0 && band < numBands)
            {
                uSums[band][uBins[idx]] += filtered[idx];
                uCounts[band][uBins[idx]]++;
            }
        }

        // --- Step 3: Find Secondary Splits and Map Labels ---
        int labelCounter = 1;
        int[][] uBinToLabelOffset = new int[numBands][];

        for (int i = 0; i < numBands; i++)
        {
            uBinToLabelOffset[i] = new int[uLength];

            if (primarySplits[i + 1] - primarySplits[i] <= 0) continue;

            // Normalize secondary projection (Average instead of Sum)
            for (int u = 0; u < uLength; u++)
            {
                if (uCounts[i][u] > 0)
                {
                    uSums[i][u] /= uCounts[i][u];
                }
                else if (u > 0)
                {
                    // Fill any micro-gaps
                    uSums[i][u] = uSums[i][u - 1];
                }
            }

            List<int> secSplits = FindValleys(uSums[i], MarkerRadius);
            if (!secSplits.Contains(0)) secSplits.Insert(0, 0);
            if (!secSplits.Contains(uLength)) secSplits.Add(uLength);
            secSplits.Sort();

            int numSegments = secSplits.Count - 1;

            for (int j = 0; j < numSegments; j++)
            {
                for (int u = secSplits[j]; u < secSplits[j + 1]; u++)
                {
                    if (u >= 0 && u < uLength)
                    {
                        uBinToLabelOffset[i][u] = labelCounter;
                    }
                }
                labelCounter++;
            }
        }

        // --- Step 4: Fast Array Labeling ---
        for (int idx = 0; idx < filtered.Length; idx++)
        {
            int band = vBinToBand[vBins[idx]];
            if (band >= 0 && band < numBands)
            {
                labels[idx] = uBinToLabelOffset[band][uBins[idx]];
            }
            else
            {
                labels[idx] = 0;
            }
        }

        // --- Step 5: Post-Processing Edge Clean-up and Island Dissolution ---
        int[] cleanLabels = (int[])labels.Clone();
        bool adjustmentsMade = false;

        // 8-neighborhood structural arrays
        int[] nX = { -1, 1, 0, 0, -1, 1, -1, 1 };
        int[] nY = { 0, 0, -1, 1, -1, -1, 1, 1 };
        Dictionary<int, int> neighborCounts = new Dictionary<int, int>();

        for (int y = 0; y < Height; y++)
        {
            int rowOffset = y * Width;
            for (int x = 0; x < Width; x++)
            {
                int idx = rowOffset + x;
                int currentLabel = labels[idx];

                // 1. Clear unassigned rounding artifact pixels (Label 0)
                if (currentLabel == 0)
                {
                    AssignToBestNeighborBrick(x, y, idx, labels, cleanLabels, nX, nY, neighborCounts);
                    adjustmentsMade = true;
                    continue;
                }

                // 2. Clear 1-Pixel wide boundary extensions and split edges 
                if (x == 0 || x == Width - 1 || y == 0 || y == Height - 1)
                {
                    int targetX = x == 0 ? 1 : (x == Width - 1 ? Width - 2 : x);
                    int targetY = y == 0 ? 1 : (y == Height - 1 ? Height - 2 : y);
                    int inwardIdx = targetY * Width + targetX;

                    if (labels[inwardIdx] != currentLabel && labels[inwardIdx] > 0)
                    {
                        cleanLabels[idx] = labels[inwardIdx];
                        adjustmentsMade = true;
                        continue;
                    }
                }

                // 3. Dissolve floating aliased islands (common around 45-degree bands)
                int matchingNeighbors = 0;
                int validNeighbors = 0;

                for (int i = 0; i < 8; i++)
                {
                    int nx = x + nX[i];
                    int ny = y + nY[i];

                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                    {
                        validNeighbors++;
                        if (labels[ny * Width + nx] == currentLabel)
                        {
                            matchingNeighbors++;
                        }
                    }
                }

                // If a pixel is disconnected/isolated from its core group structural neighbors
                if (matchingNeighbors <= 1 && validNeighbors > 0)
                {
                    AssignToBestNeighborBrick(x, y, idx, labels, cleanLabels, nX, nY, neighborCounts);
                    adjustmentsMade = true;
                }
            }
        }

        if (adjustmentsMade)
        {
            Array.Copy(cleanLabels, labels, labels.Length);
        }

        return labelCounter - 1;
    }

    // Helper utility to calculate local structural dominance in a 3x3 context matrix
    private void AssignToBestNeighborBrick(int x, int y, int idx, int[] sourceLabels, int[] destLabels, int[] nX, int[] nY, Dictionary<int, int> counts)
    {
        counts.Clear();
        for (int i = 0; i < 8; i++)
        {
            int nx = x + nX[i];
            int ny = y + nY[i];

            if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
            {
                int nLabel = sourceLabels[ny * Width + nx];
                if (nLabel > 0)
                {
                    if (counts.ContainsKey(nLabel)) counts[nLabel]++;
                    else counts[nLabel] = 1;
                }
            }
        }

        int bestLabel = sourceLabels[idx];
        int maxCount = 0;

        foreach (var pair in counts)
        {
            if (pair.Value > maxCount)
            {
                maxCount = pair.Value;
                bestLabel = pair.Key;
            }
        }

        destLabels[idx] = bestLabel;
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

    private int GridFit(float[] filtered, int[] labels, float angle)
    {
        float rad = angle * (float)Math.PI / 180f;
        float cosA = (float)Math.Cos(rad);
        float sinA = (float)Math.Sin(rad);

        // --- Calculate bounding box in the rotated coordinate system ---
        float[] cornersV = { 0, (Width - 1) * sinA, (Height - 1) * cosA, (Width - 1) * sinA + (Height - 1) * cosA };
        float[] cornersU = { 0, (Width - 1) * cosA, -(Height - 1) * sinA, (Width - 1) * cosA - (Height - 1) * sinA };

        float vMin = cornersV[0], vMax = cornersV[0];
        float uMin = cornersU[0], uMax = cornersU[0];
        for (int i = 1; i < 4; i++)
        {
            if (cornersV[i] < vMin) vMin = cornersV[i];
            if (cornersV[i] > vMax) vMax = cornersV[i];
            if (cornersU[i] < uMin) uMin = cornersU[i];
            if (cornersU[i] > uMax) uMax = cornersU[i];
        }

        int vLength = (int)Math.Ceiling(vMax - vMin) + 2;
        int uLength = (int)Math.Ceiling(uMax - uMin) + 2;

        // --- Step 1: Global Angled Projections ---
        int[] vBins = new int[Width * Height];
        int[] uBins = new int[Width * Height];
        float[] vSum = new float[vLength];
        float[] uSum = new float[uLength];
        int[] vCount = new int[vLength];
        int[] uCount = new int[uLength];
        float totalSum = 0;

        for (int y = 0; y < Height; y++)
        {
            int rowOffset = y * Width;
            for (int x = 0; x < Width; x++)
            {
                int idx = rowOffset + x;
                float val = filtered[idx];

                float v = x * sinA + y * cosA;
                float u = x * cosA - y * sinA;

                int vBin = (int)Math.Round(v - vMin);
                int uBin = (int)Math.Round(u - uMin);

                vBins[idx] = vBin;
                uBins[idx] = uBin;

                vSum[vBin] += val;
                vCount[vBin]++;

                uSum[uBin] += val;
                uCount[uBin]++;

                totalSum += val;
            }
        }

        float globalAvg = totalSum / (Width * Height);

        // Normalize projections (Average instead of Sum to flatten artificial slope)
        for (int i = 0; i < vLength; i++) { if (vCount[i] > 0) vSum[i] /= vCount[i]; else if (i > 0) vSum[i] = vSum[i - 1]; }
        for (int i = 0; i < uLength; i++) { if (uCount[i] > 0) uSum[i] /= uCount[i]; else if (i > 0) uSum[i] = uSum[i - 1]; }

        // Find valleys in normalized spaces
        List<int> vSplits = FindValleys(vSum, MarkerRadius);
        if (!vSplits.Contains(0)) vSplits.Insert(0, 0);
        if (!vSplits.Contains(vLength)) vSplits.Add(vLength);
        vSplits.Sort();

        List<int> uSplits = FindValleys(uSum, MarkerRadius);
        if (!uSplits.Contains(0)) uSplits.Insert(0, 0);
        if (!uSplits.Contains(uLength)) uSplits.Add(uLength);
        uSplits.Sort();

        int R = vSplits.Count - 1;
        int C = uSplits.Count - 1;

        if (R <= 0 || C <= 0) return 0; // Fallback for invalid dimensions

        // --- Step 2: Grid Setup, Fast Area, and Boundary Tracking ---
        int[] vBinToR = new int[vLength];
        int[] uBinToC = new int[uLength];
        Array.Fill(vBinToR, -1);
        Array.Fill(uBinToC, -1);

        for (int r = 0; r < R; r++)
            for (int v = vSplits[r]; v < vSplits[r + 1]; v++) if (v >= 0 && v < vLength) vBinToR[v] = r;

        for (int c = 0; c < C; c++)
            for (int u = uSplits[c]; u < uSplits[c + 1]; u++) if (u >= 0 && u < uLength) uBinToC[u] = c;

        // Fast O(1) lookup arrays for boundary pixel detection
        int[] vBinToHBound = new int[vLength];
        int[] uBinToVBound = new int[uLength];
        Array.Fill(vBinToHBound, -1);
        Array.Fill(uBinToVBound, -1);
        for (int r = 0; r < R - 1; r++) if (vSplits[r + 1] < vLength) vBinToHBound[vSplits[r + 1]] = r;
        for (int c = 0; c < C - 1; c++) if (uSplits[c + 1] < uLength) uBinToVBound[uSplits[c + 1]] = c;

        int[] parent = new int[R * C];
        int[] area = new int[R * C];
        for (int i = 0; i < R * C; i++) parent[i] = i;

        // Boundary trackers
        int numHBounds = (R > 0 ? R - 1 : 0) * C;
        int numVBounds = R * (C > 0 ? C - 1 : 0);
        float[] hBoundSum = new float[numHBounds];
        int[] hBoundCount = new int[numHBounds];
        float[] vBoundSum = new float[numVBounds];
        int[] vBoundCount = new int[numVBounds];

        // Single pass to strictly map real pixel areas and exact boundary accumulations
        for (int idx = 0; idx < filtered.Length; idx++)
        {
            int vBin = vBins[idx];
            int uBin = uBins[idx];
            int r = vBinToR[vBin];
            int c = uBinToC[uBin];

            float val = filtered[idx];

            // Tally exact internal area of the cell
            if (r >= 0 && r < R && c >= 0 && c < C) area[r * C + c]++;

            // Tally horizontal boundaries (evaluating merge between r and r+1)
            int hB = vBinToHBound[vBin];
            if (hB >= 0 && c >= 0 && c < C)
            {
                hBoundSum[hB * C + c] += val;
                hBoundCount[hB * C + c]++;
            }

            // Tally vertical boundaries (evaluating merge between c and c+1)
            int vB = uBinToVBound[uBin];
            if (vB >= 0 && r >= 0 && r < R)
            {
                vBoundSum[r * (C - 1) + vB] += val;
                vBoundCount[r * (C - 1) + vB]++;
            }
        }

        // Constraints
        float avgBaseArea = (Width * Height) / (float)(R * C);
        float maxAllowedArea = avgBaseArea * 4.5f;
        float minAllowedArea = maxAllowedArea / 5.5f;

        int Find(int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }
            return i;
        }

        bool Union(int i, int j)
        {
            int rootI = Find(i);
            int rootJ = Find(j);
            if (rootI != rootJ)
            {
                if (area[rootI] + area[rootJ] > maxAllowedArea) return false;
                parent[rootI] = rootJ;
                area[rootJ] += area[rootI];
                return true;
            }
            return false;
        }

        // --- Step 3: Evaluate Grid Segments (Boundaries) ---
        float jointThreshold = globalAvg * 0.98f;

        // Evaluate Horizontal Boundaries
        for (int r = 0; r < R - 1; r++)
        {
            for (int c = 0; c < C; c++)
            {
                int bIdx = r * C + c;
                if (hBoundCount[bIdx] > 0 && (hBoundSum[bIdx] / hBoundCount[bIdx]) >= jointThreshold)
                {
                    Union(r * C + c, (r + 1) * C + c);
                }
            }
        }

        // Evaluate Vertical Boundaries
        for (int c = 0; c < C - 1; c++)
        {
            for (int r = 0; r < R; r++)
            {
                int bIdx = r * (C - 1) + c;
                if (vBoundCount[bIdx] > 0 && (vBoundSum[bIdx] / vBoundCount[bIdx]) >= jointThreshold)
                {
                    Union(r * C + c, r * C + c + 1);
                }
            }
        }

        // --- Step 4: Cleanup Tiny Tiles (Force Merge) ---
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };
        bool changed = true;
        int maxPasses = 3;

        while (changed && maxPasses-- > 0)
        {
            changed = false;
            for (int r = 0; r < R; r++)
            {
                for (int c = 0; c < C; c++)
                {
                    int id = r * C + c;
                    int root = Find(id);

                    if (area[root] < minAllowedArea)
                    {
                        int bestNeighborRoot = -1;
                        int bestNeighborArea = int.MaxValue;

                        for (int i = 0; i < 4; i++)
                        {
                            int nr = r + dr[i];
                            int nc = c + dc[i];
                            if (nr >= 0 && nr < R && nc >= 0 && nc < C)
                            {
                                int nRoot = Find(nr * C + nc);
                                if (nRoot != root)
                                {
                                    if (area[root] + area[nRoot] <= maxAllowedArea && area[nRoot] < bestNeighborArea)
                                    {
                                        bestNeighborArea = area[nRoot];
                                        bestNeighborRoot = nRoot;
                                    }
                                }
                            }
                        }

                        if (bestNeighborRoot == -1)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                int nr = r + dr[i];
                                int nc = c + dc[i];
                                if (nr >= 0 && nr < R && nc >= 0 && nc < C)
                                {
                                    int nRoot = Find(nr * C + nc);
                                    if (nRoot != root && area[nRoot] < bestNeighborArea)
                                    {
                                        bestNeighborArea = area[nRoot];
                                        bestNeighborRoot = nRoot;
                                    }
                                }
                            }
                        }

                        if (bestNeighborRoot != -1)
                        {
                            parent[root] = bestNeighborRoot;
                            area[bestNeighborRoot] += area[root];
                            changed = true;
                        }
                    }
                }
            }
        }

        // --- Step 5: Map Merged Cells to Final Labels Array ---
        Dictionary<int, int> rootToLabel = new Dictionary<int, int>();
        int labelCounter = 1;

        for (int idx = 0; idx < filtered.Length; idx++)
        {
            int r = vBinToR[vBins[idx]];
            int c = uBinToC[uBins[idx]];

            if (r >= 0 && r < R && c >= 0 && c < C)
            {
                int finalRoot = Find(r * C + c);
                if (!rootToLabel.ContainsKey(finalRoot))
                {
                    rootToLabel[finalRoot] = labelCounter++;
                }
                labels[idx] = rootToLabel[finalRoot];
            }
            else
            {
                labels[idx] = 0;
            }
        }

        // --- Step 6: Edge Cleanup and Island Dissolution Pass ---
        // We use a temporary buffer for the clean-up to avoid race conditions during the check
        int[] cleanLabels = (int[])labels.Clone();
        bool adjustmentsMade = false;

        // 8-neighborhood offsets to check for structural dominance
        int[] nX = { -1, 1, 0, 0, -1, 1, -1, 1 };
        int[] nY = { 0, 0, -1, 1, -1, -1, 1, 1 };

        // We track neighbor label frequencies using a fixed-size array or small dictionary
        Dictionary<int, int> neighborCounts = new Dictionary<int, int>();

        for (int y = 0; y < Height; y++)
        {
            int rowOffset = y * Width;
            for (int x = 0; x < Width; x++)
            {
                int idx = rowOffset + x;
                int currentLabel = labels[idx];

                // 1. Handle Unassigned/Padding Pixels (Label 0)
                // If rotation math left a pixel unassigned at the extreme boundary, force-merge it
                if (currentLabel == 0)
                {
                    AssignToBestNeighbor(x, y, idx, labels, cleanLabels, nX, nY, neighborCounts);
                    adjustmentsMade = true;
                    continue;
                }

                // 2. Handle 1-Pixel Thin Edges and Border Extensions
                // If we are on a texture edge, prioritize absorbing into the inward structure
                if (x == 0 || x == Width - 1 || y == 0 || y == Height - 1)
                {
                    int targetX = x == 0 ? 1 : (x == Width - 1 ? Width - 2 : x);
                    int targetY = y == 0 ? 1 : (y == Height - 1 ? Height - 2 : y);
                    int inwardIdx = targetY * Width + targetX;

                    if (labels[inwardIdx] != currentLabel && labels[inwardIdx] > 0)
                    {
                        cleanLabels[idx] = labels[inwardIdx];
                        adjustmentsMade = true;
                        continue;
                    }
                }

                // 3. Handle Disconnected 45-Degree Islands
                // Count how many matching neighbors this pixel has. If it's completely lonely, dissolve it.
                int matchingNeighbors = 0;
                int validNeighbors = 0;

                for (int i = 0; i < 8; i++)
                {
                    int nx = x + nX[i];
                    int ny = y + nY[i];

                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                    {
                        validNeighbors++;
                        if (labels[ny * Width + nx] == currentLabel)
                        {
                            matchingNeighbors++;
                        }
                    }
                }

                // If a pixel shares its identity with less than 25% of its valid neighborhood, 
                // it's an artifact island and needs to be re-allocated.
                if (matchingNeighbors <= 1 && validNeighbors > 0)
                {
                    AssignToBestNeighbor(x, y, idx, labels, cleanLabels, nX, nY, neighborCounts);
                    adjustmentsMade = true;
                }
            }
        }

        // Write back cleaned labels if modifications occurred
        if (adjustmentsMade)
        {
            Array.Copy(cleanLabels, labels, labels.Length);
        }

        return labelCounter - 1;
    }

    // Helper method to find the most prominent valid label in a 3x3 window
    private void AssignToBestNeighbor(int x, int y, int idx, int[] sourceLabels, int[] destLabels, int[] nX, int[] nY, Dictionary<int, int> counts)
    {
        counts.Clear();
        for (int i = 0; i < 8; i++)
        {
            int nx = x + nX[i];
            int ny = y + nY[i];

            if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
            {
                int nLabel = sourceLabels[ny * Width + nx];
                if (nLabel > 0) // Only count actual structural segments
                {
                    if (counts.ContainsKey(nLabel)) counts[nLabel]++;
                    else counts[nLabel] = 1;
                }
            }
        }

        int bestLabel = sourceLabels[idx];
        int maxCount = 0;

        foreach (var pair in counts)
        {
            if (pair.Value > maxCount)
            {
                maxCount = pair.Value;
                bestLabel = pair.Key;
            }
        }

        destLabels[idx] = bestLabel;
    }
}
