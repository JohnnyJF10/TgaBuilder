namespace TrLynxLib.Transitions;

public partial class TransitionHelper
{
    private int Felzenszwalb(byte[] pixels, int[] labels, int min_size = 50, float scale = 100f)
    {
        int totalPixels = Width * Height;
        if (totalPixels == 0 || pixels.Length < totalPixels * TRANSITIONS_BPP || labels.Length != totalPixels)
            return 0;

        min_size = Math.Max(1, min_size);
        scale = Math.Max(1f, scale);

        Array.Clear(labels, 0, labels.Length);

        int edgeCapacity = Math.Max(0, (Width - 1) * Height + Width * (Height - 1));
        var edges = new List<(int A, int B, float W)>(edgeCapacity);

        for (int y = 0; y < Height; y++)
        {
            int row = y * Width;
            for (int x = 0; x < Width; x++)
            {
                int i = row + x;
                if (x + 1 < Width)
                {
                    int right = i + 1;
                    edges.Add((i, right, ColorDistanceSquared(pixels, i, right)));
                }

                if (y + 1 < Height)
                {
                    int down = i + Width;
                    edges.Add((i, down, ColorDistanceSquared(pixels, i, down)));
                }
            }
        }

        edges.Sort((a, b) => a.W.CompareTo(b.W));

        int[] parent = new int[totalPixels];
        int[] size = new int[totalPixels];
        float[] internalDifference = new float[totalPixels];

        for (int i = 0; i < totalPixels; i++)
        {
            parent[i] = i;
            size[i] = 1;
            internalDifference[i] = 0f;
        }

        foreach (var edge in edges)
        {
            int ra = FindRoot(parent, edge.A);
            int rb = FindRoot(parent, edge.B);
            if (ra == rb)
                continue;

            float thresholdA = internalDifference[ra] + (scale / size[ra]);
            float thresholdB = internalDifference[rb] + (scale / size[rb]);

            if (edge.W <= thresholdA && edge.W <= thresholdB)
            {
                int merged = UnionSets(parent, size, ra, rb);
                internalDifference[merged] = Math.Max(edge.W, Math.Max(internalDifference[ra], internalDifference[rb]));
            }
        }

        foreach (var edge in edges)
        {
            int ra = FindRoot(parent, edge.A);
            int rb = FindRoot(parent, edge.B);
            if (ra == rb)
                continue;

            if (size[ra] < min_size || size[rb] < min_size)
            {
                int merged = UnionSets(parent, size, ra, rb);
                internalDifference[merged] = Math.Max(edge.W, Math.Max(internalDifference[ra], internalDifference[rb]));
            }
        }

        var labelByRoot = new Dictionary<int, int>();
        int labelCounter = 0;
        for (int i = 0; i < totalPixels; i++)
        {
            int root = FindRoot(parent, i);
            if (!labelByRoot.TryGetValue(root, out int label))
            {
                label = ++labelCounter;
                labelByRoot[root] = label;
            }

            labels[i] = label;
        }

        return labelCounter;
    }

