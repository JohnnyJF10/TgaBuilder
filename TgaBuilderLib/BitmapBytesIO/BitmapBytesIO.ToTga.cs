using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderLib.Abstraction;

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

        private const short TR_TGA_BITS_PER_PIXEL_24 = 24;
        private const byte TR_TGA_IMAGE_DESCRIPTOR = 0x20;

        private const byte TR_TGA_FOOTER_SIZE = 26;
        private const uint TR_TGA_FOOTER_EXTENSION_AREA_OFFSET = 0;
        private const uint TR_TGA_FOOTER_DEVELOPER_DIRECTORY_OFFSET = 0;
        private const string TR_TGA_FOOTER_SIGNATURE = "TRUEVISION-XFILE.\0";
        private static readonly char[] TR_TGA_FOOTER_SIGNATURE_CHARS =
                { 'T', 'R', 'U', 'E', 'V', 'I', 'S', 'I', 'O', 'N', '-', 'X', 'F', 'I', 'L', 'E', '.', '\0' };

        private const int TR_LEVEL_PALLET_WIDTH = 256;
        private const int TR_LEVEL_PALLET_PAGE_SIZE = 65536;

        private const short TR_TGA_BITS_PER_PIXEL_32 = 32;
        private const byte TR_TGA_IMAGE_DESCRIPTOR_32 = 0x28; // 0x20 (top-left) + 0x08 (alpha bits)

        private bool _lastHadAlpha;

        public void ToTga(IReadableBitmap bitmap)
        {
            if (bitmap.HasAlpha)
                ToTga32(bitmap);
            else
                ToTga24(bitmap);

            _lastHadAlpha = bitmap.HasAlpha;
        }

        private void ToTga24(IReadableBitmap bitmap)
        {
            if (bitmap.HasAlpha)
                throw new ArgumentException("Bitmap must be in BGR24 format.");

            LoadedWidth = bitmap.PixelWidth;
            LoadedHeight = bitmap.PixelHeight;

            ActualDataLength = LoadedWidth * LoadedHeight * 3;

            LoadedBytes = _bytesPool.Rent(ActualDataLength);
            bitmap.CopyPixels(LoadedBytes, LoadedWidth * 3, 0);
        }

        private void ToTga32(IReadableBitmap bitmap)
        {
            if (!bitmap.HasAlpha)
                throw new ArgumentException("Bitmap must be in BGRA32 format.");

            LoadedWidth = bitmap.PixelWidth;
            LoadedHeight = bitmap.PixelHeight;

            ActualDataLength = LoadedWidth * LoadedHeight * 4;
            LoadedBytes = _bytesPool.Rent(ActualDataLength);
            bitmap.CopyPixels(LoadedBytes, LoadedWidth * 4, 0);
        }

        public void WriteTga(string filePath, CancellationToken? cancellationToken = null)
        {
            if (_lastHadAlpha)
                WriteTga32(filePath, cancellationToken);
            else
                WriteTga24(filePath, cancellationToken);
        }

        public void WriteTga24(string filePath, CancellationToken? cancellationToken = null)
        {
            if (LoadedBytes is null)
                throw new InvalidOperationException("No image data loaded. Please load an image first.");

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                int index = 0;
                WriteTrTgaHeader(bw, (short)LoadedWidth, (short)LoadedHeight, false);
                for (int s = LoadedHeight - 1; s >= 0; s--)
                {
                    for (int c = 0; c < LoadedWidth; c++)
                    {
                        cancellationToken?.ThrowIfCancellationRequested();

                        index = (s * LoadedWidth + c) * 3;

                        bw.Write(LoadedBytes[index + 2]);
                        bw.Write(LoadedBytes[index + 1]);
                        bw.Write(LoadedBytes[index    ]);
                    }
                }
                WriteTrTGaFooter(bw);
            }
        }

        public void WriteTga32(string filePath, CancellationToken? cancellationToken = null)
        {
            if (LoadedBytes is null)
                throw new InvalidOperationException("No image data loaded. Please load an image first.");

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                int index = 0;
                WriteTrTgaHeader(bw, (short)LoadedWidth, (short)LoadedHeight, true);
                for (int y = 0; y < LoadedHeight; y++)
                {
                    for (int x = 0; x < LoadedWidth; x++)
                    {
                        cancellationToken?.ThrowIfCancellationRequested();

                        index = (y * LoadedWidth + x) * 4;

                        bw.Write(LoadedBytes[index    ]); // Blue
                        bw.Write(LoadedBytes[index + 1]); // Green
                        bw.Write(LoadedBytes[index + 2]); // Red
                        bw.Write(LoadedBytes[index + 3]); // Alpha
                    }
                }
                WriteTrTGaFooter(bw);
            }
        }

        private void WriteTrTgaHeader(BinaryWriter bw, short width, short height, bool hasAlpha)
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
            bw.Write(hasAlpha ? TR_TGA_BITS_PER_PIXEL_32 : TR_TGA_BITS_PER_PIXEL_24);

            if (hasAlpha)
                bw.Write((byte)TR_TGA_IMAGE_DESCRIPTOR_32);
        }

        private void WriteTrTGaFooter(BinaryWriter bw)
        {
            bw.Write(TR_TGA_FOOTER_EXTENSION_AREA_OFFSET);
            bw.Write(TR_TGA_FOOTER_DEVELOPER_DIRECTORY_OFFSET);
            bw.Write(TR_TGA_FOOTER_SIGNATURE_CHARS);
        }

    }
}
