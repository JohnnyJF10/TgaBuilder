using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace THelperLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public void TileSwitch(WriteableBitmap bitmap, (int X, int Y) sourcePos, (int X, int Y) targetPos, int tileSize)
        {
            WriteableBitmap SourceTile = CropBitmap(bitmap, new Int32Rect(sourcePos.X, sourcePos.Y, tileSize, tileSize));
            WriteableBitmap TargetTile = CropBitmap(bitmap, new Int32Rect(targetPos.X, targetPos.Y, tileSize, tileSize));

            FillRectBitmapNoConvert(SourceTile, bitmap, targetPos);
            FillRectBitmapNoConvert(TargetTile, bitmap, sourcePos);
        }
    }
}