    private int Slic(byte[] pixels, int[] labels, int n_segments = 250, float compactness = 10f)
    {
        int totalPixels = Width * Height;
        if (totalPixels == 0 || pixels.Length < totalPixels * TRANSITIONS_BPP || labels.Length != totalPixels)
            return 0;

        n_segments = Math.Clamp(n_segments, 1, totalPixels);
        compactness = Math.Max(0.001f, compactness);
        Array.Fill(labels, -1);

        float step = MathF.Sqrt(totalPixels / (float)n_segments);
        if (step < 1f)
            step = 1f;

        int gridStep = Math.Max(1, (int)MathF.Round(step));
        int start = gridStep / 2;

        var centers = new List<SlicCenter>(n_segments);
        for (int y = start; y < Height; y += gridStep)
        {
            for (int x = start; x < Width; x += gridStep)
            {
                int idx = y * Width + x;
                int p = idx * TRANSITIONS_BPP;
                centers.Add(new SlicCenter(x, y, pixels[p], pixels[p + 1], pixels[p + 2]));
            }
        }

        if (centers.Count == 0)
        {
            int idx = (Height / 2) * Width + (Width / 2);
            int p = idx * TRANSITIONS_BPP;
            centers.Add(new SlicCenter(Width / 2f, Height / 2f, pixels[p], pixels[p + 1], pixels[p + 2]));
        }

        float[] distances = new float[totalPixels];
        float invStepSq = 1f / (step * step);
        float compactnessSq = compactness * compactness;

        for (int iteration = 0; iteration < 5; iteration++)
        {
            Array.Fill(distances, float.MaxValue);

            for (int ci = 0; ci < centers.Count; ci++)
            {
                SlicCenter center = centers[ci];
                int minX = Math.Max(0, (int)(center.X - 2f * step));
                int maxX = Math.Min(Width - 1, (int)(center.X + 2f * step));
                int minY = Math.Max(0, (int)(center.Y - 2f * step));
                int maxY = Math.Min(Height - 1, (int)(center.Y + 2f * step));

                for (int y = minY; y <= maxY; y++)
                {
                    int row = y * Width;
                    for (int x = minX; x <= maxX; x++)
                    {
                        int idx = row + x;
                        int p = idx * TRANSITIONS_BPP;

                        float db = pixels[p] - center.B;
                        float dg = pixels[p + 1] - center.G;
                        float dr = pixels[p + 2] - center.R;
                        float colorDistanceSq = db * db + dg * dg + dr * dr;

                        float dx = x - center.X;
                        float dy = y - center.Y;
                        float spatialDistanceSq = dx * dx + dy * dy;

                        float distance = colorDistanceSq + (compactnessSq * spatialDistanceSq * invStepSq);
                        if (distance < distances[idx])
                        {
                            distances[idx] = distance;
                            labels[idx] = ci;
                        }
                    }
                }
            }

            float[] sumX = new float[centers.Count];
            float[] sumY = new float[centers.Count];
            float[] sumB = new float[centers.Count];
            float[] sumG = new float[centers.Count];
            float[] sumR = new float[centers.Count];
            int[] counts = new int[centers.Count];

            for (int idx = 0; idx < totalPixels; idx++)
            {
                int ci = labels[idx];
                if (ci < 0 || ci >= centers.Count)
                    continue;

                int x = idx % Width;
                int y = idx / Width;
                int p = idx * TRANSITIONS_BPP;

                sumX[ci] += x;
                sumY[ci] += y;
                sumB[ci] += pixels[p];
                sumG[ci] += pixels[p + 1];
                sumR[ci] += pixels[p + 2];
                counts[ci]++;
            }

            for (int ci = 0; ci < centers.Count; ci++)
            {
                if (counts[ci] <= 0)
                    continue;

                float inv = 1f / counts[ci];
                centers[ci] = new SlicCenter(
                    sumX[ci] * inv,
                    sumY[ci] * inv,
                    sumB[ci] * inv,
                    sumG[ci] * inv,
                    sumR[ci] * inv);
            }
        }

        // 1. Shift labels from [0...centers.Count-1] to [1...centers.Count]
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i] = Math.Max(0, labels[i]) + 1;
        }

        int currentMaxLabel = centers.Count;

        // 2. Post-processing: Isolate disconnected edge components
        currentMaxLabel = IsolateDisconnectedEdgeLabels(labels, currentMaxLabel);

        return currentMaxLabel;
    }

    private int Quickshift(byte[] pixels, int[] labels, int max_dist = 10, float ratio = 1f)
    {
        int totalPixels = Width * Height;
        if (totalPixels == 0 || pixels.Length < totalPixels * TRANSITIONS_BPP || labels.Length != totalPixels)
            return 0;

        max_dist = Math.Max(1, max_dist);
        ratio = Math.Max(0.1f, ratio);
        Array.Clear(labels, 0, labels.Length);

        int[] queue = new int[totalPixels];
        int label = 0;
        float colorThreshold = MathF.Max(4f, 25f * ratio);
        float colorThresholdSq = colorThreshold * colorThreshold;

        for (int seed = 0; seed < totalPixels; seed++)
        {
            if (labels[seed] != 0)
                continue;

            int seedX = seed % Width;
            int seedY = seed / Width;
            int seedPixel = seed * TRANSITIONS_BPP;

            float sumB = pixels[seedPixel];
            float sumG = pixels[seedPixel + 1];
            float sumR = pixels[seedPixel + 2];
            int count = 1;

            label++;
            labels[seed] = label;

            int head = 0;
            int tail = 0;
            queue[tail++] = seed;

            while (head < tail)
            {
                int current = queue[head++];
                int x = current % Width;
                int y = current / Width;

                TryGrowQuickshiftRegion(x - 1, y, seedX, seedY, max_dist, label, pixels, labels, queue, ref tail, ref sumB, ref sumG, ref sumR, ref count, colorThresholdSq);
                TryGrowQuickshiftRegion(x + 1, y, seedX, seedY, max_dist, label, pixels, labels, queue, ref tail, ref sumB, ref sumG, ref sumR, ref count, colorThresholdSq);
                TryGrowQuickshiftRegion(x, y - 1, seedX, seedY, max_dist, label, pixels, labels, queue, ref tail, ref sumB, ref sumG, ref sumR, ref count, colorThresholdSq);
                TryGrowQuickshiftRegion(x, y + 1, seedX, seedY, max_dist, label, pixels, labels, queue, ref tail, ref sumB, ref sumG, ref sumR, ref count, colorThresholdSq);
            }
        }

        label = IsolateDisconnectedEdgeLabels(labels, label);

        return label;
    }

    private int IsolateDisconnectedEdgeLabels(int[] labels, int maxLabel)
    {
        int totalPixels = Width * Height;

        // Track total pixel counts for every label to detect if a label is split/disconnected
        int[] labelCounts = new int[maxLabel + 1];
        for (int i = 0; i < totalPixels; i++)
        {
            if (labels[i] <= maxLabel)
                labelCounts[labels[i]]++;
        }

        // A fast way to find neighbors during a FloodFill
        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { -1, 1, 0, 0 };

        bool[] visited = new bool[totalPixels];
        var queue = new Queue<int>();
        var currentComponent = new List<int>();

        // Helper to process a specific edge pixel index
        void ProcessEdgePixel(int edgeIdx)
        {
            if (visited[edgeIdx]) return;

            int targetLabel = labels[edgeIdx];

            // If this label only has 1 pixel total in the whole image, it can't be "disconnected" from anything else.
            if (labelCounts[targetLabel] <= 1)
            {
                visited[edgeIdx] = true;
                return;
            }

            // Run FloodFill to find the entire connected component sharing this label
            currentComponent.Clear();
            queue.Enqueue(edgeIdx);
            visited[edgeIdx] = true;

            while (queue.Count > 0)
            {
                int currIdx = queue.Dequeue();
                currentComponent.Add(currIdx);

                int cx = currIdx % Width;
                int cy = currIdx / Width;

                for (int i = 0; i < 4; i++)
                {
                    int nx = cx + dx[i];
                    int ny = cy + dy[i];

                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                    {
                        int nextIdx = ny * Width + nx;
                        if (!visited[nextIdx] && labels[nextIdx] == targetLabel)
                        {
                            visited[nextIdx] = true;
                            queue.Enqueue(nextIdx);
                        }
                    }
                }
            }

            // CRITICAL CHECK: If the connected clump we just found is smaller than the total
            // global pixel count for this label, it means there are disconnected pieces elsewhere!
            if (currentComponent.Count < labelCounts[targetLabel])
            {
                maxLabel++; // Allocate a brand-new unique label

                foreach (int idx in currentComponent)
                {
                    labels[idx] = maxLabel;
                }

                // Subtract the elements we just isolated from the original global count
                labelCounts[targetLabel] -= currentComponent.Count;
            }
        }

        // --- Scan Top and Bottom Edges ---
        for (int x = 0; x < Width; x++)
        {
            ProcessEdgePixel(x);                  // Top row (y = 0)
            ProcessEdgePixel((Height - 1) * Width + x); // Bottom row (y = Height - 1)
        }

        // --- Scan Left and Right Edges ---
        for (int y = 0; y < Height; y++)
        {
            ProcessEdgePixel(y * Width);             // Left column (x = 0)
            ProcessEdgePixel(y * Width + (Width - 1)); // Right column (x = Width - 1)
        }

        return maxLabel;
    }

    private void TryGrowQuickshiftRegion(
        int x,
        int y,
        int seedX,
        int seedY,
        int maxDist,
        int label,
        byte[] pixels,
        int[] labels,
        int[] queue,
        ref int queueTail,
        ref float sumB,
        ref float sumG,
        ref float sumR,
        ref int count,
        float colorThresholdSq)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        if (Math.Abs(x - seedX) > maxDist || Math.Abs(y - seedY) > maxDist)
            return;

        int idx = y * Width + x;
        if (labels[idx] != 0)
            return;

        int p = idx * TRANSITIONS_BPP;
        float meanB = sumB / count;
        float meanG = sumG / count;
        float meanR = sumR / count;

        float db = pixels[p] - meanB;
        float dg = pixels[p + 1] - meanG;
        float dr = pixels[p + 2] - meanR;
        float colorDistanceSq = db * db + dg * dg + dr * dr;

        if (colorDistanceSq > colorThresholdSq)
            return;

        labels[idx] = label;
        queue[queueTail++] = idx;
        sumB += pixels[p];
        sumG += pixels[p + 1];
        sumR += pixels[p + 2];
        count++;
    }

    private static float ColorDistanceSquared(byte[] pixels, int i, int j)
    {
        int p1 = i * TRANSITIONS_BPP;
        int p2 = j * TRANSITIONS_BPP;

        float db = pixels[p1] - pixels[p2];
        float dg = pixels[p1 + 1] - pixels[p2 + 1];
        float dr = pixels[p1 + 2] - pixels[p2 + 2];

        return db * db + dg * dg + dr * dr;
    }

    private static int FindRoot(int[] parent, int value)
    {
        while (parent[value] != value)
        {
            parent[value] = parent[parent[value]];
            value = parent[value];
        }

        return value;
    }

    private static int UnionSets(int[] parent, int[] size, int a, int b)
    {
        if (a == b)
            return a;

        if (size[a] < size[b])
            (a, b) = (b, a);

        parent[b] = a;
        size[a] += size[b];
        return a;
    }

    private readonly struct SlicCenter
    {
        public SlicCenter(float x, float y, float b, float g, float r)
        {
            X = x;
            Y = y;
            B = b;
            G = g;
            R = r;
        }

        public float X { get; }
        public float Y { get; }
        public float B { get; }
        public float G { get; }
        public float R { get; }
    }
}
