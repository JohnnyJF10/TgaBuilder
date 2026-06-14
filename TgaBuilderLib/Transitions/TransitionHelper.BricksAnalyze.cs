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
        private List<TileSegment> BricksAnalyze(byte[] pixels)
        {

            bool isGraySegmentation = SegmentationMethod == SegmentationMethod.Watershed
            || SegmentationMethod == SegmentationMethod.BrickFit
            || SegmentationMethod == SegmentationMethod.GridFit;

            int totalPixels = Width * Height;


            // 1. Pre-Processings
            if (isGraySegmentation)
                ComputeGrayValues(pixels, _scratchGray);
            else
                Array.Copy(pixels, _scratchFilteredColor, pixels.Length);

            // 2. Initial Filter. 

            switch (SelectedFilter, isGraySegmentation)
            {
                case (FilterType.BoxBlur, true):
                    BoxBlurGray(_scratchFiltered, _scratchGray);
                    break;
                case (FilterType.BoxBlur, false):
                    BoxBlurColor(_scratchFilteredColor, pixels);
                    break;
                case (FilterType.Median, true):
                    MedianFilter3x3Gray(_scratchFiltered, _scratchGray);
                    break;
                case (FilterType.Median, false):
                    MedianFilter3x3Color(_scratchFilteredColor, pixels);
                    break;                
                case (FilterType.Bilateral, true):
                    BilateralFilter3x3Gray(_scratchFiltered, _scratchGray, BilateralSigma);
                    break;
                case (FilterType.Bilateral, false):
                    BilateralFilter3x3Color(_scratchFilteredColor, pixels, BilateralSigma);
                    break;                
                case (FilterType.Gaussian, true):
                    GaussianBlur3x3Gray(_scratchFiltered, _scratchGray, GaussianSigma);
                    break;
                case (FilterType.Gaussian, false):
                    GaussianBlur3x3Color(_scratchFilteredColor, pixels, GaussianSigma);
                    break;
                case (FilterType.None, true):
                    Array.Copy(_scratchGray, _scratchFiltered, totalPixels);
                    break;
                case (FilterType.None, false):
                    Array.Copy(_scratchFilteredColor, pixels, totalPixels * TRANSITIONS_BPP);
                    break;
                default:
                    break;
            }

            // 3. Segmentation
            Array.Clear(_labels, 0, totalPixels);
            int labelCount = SegmentationMethod switch
            {
                SegmentationMethod.Felzenszwalb => Felzenszwalb(_scratchFilteredColor, _labels, FelzenszwalbMinSize, FelzenszwalbScale),
                SegmentationMethod.Slic => Slic(_scratchFilteredColor, _labels, SlicSegmentCount, SlicCompactness),
                SegmentationMethod.Quickshift => Quickshift(_scratchFilteredColor, _labels, QuickshiftMaxDist, QuickshiftRatio),
                SegmentationMethod.Watershed => Watershed(_scratchFiltered, _labels),
                SegmentationMethod.BrickFit => BrickFit(_scratchFiltered, _labels, GridFitAngle),
                SegmentationMethod.GridFit => GridFit(_scratchFiltered, _labels, GridFitAngle),
                _ => 0
            };

            // 4. Build _tileSegmentList (centroids + pixel offsets)
            var tileSegmentList = BuildTileSegmentList(_labels, Width, Height, labelCount);

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
