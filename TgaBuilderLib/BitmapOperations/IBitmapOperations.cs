using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.BitmapOperations
{
    public interface IBitmapOperations
    {
        int PlacedSize { get; set; }
        WriteableBitmap? SwapBitmap { get; set; }


        // Create
        WriteableBitmap CreateBitmapAndResize(
            byte[] data,
            int width,
            int height,
            int stride,
            PixelFormat pixelFormat,
            int targetWidth,
            int targetHeight,
            BitmapScalingMode scalingMode);

        WriteableBitmap GetTargetFromSource(
            WriteableBitmap source);


        // Crop
        WriteableBitmap CropBitmap(
            WriteableBitmap source,
            Int32Rect rectangle);

        WriteableBitmap CropBitmap(
            WriteableBitmap source,
            Int32Rect rectangle,
            Color replacedColor,
            Color newColor);


        // Scale / Change Size
        WriteableBitmap Resize(
            WriteableBitmap sourceBitmap,
            int newWidth,
            int newHeight);

        WriteableBitmap ResizeHeightMonitored(
            WriteableBitmap sourceBitmap,
            int newHeight,
            byte[] undoData);

        WriteableBitmap ResizeWidthMonitored(
            WriteableBitmap sourceBitmap,
            int newWidth,
            byte[] undoData);

        WriteableBitmap ResizeScaled(
            WriteableBitmap source,
            int targetWidth,
            int targetHeight = -1);

        WriteableBitmap ResizeSorted(
            WriteableBitmap oldBitmap,
            int newWidth,
            int tileSize,
            int newHeight = -1);


        // Insert / Fill
        void FillRectArray(
            WriteableBitmap bitmap,
            Int32Rect rect,
            byte[] pixels);

        void FillRectBitmap(
            WriteableBitmap source,
            WriteableBitmap target,
            (int X, int Y) pos,
            PlacingMode placingMode = PlacingMode.Default);

        void FillRectBitmapMonitored(
            WriteableBitmap source,
            WriteableBitmap target,
            (int X, int Y) pos,
            byte[] undoPixels,
            byte[] redoPixels,
            PlacingMode placingMode = PlacingMode.Default);

        void FillRectBitmapNoConvert(
            WriteableBitmap source,
            WriteableBitmap target,
            (int X, int Y) pos);

        void FillRectColor(
            WriteableBitmap bitmap,
            Int32Rect rect,
            Color? fillColor = null);


        // Pixel Operationens
        Color GetPixelBrush(
            WriteableBitmap bitmap,
            int x,
            int y);

        byte[] GetRegionPixels(
            WriteableBitmap bmp,
            Int32Rect rect);

        WriteableBitmap ReplaceColor(
            WriteableBitmap source,
            Color replacedColor,
            Color newColor);


        // Transformations
        void FlipRectHor(
            WriteableBitmap bitmap,
            Int32Rect rectangle);

        void FlipRectVert(
            WriteableBitmap bitmap,
            Int32Rect rectangle);

        void RotateRec(
            WriteableBitmap bitmap,
            Int32Rect rectangle,
            bool counterclockwise = false);


        // Tile Manipulation
        void TileRally(
            WriteableBitmap bitmap,
            (int X, int Y) sourcePos,
            (int X, int Y) targetPos,
            int tileSize);

        void TileSwitch(
            WriteableBitmap bitmap,
            (int X, int Y) sourcePos,
            (int X, int Y) targetPos,
            int tileSize);
    }
}