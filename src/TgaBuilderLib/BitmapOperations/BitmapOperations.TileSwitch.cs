using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public void TileSwitch(IWriteableBitmap bitmap, (int X, int Y) sourcePos, (int X, int Y) targetPos, int tileSize)
        {
            IWriteableBitmap SourceTile = CropBitmap(bitmap, new PixelRect(sourcePos.X, sourcePos.Y, tileSize, tileSize));
            IWriteableBitmap TargetTile = CropBitmap(bitmap, new PixelRect(targetPos.X, targetPos.Y, tileSize, tileSize));

            FillRectBitmapNoConvert(SourceTile, bitmap, targetPos);
            FillRectBitmapNoConvert(TargetTile, bitmap, sourcePos);
        }
    }
}
