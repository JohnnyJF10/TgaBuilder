using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace TgaBuilderLib.BitmapBytesIO
{
    public partial class BitmapBytesIO
    {

        public void ToUsual(BitmapSource bitmap, string extension)
        {
            // ToDo: Implement cancellation logic if needed.
            BitmapEncoder encoder = extension.ToLower() switch
            {
                "png" => new PngBitmapEncoder(),
                "jpg" or "jpeg" => new JpegBitmapEncoder(),
                "bmp" => new BmpBitmapEncoder(),
                _ => throw new NotSupportedException($"Unsupported file format: {extension}")
            };

            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var memoryStream = new MemoryStream();
            encoder.Save(memoryStream);

            ActualDataLength = (int)memoryStream.Length;

            LoadedBytes = _bytesPool.Rent(ActualDataLength);

            memoryStream.Position = 0;

            memoryStream.Read(LoadedBytes, 0, ActualDataLength);
        }

        public void WriteUsual(string fileName, CancellationToken? cancellationToken = null)
        {
            if (LoadedBytes is null)
                throw new InvalidOperationException("No data loaded to save.");

            using var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            if (cancellationToken is not CancellationToken token)
                fileStream.WriteAsync(LoadedBytes, 0, ActualDataLength);
            else
                fileStream.WriteAsync(LoadedBytes, 0, ActualDataLength, token);

        }
    }
}
