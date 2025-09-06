using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.BitmapOperations
{
    public interface IBitmapOperations
    {
        int PlacedSize { get; set; }
        IWriteableBitmap? SwapBitmap { get; set; }


        //Convert
        IWriteableBitmap ConvertRGB24ToBGRA32(
            IWriteableBitmap sourceBitmap);

        IWriteableBitmap ConvertBGRA32ToRGB24(
            IWriteableBitmap sourceBitmap);


        // Crop
        IWriteableBitmap CropBitmap(
            IWriteableBitmap source,
            PixelRect rectangle);

        IWriteableBitmap CropBitmap(
            IWriteableBitmap source,
            PixelRect rectangle,
            Color replacedColor,
            Color newColor);

        IReadableBitmap CropIReadableBitmap(
            IReadableBitmap source,
            PixelRect rectangle,
            byte[]? pixelbuffer = null);


        // Scale / Change Size
        IWriteableBitmap Resize(
            IWriteableBitmap sourceBitmap,
            int newWidth,
            int newHeight);

        IWriteableBitmap ResizeHeightMonitored(
            IWriteableBitmap sourceBitmap,
            int newHeight,
            byte[] undoData);

        IWriteableBitmap ResizeWidthMonitored(
            IWriteableBitmap sourceBitmap,
            int newWidth,
            byte[] undoData);

        IWriteableBitmap ResizeScaled(
            IWriteableBitmap source,
            int targetWidth,
            int targetHeight = -1);

        IWriteableBitmap ResizeSorted(
            IWriteableBitmap oldBitmap,
            int newWidth,
            int tileSize,
            int newHeight = -1);


        // Insert / Fill
        void FillRectArray(
            IWriteableBitmap bitmap,
            PixelRect rect,
            byte[] pixels);

        void FillRectBitmapUnmonitored(
            IWriteableBitmap source,
            IWriteableBitmap target,
            (int X, int Y) pos,
            double opacity = 1.0,
            PlacingMode placingMode = PlacingMode.Default);

        void FillRectBitmap(
            IWriteableBitmap source,
            IWriteableBitmap target,
            (int X, int Y) pos,
            byte[] undoPixels,
            byte[] redoPixels,
            double opacity = 1.0,
            PlacingMode placingMode = PlacingMode.Default);

        void FillRectBitmapNoConvert(
            IWriteableBitmap source,
            IWriteableBitmap target,
            (int X, int Y) pos);

        void FillRectColor(
            IWriteableBitmap bitmap,
            PixelRect rect,
            Color? fillColor = null);


        // Pixel Operationens
        Color GetPixelBrush(
            IWriteableBitmap bitmap,
            int x,
            int y);

        byte[] GetRegionPixels(
            IWriteableBitmap bmp,
            PixelRect rect);

        IWriteableBitmap ReplaceColor(
            IWriteableBitmap source,
            Color replacedColor,
            Color newColor);


        // Transformations
        void FlipRectHor(
            IWriteableBitmap bitmap,
            PixelRect rectangle);

        void FlipRectVert(
            IWriteableBitmap bitmap,
            PixelRect rectangle);

        void RotateRec(
            IWriteableBitmap bitmap,
            PixelRect rectangle,
            bool counterclockwise = false);


        // Tile Manipulation
        void TileRally(
            IWriteableBitmap bitmap,
            (int X, int Y) sourcePos,
            (int X, int Y) targetPos,
            int tileSize);

        void TileSwitch(
            IWriteableBitmap bitmap,
            (int X, int Y) sourcePos,
            (int X, int Y) targetPos,
            int tileSize);
    }
}