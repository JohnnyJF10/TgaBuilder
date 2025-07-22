using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TgaBuilderLib.BitmapBytesIO
{
    public partial class BitmapBytesIO
    {
        private const byte TR_TGA_DIMENSION_POSITION = 0x0C;
        private const byte TR_TGA_HEADER_LENGTH = 0x12;

        private const byte TR_TGA_ID_LENGTH = 0;
        private const byte TR_TGA_COLOR_MAP_TYPE = 0;
        private const byte TR_TGA_DATA_TYPE_CODE = 2;
        private const short TR_TGA_COLOR_MAP_ORIGIN = 0;
        private const short TR_TGA_COLOR_MAP_LENGTH = 0;
        private const byte TR_TGA_COLOR_MAP_DEPTH = 0;
        private const short TR_TGA_X_ORIGIN = 0;
        private const short TR_TGA_Y_ORIGIN = 0;

        private const short TR_TGA_BITS_PER_PIXEL = 24;
        private const byte TR_TGA_IMAGE_DESCRIPTOR = 0x20;

        private const byte TR_TGA_FOOTER_SIZE = 26;
        private const uint TR_TGA_FOOTER_EXTENSION_AREA_OFFSET = 0;
        private const uint TR_TGA_FOOTER_DEVELOPER_DIRECTORY_OFFSET = 0;
        private const string TR_TGA_FOOTER_SIGNATURE = "TRUEVISION-XFILE.\0";
        private static readonly char[] TR_TGA_FOOTER_SIGNATURE_CHARS =
                { 'T', 'R', 'U', 'E', 'V', 'I', 'S', 'I', 'O', 'N', '-', 'X', 'F', 'I', 'L', 'E', '.', '\0' };

        private const int TR_LEVEL_PALLET_WIDTH = 256;
        private const int TR_LEVEL_PALLET_PAGE_SIZE = 65536;

        public void ToTga(BitmapSource bitmap)
        {
            if (bitmap.Format != PixelFormats.Rgb24)
            {
                throw new ArgumentException("Bitmap must be in BGR24 format.");
            }
            LoadedWidth = bitmap.PixelWidth;
            LoadedHeight = bitmap.PixelHeight;

            ActualDataLength = LoadedWidth * LoadedHeight * 3;

            LoadedBytes = _bytesPool.Rent(ActualDataLength);
            bitmap.CopyPixels(LoadedBytes, LoadedWidth * 3, 0);
        }

        public void WriteTga(string filePath)
        {
            if (LoadedBytes is null)
                throw new InvalidOperationException("No image data loaded. Please load an image first.");

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                int index = 0;
                WriteTrTgaHeader(bw, (short)LoadedWidth, (short)LoadedHeight);
                for (int s = LoadedHeight - 1; s >= 0; s--)
                {
                    for (int c = 0; c < LoadedWidth; c++)
                    {
                        index = (s * LoadedWidth + c) * 3;
                        bw.Write(LoadedBytes[index + 2]);
                        bw.Write(LoadedBytes[index + 1]);
                        bw.Write(LoadedBytes[index]);
                    }
                }
                WriteTrTGaFooter(bw);
            }
        }

        private void WriteTrTgaHeader(BinaryWriter bw, short width, short height)
        {
            bw.Write(TR_TGA_ID_LENGTH);
            bw.Write(TR_TGA_COLOR_MAP_TYPE);
            bw.Write(TR_TGA_DATA_TYPE_CODE);
            bw.Write(TR_TGA_COLOR_MAP_ORIGIN);
            bw.Write(TR_TGA_COLOR_MAP_LENGTH);
            bw.Write(TR_TGA_COLOR_MAP_DEPTH);
            bw.Write(TR_TGA_X_ORIGIN);
            bw.Write(TR_TGA_Y_ORIGIN);
            bw.Write(width);
            bw.Write(height);
            bw.Write(TR_TGA_BITS_PER_PIXEL);
        }

        private void WriteTrTGaFooter(BinaryWriter bw)
        {

            bw.Write(TR_TGA_FOOTER_EXTENSION_AREA_OFFSET);
            bw.Write(TR_TGA_FOOTER_DEVELOPER_DIRECTORY_OFFSET);
            bw.Write(TR_TGA_FOOTER_SIGNATURE_CHARS);
        }

    }
}
