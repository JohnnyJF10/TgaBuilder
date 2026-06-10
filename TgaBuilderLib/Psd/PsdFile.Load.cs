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
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BEHelpers LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion

using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Psd;

public partial class PsdFile
{
    public PsdFile? Load(string filename, IMediaFactory? mediaFactory = null)
    {
        using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
        {
            return Load(stream, mediaFactory);
        }
    }

    public PsdFile? Load(byte[] data, IMediaFactory? mediaFactory = null)
    {
        var stream = new MemoryStream(data);

        return Load(stream, mediaFactory);
    }

    public PsdFile? Load(Stream stream, IMediaFactory? mediaFactory = null)
    {
        //binary reverse reader reads data types in big-endian format.
        BinaryReverseReader reader = new BinaryReverseReader(stream);

        #region "Headers"
        //The headers area is used to check for a valid PSD file

        string signature = new string(reader.ReadChars(4));

        if (signature != "8BPS") throw new IOException("Bad or invalid file stream supplied");

        //get the version number, should be 1 always
        if ((Version = reader.ReadInt16()) != 1) throw new IOException("Invalid version number supplied");

        //get rid of the 6 bytes reserved in PSD format
        reader.BaseStream.Position += 6;

        //get the rest of the information from the PSD file.
        //Every time ReadInt16() is called, it reads 2 bytes.
        //Every time ReadInt32() is called, it reads 4 bytes.
        _channels = reader.ReadInt16();
        _rows = reader.ReadInt32();
        _columns = reader.ReadInt32();
        _depth = reader.ReadInt16();
        ColorMode = (ColorMode)reader.ReadInt16();

        //by end of headers, the reader has read 26 bytes into the file.
        #endregion //End Headers

        #region "ColorModeData"

        uint paletteLength = reader.ReadUInt32(); //readUint32() advances the reader 4 bytes.
        if (paletteLength > 0)
        {
            ColorModeData = reader.ReadBytes((int)paletteLength);
        }
        #endregion //End ColorModeData

        #region "Loading Image Resources"
        //This part takes extensive use of classes that I didn't write therefore
        //I can't document much on what they do.


        _imageResources.Clear();

        uint imgResLength = reader.ReadUInt32();
        if (imgResLength > 0)
        {
            long startPosition = reader.BaseStream.Position;

            while (reader.BaseStream.Position - startPosition < imgResLength)
            {
                ImageResource imgRes = new ImageResource(reader);

                ResourceIDs resID = (ResourceIDs)imgRes.ID;
                switch (resID)
                {
                    case ResourceIDs.ResolutionInfo:
                        imgRes = new ResolutionInfo(imgRes);
                        break;
                    case ResourceIDs.GridGuidesInfo:
                        imgRes = new GridGuidesInfo(imgRes);
                        break;
                    case ResourceIDs.Thumbnail1:
                    case ResourceIDs.Thumbnail2:
                        imgRes = new Thumbnail(imgRes, mediaFactory);
                        break;
                    case ResourceIDs.AlphaChannelNames:
                        imgRes = new AlphaChannels(imgRes);
                        break;
                }

                _imageResources.Add(imgRes);
            }
            // make sure we are not on a wrong offset, so set the stream position 
            // manually
            reader.BaseStream.Position = startPosition + imgResLength;
        }

        #endregion //End LoadingImageResources

        #region "Layer and Mask Info"
        //We are gonna load up all the layers and masking of the PSD now.
        uint layersAndMaskLength = reader.ReadUInt32();

        if (layersAndMaskLength > 0)
        {
            //new start position
            long startPosition = reader.BaseStream.Position;

            //Lets start by loading up all the layers
            LoadLayers(reader);
            //we are done the layers, load up the masks
            LoadGlobalLayerMask(reader);

            // make sure we are not on a wrong offset, so set the stream position 
            // manually

            // Krita is adding the patterns block here. If now patterns are included, it has a lengsth of 12 bytes.
            reader.BaseStream.Position = startPosition + layersAndMaskLength;
        }
        #endregion //End Layer and Mask info

        #region "Loading Final Image"

        //we have loaded up all the information from the PSD file
        //into variables we can use later on.

        //lets finish loading the raw data that defines the image 
        //in the picture.


        ImageCompression = (ImageCompression)reader.ReadInt16();

        ImageData = new byte[_channels][];

        var rowLengthTable = new ushort[_channels][];

        //---------------------------------------------------------------

        if (ImageCompression == ImageCompression.Rle)
        {
            // The RLE-compressed data is proceeded by a 2-byte data count for each row in the data,
            // which we're going to just skip.

            for (int ch = 0; ch < _channels; ch++)
            {
                rowLengthTable[ch] = new ushort[_rows];
                for (int i = 0; i < _rows; i++)
                {
                    rowLengthTable[ch][i] = reader.ReadUInt16();
                }
            }
        }

        //---------------------------------------------------------------

        int bytesPerRow = 0;

        switch (_depth)
        {
            case 1:
                bytesPerRow = _columns;//NOT Sure
                break;
            case 8:
                bytesPerRow = _columns;
                break;
            case 16:
                bytesPerRow = _columns * 2;
                break;
        }

        //---------------------------------------------------------------

        for (int ch = 0; ch < _channels; ch++)
        {
            ImageData[ch] = new byte[_rows * bytesPerRow];

            switch (ImageCompression)
            {
                case ImageCompression.Raw:
                    reader.Read(ImageData[ch], 0, ImageData[ch].Length);
                    break;

                case ImageCompression.Rle:
                    {
                        for (int i = 0; i < _rows; i++)
                        {
                            int rowIndex = i * bytesPerRow;

                            int compressedLength = rowLengthTable[ch][i];

                            RleHelper.DecodedRow(reader.BaseStream, ImageData[ch], rowIndex, bytesPerRow, compressedLength);
                        }
                    }
                    break;
            }
        }

        #endregion //End LoadingFinalImage

        return this;
    }

    /// <summary>
    /// Loads up the Layers of the supplied PSD file.
    /// </summary>      
    private void LoadLayers(BinaryReverseReader reader)
    {

        uint layersInfoSectionLength = reader.ReadUInt32();

        if (layersInfoSectionLength <= 0)
            return;

        long startPosition = reader.BaseStream.Position;

        short numberOfLayers = reader.ReadInt16();

        // If <0, then number of layers is absolute value,
        // and the first alpha channel contains the transparency data for
        // the merged result.
        if (numberOfLayers < 0)
        {
            AbsoluteAlpha = true;
            numberOfLayers = Math.Abs(numberOfLayers);
        }

        _layers.Clear();

        if (numberOfLayers == 0) return;

        for (int i = 0; i < numberOfLayers; i++)
        {
            _layers.Add(new Layer(reader, this));
        }

        foreach (Layer layer in Layers)
        {
            foreach (Layer.Channel channel in layer.Channels.Where(c => c.ID != -2))
            {
                channel.LoadPixelData(reader);
            }
            layer.MaskData?.LoadPixelData(reader);
        }


        if (reader.BaseStream.Position % 2 == 1) reader.ReadByte();

        // make sure we are not on a wrong offset, so set the stream position 
        // manually
        reader.BaseStream.Position = startPosition + layersInfoSectionLength;
    }

    /// <summary>
    /// Load up the masking information of the supplied PSD
    /// </summary>        
    private void LoadGlobalLayerMask(BinaryReverseReader reader)
    {

        uint maskLength = reader.ReadUInt32();

        if (maskLength <= 0) return;

        _globalLayerMaskData = reader.ReadBytes((int)maskLength);
    }
}
