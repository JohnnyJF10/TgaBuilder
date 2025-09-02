using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public BitmapOperations(
            Func<int, int, bool, WriteableBitmap> bitmapFactory)
        {
            _bitmapFactory = bitmapFactory;
        }

        private readonly Func<int, int, bool, WriteableBitmap> _bitmapFactory;

        private WriteableBitmap GetNewWriteableBitmap(int width, int height, bool hasAlpha)
            => _bitmapFactory.Invoke(width, height, hasAlpha);
    }
}
