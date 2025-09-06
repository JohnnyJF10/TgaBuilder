using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public IWriteableBitmap ConvertRGB24ToBGRA32(IWriteableBitmap sourceBitmap)
        {
            if (sourceBitmap == null)
                throw new ArgumentNullException(nameof(sourceBitmap), "Source bitmap cannot be null.");

            if (sourceBitmap.HasAlpha)
                throw new ArgumentException("Source bitmap must be in RGB24 format.", nameof(sourceBitmap));

            // Create a new IWriteableBitmap with BGRA32 format
            var targetBitmap = _mediaFactory.CreateEmptyBitmap(
                width:          sourceBitmap.PixelWidth,
                height:         sourceBitmap.PixelHeight,
                hasAlpha:       true);

            var targetDirtyRect = new PixelRect(0, 0, targetBitmap.PixelWidth, targetBitmap.PixelHeight);

            // Lock the source and target bitmaps for writing

            using (var sourceLocker = sourceBitmap.GetLocker())
            using (var targetLocker = targetBitmap.GetLocker(targetDirtyRect))
            {
                unsafe
                {
                    byte* srcPtr = (byte*)sourceLocker.BackBuffer;
                    byte* dstPtr = (byte*)targetLocker.BackBuffer;

                    int width = sourceBitmap.PixelWidth;
                    int height = sourceBitmap.PixelHeight;
                    int srcStride = sourceBitmap.BackBufferStride;
                    int dstStride = targetBitmap.BackBufferStride;

                    int dstIdx = 0, srcIdx = 0;

                    byte r, g, b;

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            srcIdx = (y * srcStride) + (x * 3);

                            // Read RGB values from the source bitmap
                            r = srcPtr[srcIdx    ];
                            g = srcPtr[srcIdx + 1];
                            b = srcPtr[srcIdx + 2];

                            dstIdx = (y * dstStride) + (x * 4);

                            if ((r, g, b) != (255, 0, 255)) // Write BGRA values to the target bitmap if not magenta
                            {
                                dstPtr[dstIdx    ] = b;   // B
                                dstPtr[dstIdx + 1] = g;   // G
                                dstPtr[dstIdx + 2] = r;   // R
                                dstPtr[dstIdx + 3] = 255; // A (fully opaque)
                            }
                        }
                    }
                }
            }


                return targetBitmap;
        }

        public IWriteableBitmap ConvertBGRA32ToRGB24(IWriteableBitmap sourceBitmap)
        {
            if (sourceBitmap == null)
                throw new ArgumentNullException(nameof(sourceBitmap), "Source bitmap cannot be null.");
            if (!sourceBitmap.HasAlpha)
                throw new ArgumentException("Source bitmap must be in BGRA32 format.", nameof(sourceBitmap));
            
            // Create a new IWriteableBitmap with RGB24 format
            var targetBitmap = _mediaFactory.CreateEmptyBitmap(
                width:          sourceBitmap.PixelWidth,
                height:         sourceBitmap.PixelHeight,
                hasAlpha:       false);

            var targetDirtyRect = new PixelRect(0, 0, targetBitmap.PixelWidth, targetBitmap.PixelHeight);

            // Lock the source and target bitmaps for writing

            using (var sourceLocker = sourceBitmap.GetLocker())
            using (var targetLocker = targetBitmap.GetLocker(targetDirtyRect))
            {
                unsafe
                {
                    byte* srcPtr = (byte*)sourceLocker.BackBuffer;
                    byte* dstPtr = (byte*)targetLocker.BackBuffer;

                    int width = sourceBitmap.PixelWidth;
                    int height = sourceBitmap.PixelHeight;
                    int srcStride = sourceBitmap.BackBufferStride;
                    int dstStride = targetBitmap.BackBufferStride;

                    int dstIdx = 0, srcIdx = 0;

                    byte r, g, b, a;

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            srcIdx = (y * srcStride) + (x * 4);
                            // Read BGRA values from the source bitmap
                            b = srcPtr[srcIdx    ];
                            g = srcPtr[srcIdx + 1];
                            r = srcPtr[srcIdx + 2];
                            a = srcPtr[srcIdx + 3];

                            dstIdx = (y * dstStride) + (x * 3);

                            if (a == 0) // If fully transparent, write magegenta
                            {
                                dstPtr[dstIdx    ] = 255;   // R
                                dstPtr[dstIdx + 1] = 0;     // G
                                dstPtr[dstIdx + 2] = 255;   // B
                            }
                            else if (a < 255) // If semi-transparent, blend with black
                            {
                                dstPtr[dstIdx    ] = (byte)(r * a / 255);   // R
                                dstPtr[dstIdx + 1] = (byte)(g * a / 255);   // G
                                dstPtr[dstIdx + 2] = (byte)(b * a / 255);   // B
                            }
                            else // Write RGB values to the target bitmap
                            {
                                dstPtr[dstIdx    ] = r;   // R
                                dstPtr[dstIdx + 1] = g;   // G
                                dstPtr[dstIdx + 2] = b;   // B                        
                            }
                        }
                    }
                }

            }

            return targetBitmap;

        }
    }
}