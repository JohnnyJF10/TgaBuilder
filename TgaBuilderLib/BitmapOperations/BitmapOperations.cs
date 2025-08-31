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
            Func<int, int, int, WriteableBitmap> bitmapFactory)
        {
            _bitmapFactory = bitmapFactory;
        }

        private readonly Func<int, int, int, WriteableBitmap> _bitmapFactory;

        private WriteableBitmap GetNewWriteableBitmap(int width, int height, int bytesPerPixel)
            => _bitmapFactory.Invoke(width, height, bytesPerPixel);
    }
}
