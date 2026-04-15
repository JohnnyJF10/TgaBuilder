using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Transitions
{
    public partial class TransitionHelper
    {
        // Runs a watershed-style tile analysis and builds labels, centroids, and a debug map.
        public unsafe void AnalyzeTilesWatershed(byte[] pixels)
        {
            int totalPixels = Width * Height;

            float[] blur = new float[totalPixels];
            int[] labels = new int[totalPixels];

            // 1. Compute grayscale values
            float[] gray = new float[totalPixels];

            fixed (byte* p = pixels)
            {
                for (int i = 0; i < totalPixels; i++)
                {
                    byte* px = p + (i * Bpp);
                    gray[i] = px[2] * 0.299f + px[1] * 0.587f + px[0] * 0.114f;
                }
            }

            // 2. Box blur
            for (int y = 1; y < Height - 1; y++)
            {
                int row = y * Width;
                for (int x = 1; x < Width - 1; x++)
                {
                    int idx = row + x;
                    blur[idx] =
                        (gray[idx - Width - 1] + gray[idx - Width] + gray[idx - Width + 1] +
                         gray[idx - 1] + gray[idx] + gray[idx + 1] +
                         gray[idx + Width - 1] + gray[idx + Width] + gray[idx + Width + 1]) / 9f;
                }
            }

            // 3. Seed candidates (keep all valid local maxima)
            var seedCandidates = new List<(int idx, float val)>(128);

            for (int y = MarkerRadius; y < Height - MarkerRadius; y++)
            {
                int row = y * Width;
                for (int x = MarkerRadius; x < Width - MarkerRadius; x++)
                {
                    int idx = row + x;
                    float val = blur[idx];
                    bool isMax = true;

                    for (int iy = -MarkerRadius; iy <= MarkerRadius; iy++)
                    {
                        int nRow = (y + iy) * Width;
                        for (int ix = -MarkerRadius; ix <= MarkerRadius; ix++)
                        {
                            if (ix == 0 && iy == 0) continue;
                            if (blur[nRow + x + ix] >= val)
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

            // NOTE: The previous seed trimming was removed here
            // because it destroys topology. We regulate count during post-processing.

            // 4. Buckets
            Queue<int>[] buckets = new Queue<int>[256];
            for (int i = 0; i < 256; i++)
                buckets[i] = new Queue<int>(32);

            List<TileSegment> tiles = new List<TileSegment>(seedCandidates.Count);

            for (int i = 0; i < seedCandidates.Count; i++)
            {
                int label = i + 1;
                int idx = seedCandidates[i].idx;

                labels[idx] = label;

                TileSegment ts = new TileSegment();
                tiles.Add(ts);

                int x = idx % Width;
                int y = idx / Width;

                AddPixelToTile(ts, x, y);
                EnqueueNeighbors(idx, label, labels, blur, buckets, 255);
            }

            if (tiles.Count == 0)
            {
                TileData = tiles;
                return;
            }
                

            // 5. Watershed flood
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

                    AddPixelToTile(
                        tiles[label - 1],
                        idx % Width,
                        idx / Width);

                    EnqueueNeighbors(idx, label, labels, blur, buckets, b);
                }
            }

            // 6. Final fill (remaining pixels)
            FinalFill(labels, tiles);

            // 7. NEW: post-processing region merging
            if (ExpectedRegionCount > 0 && tiles.Count > ExpectedRegionCount)
            {
                MergeSmallestRegions(tiles, labels, Width, Height, ExpectedRegionCount);

                // After merging, clean up the list and remap labels to a contiguous range
                var consolidatedTiles = new List<TileSegment>(ExpectedRegionCount);
                int[] newLabelMap = new int[tiles.Count + 1];
                int currentNewLabel = 1;

                for (int i = 0; i < tiles.Count; i++)
                {
                    if (tiles[i].PixelOffsets.Count > 0)
                    {
                        consolidatedTiles.Add(tiles[i]);
                        newLabelMap[i + 1] = currentNewLabel;
                        currentNewLabel++;
                    }
                }

                // Rewrite the full label array using the new contiguous IDs
                for (int i = 0; i < labels.Length; i++)
                {
                    if (labels[i] > 0)
                    {
                        labels[i] = newLabelMap[labels[i]];
                    }
                }

                tiles = consolidatedTiles; // Replace list
            }

            // 8. Optional corner tile slicing
            if (SliceCornerTiles)
            {
                SliceCornerTilesAlongTopology(tiles, labels, Width, Height);
            }

            // 9. Centroids
            foreach (var tile in tiles)
            {
                if (tile.PixelOffsets.Count > 0)
                {
                    tile.CentroidX = (float)tile.SumX / tile.PixelOffsets.Count / (Width - 1);
                    tile.CentroidY = (float)tile.SumY / tile.PixelOffsets.Count / (Height - 1);
                }
            }

            // 10. Generate label map
            GenerateLabelMap(Width, Height, labels, tiles.Count);

            // Assign labels to the class property
            Labels = labels;

            TileData = tiles;
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
        private void FinalFill(int[] labels, List<TileSegment> tiles)
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
                        tiles[l - 1].PixelOffsets.Add((i / Width) * Stride + (i % Width) * Bpp);
                        // Centroid sums can optionally be updated here as well
                    }
                }
            }
        }

        // Merges the smallest regions until the requested target region count is reached.
        private void MergeSmallestRegions(List<TileSegment> tiles, int[] labels, int width, int height, int targetCount)
        {
            int mergesNeeded = tiles.Count - targetCount;

            // List of indices that are still active (not merged away)
            var activeTiles = new List<int>(tiles.Count);
            for (int i = 0; i < tiles.Count; i++) activeTiles.Add(i);

            for (int m = 0; m < mergesNeeded; m++)
            {
                int smallestIdx = -1;
                int minSize = int.MaxValue;

                // Find the smallest tile that still exists
                for (int i = 0; i < activeTiles.Count; i++)
                {
                    int idx = activeTiles[i];
                    if (tiles[idx].PixelOffsets.Count < minSize)
                    {
                        minSize = tiles[idx].PixelOffsets.Count;
                        smallestIdx = idx;
                    }
                }

                if (smallestIdx == -1) break;

                int labelToMerge = smallestIdx + 1;
                var tileToMerge = tiles[smallestIdx];

                // Find the neighbor with the longest shared border
                int bestNeighborLabel = FindDominantNeighbor(tileToMerge, labels, width, height, labelToMerge);

                if (bestNeighborLabel > 0 && bestNeighborLabel != labelToMerge)
                {
                    var neighborTile = tiles[bestNeighborLabel - 1];

                    // Transfer pixels and accumulated sums
                    neighborTile.PixelOffsets.AddRange(tileToMerge.PixelOffsets);
                    neighborTile.SumX += tileToMerge.SumX;
                    neighborTile.SumY += tileToMerge.SumY;

                    // Update the 1D label array efficiently (only affected pixels)
                    foreach (int offset in tileToMerge.PixelOffsets)
                    {
                        int pixelIdx = offset / Bpp;
                        labels[pixelIdx] = bestNeighborLabel;
                    }

                    // Clear merged tile
                    tileToMerge.PixelOffsets.Clear();
                }

                // Remove from active checks
                activeTiles.Remove(smallestIdx);
            }
        }

        // Finds the neighboring label that shares the strongest border with the tile.
        private int FindDominantNeighbor(TileSegment tile, int[] labels, int width, int height, int myLabel)
        {
            int bestLabel = 0;
            int maxCount = -1;

            // Count how often a foreign label borders this tile
            var counts = new Dictionary<int, int>();

            foreach (int offset in tile.PixelOffsets)
            {
                int pixelIdx = offset / Bpp;

                // Check 4 neighbors: up, down, left, right
                if (pixelIdx >= width)
                    AddNeighborCount(labels[pixelIdx - width], myLabel, counts);

                if (pixelIdx < labels.Length - width)
                    AddNeighborCount(labels[pixelIdx + width], myLabel, counts);

                if (pixelIdx % width > 0)
                    AddNeighborCount(labels[pixelIdx - 1], myLabel, counts);

                if (pixelIdx % width < width - 1)
                    AddNeighborCount(labels[pixelIdx + 1], myLabel, counts);
            }

            // Select the neighbor with the highest count
            foreach (var kvp in counts)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    bestLabel = kvp.Key;
                }
            }

            return bestLabel;
        }

        // Increments the border-contact count for a valid neighboring label.
        private void AddNeighborCount(int nLabel, int myLabel, Dictionary<int, int> counts)
        {
            if (nLabel != 0 && nLabel != myLabel)
            {
                if (!counts.TryGetValue(nLabel, out int count))
                    count = 0;

                counts[nLabel] = count + 1;
            }
        }

        // Creates a colored debug map from label data and stores analysis dimensions.
        private unsafe void GenerateLabelMap(int width, int height, int[] labels, int labelCount)
        {
            int stride = width * Bpp;

            // Create target array
            byte[] map = new byte[width * height * Bpp];

            LastAnalysisMap = map;
            LastAnalysisWidth = width;
            LastAnalysisHeight = height;

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

            fixed (byte* pDebug = map)
            fixed (int* pLabels = labels)
            {
                for (int y = 0; y < height; y++)
                {
                    int rowOffset = y * stride;
                    int labelRow = y * width;

                    for (int x = 0; x < width; x++)
                    {
                        int label = pLabels[labelRow + x];
                        int offset = rowOffset + x * Bpp;

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

        // Enqueues direct neighbors around a coordinate into bucketed flood queues.
        private void EnqueueNeighbors(int x, int y, int label, int width, int height, int[] labels, float[] blur, Queue<(int, int, int)>[] buckets)
        {
            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];

                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (labels[ny * width + nx] == 0) // Only check unassigned pixels
                    {
                        int val = (int)Math.Clamp(blur[ny * width + nx], 0, 255);
                        buckets[val].Enqueue((nx, ny, label));
                    }
                }
            }
        }

        // Adds a pixel to a tile and updates centroid accumulation sums.
        private void AddPixelToTile(TileSegment tile, int x, int y)
        {
            tile.PixelOffsets.Add((y * Stride) + (x * Bpp));
            tile.SumX += x;
            tile.SumY += y;
        }

        // Slices corner tiles along an oriented line through the corner pixel.
        // The cut angle is pivot-driven and adjusted per mode and corner position:
        //   Left / Right modes        : angle = 10° + Pivot×70°  (Pivot=0→10°, 0.5→45°, 1→80°)
        //   Top  / Bottom modes       : angle = 80° − Pivot×70°  (reversed)
        //   Diagonal modes, top corner: reversed; bottom corner: original
        // Corner tiles are tiles containing any of the four image corner pixels.
        // Within each side, connected component analysis is run to handle star-shaped tiles
        // that may produce multiple disconnected fragments.
        private void SliceCornerTilesAlongTopology(List<TileSegment> tiles, int[] labels, int width, int height)
        {
            // Corner pixel indices and their coordinates
            int[] cornerPixelIndices = new int[]
            {
                0,                                      // top-left (0,0)
                width - 1,                              // top-right (W-1, 0)
                (height - 1) * width,                   // bottom-left (0, H-1)
                (height - 1) * width + (width - 1)      // bottom-right (W-1, H-1)
            };

            (int cx, int cy)[] cornerCoords = new (int, int)[]
            {
                (0, 0),
                (width - 1, 0),
                (0, height - 1),
                (width - 1, height - 1)
            };

            // Map each unique corner tile label to the first corner coordinate it contains
            var tileToCornersMap = new Dictionary<int, (int cx, int cy)>();
            for (int i = 0; i < cornerPixelIndices.Length; i++)
            {
                int label = labels[cornerPixelIndices[i]];
                if (label > 0 && !tileToCornersMap.ContainsKey(label))
                    tileToCornersMap[label] = cornerCoords[i];
            }

            // Process each corner tile
            foreach (var (cornerLabel, (cornerX, cornerY)) in tileToCornersMap)
            {
                int tileIndex = cornerLabel - 1;
                if (tileIndex < 0 || tileIndex >= tiles.Count)
                    continue;

                var tile = tiles[tileIndex];
                if (tile.PixelOffsets.Count == 0)
                    continue;

                // Determine whether to reverse the pivot-to-angle mapping for this corner.
                // Top/Bottom modes always reverse; Diagonal modes reverse only for top corners.
                bool reverseAngle = Mode switch
                {
                    TransitionMode.Top => true,
                    TransitionMode.Bottom => true,
                    TransitionMode.DiagonalTopLeft => cornerY == 0,
                    TransitionMode.DiagonalTopRight => cornerY == 0,
                    _ => false   // Left, Right: original mapping
                };

                // Original: Pivot=0→10°, 0.5→45°, 1.0→80°
                // Reversed: Pivot=0→80°, 0.5→45°, 1.0→10°
                float angleDeg = reverseAngle ? 80f - Pivot * 70f : 10f + Pivot * 70f;
                float tanAngle = MathF.Tan(angleDeg * MathF.PI / 180f);

                // Classify each pixel by which side of the angle-based line it falls on.
                // For pixel (px, py), compute dx = |px - cornerX|, dy = |py - cornerY|.
                // The cut line from the corner is: dy = dx * tanAngle.
                // side = true  → dy < dx * tanAngle  (closer to the horizontal edge from corner)
                // side = false → dy >= dx * tanAngle (closer to the vertical edge from corner)
                var pixelSides = new List<(int offset, int x, int y, bool side)>(tile.PixelOffsets.Count);

                bool hasHighSide = false;
                bool hasLowSide = false;

                foreach (int offset in tile.PixelOffsets)
                {
                    int py = offset / Stride;
                    int px = (offset % Stride) / Bpp;

                    float dx = Math.Abs(px - cornerX);
                    float dy = Math.Abs(py - cornerY);

                    bool side = dy < dx * tanAngle;
                    pixelSides.Add((offset, px, py, side));

                    if (side) hasHighSide = true;
                    else hasLowSide = true;
                }

                // If all pixels fall on the same side, no split needed
                if (!hasHighSide || !hasLowSide)
                    continue;

                // Build a pixel lookup for connected component analysis within this tile
                var pixelMap = new Dictionary<(int x, int y), (int offset, bool side)>(pixelSides.Count);
                foreach (var (offset, px, py, side) in pixelSides)
                {
                    pixelMap[(px, py)] = (offset, side);
                }

                // Run connected component analysis within the tile, respecting the side boundary
                var visited = new HashSet<(int x, int y)>(pixelSides.Count);
                var components = new List<List<(int offset, int x, int y)>>();

                foreach (var (offset, px, py, side) in pixelSides)
                {
                    if (visited.Contains((px, py)))
                        continue;

                    // BFS within the same side
                    var component = new List<(int offset, int x, int y)>();
                    var queue = new Queue<(int x, int y)>();
                    queue.Enqueue((px, py));
                    visited.Add((px, py));

                    while (queue.Count > 0)
                    {
                        var (cx, cy) = queue.Dequeue();
                        var (co, cs) = pixelMap[(cx, cy)];
                        component.Add((co, cx, cy));

                        // Check 4-connected neighbors
                        int[] dxArr = { 0, 0, -1, 1 };
                        int[] dyArr = { -1, 1, 0, 0 };

                        for (int d = 0; d < 4; d++)
                        {
                            int nnx = cx + dxArr[d];
                            int nny = cy + dyArr[d];

                            if (!visited.Contains((nnx, nny)) &&
                                pixelMap.TryGetValue((nnx, nny), out var neighbor) &&
                                neighbor.side == side)
                            {
                                visited.Add((nnx, nny));
                                queue.Enqueue((nnx, nny));
                            }
                        }
                    }

                    components.Add(component);
                }

                // If only one component, no actual split happened
                if (components.Count <= 1)
                    continue;

                // Rewrite the original tile with the first component, create new tiles for the rest
                tile.PixelOffsets.Clear();
                tile.SumX = 0;
                tile.SumY = 0;

                foreach (var (co, cx, cy) in components[0])
                {
                    tile.PixelOffsets.Add(co);
                    tile.SumX += cx;
                    tile.SumY += cy;
                }

                // Create new tiles for remaining components
                for (int c = 1; c < components.Count; c++)
                {
                    var newTile = new TileSegment();
                    int newLabel = tiles.Count + 1;

                    foreach (var (co, cx, cy) in components[c])
                    {
                        newTile.PixelOffsets.Add(co);
                        newTile.SumX += cx;
                        newTile.SumY += cy;

                        // Update the label array
                        int pixelIdx = cy * width + cx;
                        labels[pixelIdx] = newLabel;
                    }

                    tiles.Add(newTile);
                }
            }
        }
    }
}
