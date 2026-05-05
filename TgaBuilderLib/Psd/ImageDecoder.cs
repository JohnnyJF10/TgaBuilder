#region Licence
/*
Copyright (c) 2013, Darren Horrocks
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the <organization> nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion
using System;
using System.Threading.Tasks;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Psd
{
    public class ImageDecoder
    {
        public static IWriteableBitmap DecodeImage(PsdFile psdFile, IMediaFactory mediaFactory)
        {
            var bitmap = mediaFactory.CreateEmptyBitmap(psdFile.Columns, psdFile.Rows, hasAlpha: true);
            int stride = bitmap.BackBufferStride;
            var dirtyRect = new PixelRect(0, 0, psdFile.Columns, psdFile.Rows);

            using (var locker = bitmap.GetLocker(dirtyRect))
            {
                unsafe
                {
                    byte* ptr = (byte*)locker.BackBuffer.ToPointer();
                    Parallel.For(0, psdFile.Rows, y =>
                    {
                        int rowIndex = y * psdFile.Columns;
                        byte* rowPtr = ptr + y * stride;
                        for (int x = 0; x < psdFile.Columns; x++)
                        {
                            int pos = rowIndex + x;
                            GetColor(psdFile, pos, out byte r, out byte g, out byte b, out byte a);
                            byte* pixel = rowPtr + x * 4;
                            pixel[0] = b;
                            pixel[1] = g;
                            pixel[2] = r;
                            pixel[3] = a;
                        }
                    });
                }
            }

            return bitmap;
        }

        public static IWriteableBitmap? DecodeImage(Layer layer, IMediaFactory mediaFactory)
        {
            if (layer.Rect.Width == 0 || layer.Rect.Height == 0) return null;

            var bitmap = mediaFactory.CreateEmptyBitmap(layer.Rect.Width, layer.Rect.Height, hasAlpha: true);
            int stride = bitmap.BackBufferStride;
            var dirtyRect = new PixelRect(0, 0, layer.Rect.Width, layer.Rect.Height);

            bool hasMask = layer.SortedChannels.ContainsKey(-2);

            using (var locker = bitmap.GetLocker(dirtyRect))
            {
                unsafe
                {
                    byte* ptr = (byte*)locker.BackBuffer.ToPointer();
                    Parallel.For(0, layer.Rect.Height, y =>
                    {
                        int rowIndex = y * layer.Rect.Width;
                        byte* rowPtr = ptr + y * stride;
                        for (int x = 0; x < layer.Rect.Width; x++)
                        {
                            int pos = rowIndex + x;
                            GetColor(layer, pos, out byte r, out byte g, out byte b, out byte a);

                            if (hasMask)
                            {
                                int maskAlpha = GetMaskAlpha(layer.MaskData, x, y);
                                a = (byte)(a * maskAlpha / 255);
                            }

                            byte* pixel = rowPtr + x * 4;
                            pixel[0] = b;
                            pixel[1] = g;
                            pixel[2] = r;
                            pixel[3] = a;
                        }
                    });
                }
            }

            return bitmap;
        }

        public static IWriteableBitmap? DecodeImage(Layer.Mask mask, IMediaFactory mediaFactory)
        {
            Layer layer = mask.Layer;

            if (mask.Rect.Width == 0 || mask.Rect.Height == 0) return null;

            var bitmap = mediaFactory.CreateEmptyBitmap(mask.Rect.Width, mask.Rect.Height, hasAlpha: true);
            int stride = bitmap.BackBufferStride;
            var dirtyRect = new PixelRect(0, 0, mask.Rect.Width, mask.Rect.Height);

            using (var locker = bitmap.GetLocker(dirtyRect))
            {
                unsafe
                {
                    byte* ptr = (byte*)locker.BackBuffer.ToPointer();
                    Parallel.For(0, layer.Rect.Height, y =>
                    {
                        int rowIndex = y * layer.Rect.Width;
                        byte* rowPtr = ptr + y * stride;
                        for (int x = 0; x < layer.Rect.Width; x++)
                        {
                            int pos = rowIndex + x;
                            byte gray = mask.ImageData[pos];
                            byte* pixel = rowPtr + x * 4;
                            pixel[0] = gray;
                            pixel[1] = gray;
                            pixel[2] = gray;
                            pixel[3] = 255;
                        }
                    });
                }
            }

            return bitmap;
        }

        private static void GetColor(PsdFile psdFile, int pos, out byte r, out byte g, out byte b, out byte a)
        {
            r = g = b = 255;
            a = 255;

            byte red = psdFile.ImageData[0][pos];
            byte green = psdFile.ImageData[1][pos];
            byte blue = psdFile.ImageData[2][pos];

            byte alpha = 255;
            if (psdFile.ImageData.Length > 3)
                alpha = psdFile.ImageData[3][pos];

            switch (psdFile.ColorMode)
            {
                case ColorMode.RGB:
                    r = red; g = green; b = blue; a = alpha;
                    break;
                case ColorMode.CMYK:
                    CMYKToRGB(red, green, blue, alpha, out r, out g, out b);
                    break;
                case ColorMode.Multichannel:
                    CMYKToRGB(red, green, blue, 0, out r, out g, out b);
                    break;
                case ColorMode.Grayscale:
                case ColorMode.Duotone:
                    r = g = b = red;
                    break;
                case ColorMode.Indexed:
                    {
                        int index = red;
                        r = psdFile.ColorModeData[index];
                        g = psdFile.ColorModeData[index + 256];
                        b = psdFile.ColorModeData[index + 2 * 256];
                    }
                    break;
                case ColorMode.Lab:
                    LabToRGB(red, green, blue, out r, out g, out b);
                    break;
            }
        }

        private static void GetColor(Layer layer, int pos, out byte r, out byte g, out byte b, out byte a)
        {
            r = g = b = 255;
            a = 255;

            switch (layer.PsdFile.ColorMode)
            {
                case ColorMode.RGB:
                    r = layer.SortedChannels[0].ImageData[pos];
                    g = layer.SortedChannels[1].ImageData[pos];
                    b = layer.SortedChannels[2].ImageData[pos];
                    break;
                case ColorMode.CMYK:
                    CMYKToRGB(layer.SortedChannels[0].ImageData[pos], layer.SortedChannels[1].ImageData[pos], layer.SortedChannels[2].ImageData[pos], layer.SortedChannels[3].ImageData[pos], out r, out g, out b);
                    break;
                case ColorMode.Multichannel:
                    CMYKToRGB(layer.SortedChannels[0].ImageData[pos], layer.SortedChannels[1].ImageData[pos], layer.SortedChannels[2].ImageData[pos], 0, out r, out g, out b);
                    break;
                case ColorMode.Grayscale:
                case ColorMode.Duotone:
                    r = g = b = layer.SortedChannels[0].ImageData[pos];
                    break;
                case ColorMode.Indexed:
                    {
                        int index = layer.SortedChannels[0].ImageData[pos];
                        r = layer.PsdFile.ColorModeData[index];
                        g = layer.PsdFile.ColorModeData[index + 256];
                        b = layer.PsdFile.ColorModeData[index + 2 * 256];
                    }
                    break;
                case ColorMode.Lab:
                    LabToRGB(layer.SortedChannels[0].ImageData[pos], layer.SortedChannels[1].ImageData[pos], layer.SortedChannels[2].ImageData[pos], out r, out g, out b);
                    break;
            }

            if (layer.SortedChannels.ContainsKey(-1))
                a = layer.SortedChannels[-1].ImageData[pos];
        }

        private static int GetMaskAlpha(Layer.Mask mask, int x, int y)
        {
            int c = 255;

            if (mask.PositionIsRelative)
            {
                x -= mask.Rect.X;
                y -= mask.Rect.Y;
            }
            else
            {
                x = x + mask.Layer.Rect.X - mask.Rect.X;
                y = y + mask.Layer.Rect.Y - mask.Rect.Y;
            }

            if (y >= 0 && y < mask.Rect.Height &&
                x >= 0 && x < mask.Rect.Width)
            {
                int pos = y * mask.Rect.Width + x;
                c = pos < mask.ImageData.Length ? mask.ImageData[pos] : 255;
            }

            return c;
        }

        private static void LabToRGB(byte lb, byte ab, byte bb, out byte r, out byte g, out byte b)
        {
            double exL = lb;
            double exA = ab;
            double exB = bb;

            const double lCoef = 256.0 / 100.0;
            const double aCoef = 256.0 / 256.0;
            const double bCoef = 256.0 / 256.0;

            int l = (int)(exL / lCoef);
            int a = (int)(exA / aCoef - 128.0);
            int bVal = (int)(exB / bCoef - 128.0);

            // For the conversion we first convert values to XYZ and then to RGB
            // Standards used Observer = 2, Illuminant = D65

            const double refX = 95.047;
            const double refY = 100.000;
            const double refZ = 108.883;

            double varY = (l + 16.0) / 116.0;
            double varX = a / 500.0 + varY;
            double varZ = varY - bVal / 200.0;

            varY = Math.Pow(varY, 3) > 0.008856 ? Math.Pow(varY, 3) : (varY - 16 / 116) / 7.787;
            varX = Math.Pow(varX, 3) > 0.008856 ? Math.Pow(varX, 3) : (varX - 16 / 116) / 7.787;
            varZ = Math.Pow(varZ, 3) > 0.008856 ? Math.Pow(varZ, 3) : (varZ - 16 / 116) / 7.787;

            double x = refX * varX;
            double y = refY * varY;
            double z = refZ * varZ;

            XYZToRGB(x, y, z, out r, out g, out b);
        }

        private static void XYZToRGB(double x, double y, double z, out byte r, out byte g, out byte b)
        {
            // Standards used Observer = 2, Illuminant = D65
            // ref_X = 95.047, ref_Y = 100.000, ref_Z = 108.883
            double varX = x / 100.0;
            double varY = y / 100.0;
            double varZ = z / 100.0;

            double varR = varX * 3.2406 + varY * -1.5372 + varZ * -0.4986;
            double varG = varX * -0.9689 + varY * 1.8758 + varZ * 0.0415;
            double varB = varX * 0.0557 + varY * -0.2040 + varZ * 1.0570;

            varR = varR > 0.0031308 ? 1.055 * Math.Pow(varR, 1 / 2.4) - 0.055 : 12.92 * varR;
            varG = varG > 0.0031308 ? 1.055 * Math.Pow(varG, 1 / 2.4) - 0.055 : 12.92 * varG;
            varB = varB > 0.0031308 ? 1.055 * Math.Pow(varB, 1 / 2.4) - 0.055 : 12.92 * varB;

            int nRed = (int)(varR * 256.0);
            int nGreen = (int)(varG * 256.0);
            int nBlue = (int)(varB * 256.0);

            nRed = nRed > 0 ? nRed : 0;
            nRed = nRed < 255 ? nRed : 255;

            nGreen = nGreen > 0 ? nGreen : 0;
            nGreen = nGreen < 255 ? nGreen : 255;

            nBlue = nBlue > 0 ? nBlue : 0;
            nBlue = nBlue < 255 ? nBlue : 255;

            r = (byte)nRed;
            g = (byte)nGreen;
            b = (byte)nBlue;
        }

        ///////////////////////////////////////////////////////////////////////////////
        //
        // The algorithms for these routines were taken from:
        //     http://www.neuro.sfc.keio.ac.jp/~aly/polygon/info/color-space-faq.html
        //
        // RGB --> CMYK                              CMYK --> RGB
        // ---------------------------------------   --------------------------------------------
        // Black   = minimum(1-Red,1-Green,1-Blue)   Red   = 1-minimum(1,Cyan*(1-Black)+Black)
        // Cyan    = (1-Red-Black)/(1-Black)         Green = 1-minimum(1,Magenta*(1-Black)+Black)
        // Magenta = (1-Green-Black)/(1-Black)       Blue  = 1-minimum(1,Yellow*(1-Black)+Black)
        // Yellow  = (1-Blue-Black)/(1-Black)
        //

        private static void CMYKToRGB(byte c, byte m, byte y, byte k, out byte r, out byte g, out byte b)
        {
            double dMaxColours = Math.Pow(2, 8);

            double exC = c;
            double exM = m;
            double exY = y;
            double exK = k;

            double C = 1.0 - exC / dMaxColours;
            double M = 1.0 - exM / dMaxColours;
            double Y = 1.0 - exY / dMaxColours;
            double K = 1.0 - exK / dMaxColours;

            int nRed = (int)((1.0 - (C * (1 - K) + K)) * 255);
            int nGreen = (int)((1.0 - (M * (1 - K) + K)) * 255);
            int nBlue = (int)((1.0 - (Y * (1 - K) + K)) * 255);

            nRed = nRed > 0 ? nRed : 0;
            nRed = nRed < 255 ? nRed : 255;

            nGreen = nGreen > 0 ? nGreen : 0;
            nGreen = nGreen < 255 ? nGreen : 255;

            nBlue = nBlue > 0 ? nBlue : 0;
            nBlue = nBlue < 255 ? nBlue : 255;

            r = (byte)nRed;
            g = (byte)nGreen;
            b = (byte)nBlue;
        }
    }
}
