using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Transitions
{
    public enum FilterType
    {
        None,
        BoxBlur,
        Median,
        Bilateral
    }

    public enum SegmentationMethod
    {
        Watershed,
        XYProjection
    }

    public partial class TransitionHelper
    {

        // Runs a watershed-style tile analysis and builds labels, centroids, and a debug map.
        public unsafe void AnalyzeTiles(byte[] pixels)
        {
            int totalPixels = Width * Height;

            float[] filtered = new float[totalPixels];
            int[] labels = new int[totalPixels];
            

            // 1. Compute grayscale values
            float[] gray = new float[totalPixels];

            fixed (byte* p = pixels)
            {
                for (int i = 0; i < totalPixels; i++)
                {
                    byte* px = p + (i * TRANSITIONS_BPP);
                    gray[i] = px[2] * 0.299f + px[1] * 0.587f + px[0] * 0.114f;
                }
            }

            // 2. Initial Filter
            switch (SelectedFilter)
            {
                case FilterType.BoxBlur:
                    BoxBlur(filtered, gray);
                    break;
                case FilterType.Median:
                    MedianFilter3x3(filtered, gray);
                    break;
                case FilterType.Bilateral:
                    BilateralFilter3x3(filtered, gray, 30f);
                    break;
                case FilterType.None:
                default:
                    filtered = gray;
                    break;
            }

            // 3. Segmentation
            int labelCount = SegmentationMethod switch
            {
                SegmentationMethod.Watershed => WatershedSegmentation(filtered, labels),
                SegmentationMethod.XYProjection => XYProjectionSegmentation(filtered, labels),
                _ => 0
            };

            // 4. Build TileSegmentList (centroids + pixel offsets)
            var tileSegmentList = BuildTileSegmentList(labels, Width, Height, labelCount);

            // 5. Generate label map
            GenerateLabelMap(Width, Height, labels, labelCount);

            // Assign labels to the class property
            Labels = labels;

            TileSegmentList = tileSegmentList;
        }


        private List<TileSegment> BuildTileSegmentList(int[] labels, int width, int height, int labelCount)
        {
            // Arrays for summing coordinates and counting pixels for each label
            long[] sumX = new long[labelCount + 1];
            long[] sumY = new long[labelCount + 1];
            int[] counts = new int[labelCount + 1];

            int totalPixels = width * height;

            for (int i = 0; i < totalPixels; i++)
            {
                int lbl = labels[i];
                if (lbl > 0 && lbl <= labelCount)
                {
                    sumX[lbl] += i % width;
                    sumY[lbl] += i / width;
                    counts[lbl]++;
                }
            }

            var segments = new List<TileSegment>(labelCount);
            for (int i = 1; i <= labelCount; i++)
            {
                var segment = new TileSegment();
                if (counts[i] > 0)
                {
                    // Divide the average coordinate by the dimension to get relative (0-1) values
                    segment.CentroidX = ((float)sumX[i] / counts[i]) / width;
                    segment.CentroidY = ((float)sumY[i] / counts[i]) / height;
                }
                segments.Add(segment);
            }

            // Populate pixel offsets for each segment
            for (int i = 0; i < totalPixels; i++)
            {
                int lbl = labels[i];
                if (lbl > 0 && lbl <= labelCount)
                {
                    segments[lbl - 1].PixelOffsets.Add(i);
                }
            }

            return segments;
        }




        // Creates a colored debug map from label data and stores analysis dimensions.
        private unsafe void GenerateLabelMap(int width, int height, int[] labels, int labelCount)
        {
            int stride = width * TRANSITIONS_BPP;

            // Create target array
            byte[] map = new byte[width * height * TRANSITIONS_BPP];

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
    }
}
