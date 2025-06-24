using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace THelperLib.BitmapOperations
{
    public partial class BitmapOperations : IBitmapOperations
    {
        public WriteableBitmap CreateBitmapAndResize(
            byte[] data,
            int width,
            int height,
            int stride,
            PixelFormat pixelFormat,
            int targetWidth,
            int targetHeight,
            BitmapScalingMode scalingMode)
        {
            var wb = new WriteableBitmap(
                pixelWidth:     width, 
                pixelHeight:    height, 
                dpiX:           96, 
                dpiY:           96, 
                pixelFormat:    pixelFormat, 
                palette:        null);

            wb.WritePixels(
                sourceRect: new Int32Rect(0, 0, width, height), 
                pixels:     data, 
                stride:     stride, 
                offset:     0);

            var rect = new Rect(0, 0, targetWidth, targetHeight);

            var drawingVisual = new DrawingVisual();

            using (var dc = drawingVisual.RenderOpen())
            {
                RenderOptions.SetBitmapScalingMode(
                    target:             drawingVisual, 
                    bitmapScalingMode:  scalingMode);
                dc.DrawImage(wb, rect);
            }

            var resized = new RenderTargetBitmap(
                pixelWidth:     targetWidth, 
                pixelHeight:    targetHeight, 
                dpiX:           96,
                dpiY:           96, 
                pixelFormat:    PixelFormats.Pbgra32);

            resized.Render(drawingVisual);

            return new WriteableBitmap(resized);
        }
    }
}
