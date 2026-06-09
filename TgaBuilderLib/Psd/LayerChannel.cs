#region Licence
/*
Copyright (c) 2013, Darren Horrocks
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the <organization> nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace TgaBuilderLib.Psd
{
    public partial class Layer
    {
        public class Channel
        {
            /// <summary>
            /// The layer to which this channel belongs.
            /// </summary>
            public Layer Layer { get; private set; }

            /// <summary>
            /// 0 = red, 1 = green, etc.
            /// 1 = transparency mask
            /// 2 = user supplied layer mask
            /// </summary>
            public short ID { get; private set; }

            /// <summary>
            /// The length of the compressed channel data.
            /// </summary>
            public int Length { get; private set; }

            /// <summary>
            /// The compressed raw channel data.
            /// </summary>
            public byte[] Data { get; set; } = null!;

            public byte[] ImageData { get; set; } = null!;

            public ImageCompression ImageCompression { get; set; }

            internal Channel(short id, Layer layer)
            {
                ID = id;
                Layer = layer;
                Layer.Channels.Add(this);
                Layer.SortedChannels.Add(ID, this);
            }

            internal Channel(BinaryReverseReader reverseReader, Layer layer)
            {

                ID = reverseReader.ReadInt16();
                Length = reverseReader.ReadInt32();

                Layer = layer;
            }

            internal void Save(BinaryReverseWriter reverseWriter)
            {

                reverseWriter.Write(ID);

                CompressImageData();

                reverseWriter.Write(Data.Length + 2); // 2 bytes for the image compression
            }

            internal void LoadPixelData(BinaryReverseReader reverseReader)
            {
                Data = reverseReader.ReadBytes(Length);

                using (BinaryReverseReader imageReader = DataReader!)
                {
                    ImageCompression = (ImageCompression)imageReader.ReadInt16();

                    int bytesPerRow = 0;
                    switch (Layer.PsdFile.Depth)
                    {
                        case 1:
                            bytesPerRow = Layer.Rect.Width; // NOT sure
                            break;
                        case 8:
                            bytesPerRow = Layer.Rect.Width;
                            break;
                        case 16:
                            bytesPerRow = Layer.Rect.Width * 2;
                            break;
                    }

                    ImageData = new byte[Layer.Rect.Height * bytesPerRow];

                    switch (ImageCompression)
                    {
                        case ImageCompression.Raw:
                            imageReader.Read(ImageData, 0, ImageData.Length);
                            break;

                        case ImageCompression.Rle:
                            {
                                var rowLengthList = new int[Layer.Rect.Height];

                                for (int i = 0; i < rowLengthList.Length; i++)
                                {
                                    rowLengthList[i] = imageReader.ReadInt16();
                                }

                                for (int i = 0; i < Layer.Rect.Height; i++)
                                {
                                    int rowIndex = i * bytesPerRow;

                                    // Here we optionally check the list and the current index
                                    int compressedLength = rowLengthList[i];
                                    RleHelper.DecodedRow(imageReader.BaseStream, ImageData, rowIndex, bytesPerRow, compressedLength);
                                }
                            }
                            break;
                    }
                }
            }

            private void CompressImageData()
            {
                if (ImageCompression == ImageCompression.Rle)
                {
                    var memoryStream = new MemoryStream();
                    var reverseWriter = new BinaryReverseWriter(memoryStream);

                    // we will write the correct lengths later, so remember 
                    // the position
                    long lengthPosition = reverseWriter.BaseStream.Position;

                    var rleRowLengths = new int[Layer.Rect.Height];

                    if (ImageCompression == ImageCompression.Rle)
                    {
                        for (int i = 0; i < rleRowLengths.Length; i++)
                        {
                            reverseWriter.Write((short)0x1234);
                        }
                    }

                    int bytesPerRow = 0;

                    switch (Layer.PsdFile.Depth)
                    {
                        case 1:
                            bytesPerRow = Layer.Rect.Width;//NOT Sure
                            break;
                        case 8:
                            bytesPerRow = Layer.Rect.Width;
                            break;
                        case 16:
                            bytesPerRow = Layer.Rect.Width * 2;
                            break;
                    }

                    for (int row = 0; row < Layer.Rect.Height; row++)
                    {
                        int rowIndex = row * bytesPerRow;
                        rleRowLengths[row] = RleHelper.EncodedRow(reverseWriter.BaseStream, ImageData, rowIndex, bytesPerRow);
                    }

                    long endPosition = reverseWriter.BaseStream.Position;

                    reverseWriter.BaseStream.Position = lengthPosition;

                    foreach (int length in rleRowLengths)
                    {
                        reverseWriter.Write((short)length);
                    }

                    reverseWriter.BaseStream.Position = endPosition;

                    memoryStream.Close();

                    Data = memoryStream.ToArray();

                    memoryStream.Dispose();

                }
                else
                {
                    Data = (byte[])ImageData.Clone();
                }
            }

            internal void SavePixelData(BinaryReverseWriter writer)
            {

                writer.Write((short)ImageCompression);
                if (ImageCompression == ImageCompression.Rle)
                {
                    if (Data == null || Data.Length == 0)
                        CompressImageData();
                    if (Data == null)
                        throw new InvalidDataException("RLE channel data is missing.");
                    writer.Write(Data);
                }
                else
                {
                    writer.Write(ImageData);
                }
            }

            public BinaryReverseReader? DataReader
            {
                get => Data == null ? null : new BinaryReverseReader(new MemoryStream(Data));
            }
        }
    }
}
