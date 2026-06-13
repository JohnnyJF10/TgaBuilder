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
        Bilateral,
        Gaussian
    }

    public enum SegmentationMethod
    {
        Felzenszwalb,
        Slic,
        Quickshift,
        Watershed,
        BrickFit,
        GridFit,
    }

    public partial class TransitionHelper
    {

        // Runs a watershed-style tile analysis and builds labels, centroids, and a debug map.
        // Writes the labels into the reusable _labels buffer and returns the tile segment list.
        private List<TileSegment> BricksAnalyze(byte[] pixels)
        {
            int totalPixels = Width * Height;

            // Reuse the cached label buffer. `filtered`/`filteredColorPixels` are reference aliases
            // onto the appropriate scratch buffers (or the unfiltered inputs for FilterType.None).
            int[] labels = _labels;
            float[] filtered;
            byte[] filteredColorPixels;

            // 1. Compute grayscale values (fully overwrites _scratchGray, so no clear needed)
            float[] gray = _scratchGray;
            ComputeGrayValues(pixels, gray);

            // 2. Initial Filter. The gray filters write only the interior [1..W-2, 1..H-2], so the
            //    reused _scratchFiltered buffer must be cleared first to match the fresh-array
            //    zero border the segmentation relies on. The color filters write only the interior
            //    too, but seed their borders from the copied source pixels (same as the former
            //    pixels.Clone()), so they need the copy rather than a clear.
            switch (SelectedFilter)
            {
                case FilterType.BoxBlur:
                    filtered = _scratchFiltered;
                    Array.Clear(filtered, 0, totalPixels);
                    BoxBlurGray(filtered, gray);
                    filteredColorPixels = _scratchFilteredColor;
                    Array.Copy(pixels, filteredColorPixels, pixels.Length);
                    BoxBlurColor(filteredColorPixels, pixels);
                    break;
                case FilterType.Median:
                    filtered = _scratchFiltered;
                    Array.Clear(filtered, 0, totalPixels);
                    MedianFilter3x3Gray(filtered, gray);
                    filteredColorPixels = _scratchFilteredColor;
                    Array.Copy(pixels, filteredColorPixels, pixels.Length);
                    MedianFilter3x3Color(filteredColorPixels, pixels);
                    break;
                case FilterType.Bilateral:
                    filtered = _scratchFiltered;
                    Array.Clear(filtered, 0, totalPixels);
                    BilateralFilter3x3Gray(filtered, gray, BilateralSigma);
                    filteredColorPixels = _scratchFilteredColor;
                    Array.Copy(pixels, filteredColorPixels, pixels.Length);
                    BilateralFilter3x3Color(filteredColorPixels, pixels, BilateralSigma);
                    break;
                case FilterType.Gaussian:
                    filtered = _scratchFiltered;
                    Array.Clear(filtered, 0, totalPixels);
                    GaussianBlur3x3Gray(filtered, gray, GaussianSigma);
                    filteredColorPixels = _scratchFilteredColor;
                    Array.Copy(pixels, filteredColorPixels, pixels.Length);
                    GaussianBlur3x3Color(filteredColorPixels, pixels, GaussianSigma);
                    break;
                case FilterType.None:
                default:
                    filtered = gray;
                    filteredColorPixels = pixels;
                    break;
            }

            // 3. Segmentation (writes labels in place). Clear first so unassigned pixels read 0,
            //    matching the former fresh int[] allocation.
            Array.Clear(labels, 0, totalPixels);
            int labelCount = SegmentationMethod switch
            {
                SegmentationMethod.Felzenszwalb => Felzenszwalb(filteredColorPixels, labels, FelzenszwalbMinSize, FelzenszwalbScale),
                SegmentationMethod.Slic => Slic(filteredColorPixels, labels, SlicSegmentCount, SlicCompactness),
                SegmentationMethod.Quickshift => Quickshift(filteredColorPixels, labels, QuickshiftMaxDist, QuickshiftRatio),
                SegmentationMethod.Watershed => Watershed(filtered, labels),
                SegmentationMethod.BrickFit => BrickFit(filtered, labels, GridFitAngle),
                SegmentationMethod.GridFit => GridFit(filtered, labels, GridFitAngle),
                _ => 0
            };

            // 4. Build _tileSegmentList (centroids + pixel offsets)
            var tileSegmentList = BuildTileSegmentList(labels, Width, Height, labelCount);

            return tileSegmentList;
        }

        private void ComputeGrayValues(byte[] pixels, float[] gray)
        {
            int totalPixels = Width * Height;

            if (gray.Length != totalPixels)
                throw new ArgumentException("Gray array length does not match pixel data.");

            unsafe
            {
                fixed (byte* p = pixels)
                {
                    if (!InvertGrayscale)
                    {
                        for (int i = 0; i < totalPixels; i++)
                        {
                            byte* px = p + (i * TRANSITIONS_BPP);
                            gray[i] = px[2] * 0.299f + px[1] * 0.587f + px[0] * 0.114f;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < totalPixels; i++)
                        {
                            byte* px = p + (i * TRANSITIONS_BPP);
                            gray[i] = 255f - (px[2] * 0.299f + px[1] * 0.587f + px[0] * 0.114f);
                        }
                    }
                } 
            }
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
                // Segments with counts[i] == 0 retain default centroid (0,0) and empty PixelOffsets;
                // they are skipped during selection by the Count == 0 guard in BuildSelection.
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
    }
}
