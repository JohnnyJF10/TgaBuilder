using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace TgaBuilderLib.BitmapIO
{
    public partial class BitmapIO
    {

        public void ToUsual(string fileName, string extension, BitmapSource bitmap)
        {
            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                BitmapEncoder encoder = extension switch
                {
                    "png" => new PngBitmapEncoder(),
                    "jpg" or "jpeg" => new JpegBitmapEncoder(),
                    "bmp" => new BmpBitmapEncoder(),
                    _ => throw new NotSupportedException($"Unsupported file format: {extension}")
                };

                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(fileStream);
            }
        }

    }
}
