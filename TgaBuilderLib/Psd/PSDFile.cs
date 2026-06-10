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

namespace TgaBuilderLib.Psd;

public partial class PsdFile
{
    private List<Layer> _layers;
    private byte[] _globalLayerMaskData = Array.Empty<byte>(); // Masking data for the PSD
    private short _channels;
    private int _rows;
    private int _depth;
    private int _columns;

    public PsdFile()
    {
        _layers = new List<Layer>();
        Version = 1;
        _imageResources = new List<ImageResource>();
    }

    /// <summary>
    /// If ColorMode is ColorModes.Indexed, the following 768 bytes will contain 
    /// a 256-color palette. If the ColorMode is ColorModes.Duotone, the data 
    /// following presumably consists of screen parameters and other related information. 
    /// Unfortunately, it is intentionally not documented by Adobe, and non-Photoshop 
    /// readers are advised to treat duotone images as gray-scale images.
    /// </summary>
    public byte[] ColorModeData = Array.Empty<byte>();

    public short Version { get; private set; }


    /// <summary>
    /// The number of channels in the image, including any alpha channels.
    /// Supported range is 1 to 24.
    /// </summary>
    public short Channels
    {
        get { return _channels; }
        private set
        {
            if (value < 1 || value > 24) throw new ArgumentException("Supported range is 1 to 24");
            _channels = value;
        }
    }

    /// <summary>
    /// The height of the image in pixels.
    /// </summary>
    public int Rows
    {
        get => _rows;
        private set
        {
            if (value < 0 || value > 30000) throw new ArgumentException("Supported range is 1 to 30000.");
            _rows = value;
        }
    }

    /// <summary>
    /// The width of the image in pixels. 
    /// </summary>
    public int Columns
    {
        get => _columns;
        private set
        {
            if (value < 0 || value > 30000)
                throw new ArgumentException("Supported range is 1 to 30000.");
            _columns = value;
        }
    }

    /// <summary>
    /// The number of bits per channel. Supported values are 1, 8, and 16.
    /// </summary>
    public int Depth
    {
        get => _depth;
        private set
        {
            if (value == 1 || value == 8 || value == 16)
            {
                _depth = value;
            }
            else
            {
                throw new ArgumentException("Supported values are 1, 8, and 16.");
            }
        }
    }

    /// <summary>
    /// The color mode of the file.
    /// </summary>
    public ColorMode ColorMode { get; private set; }

    public IEnumerable<Layer> Layers => _layers;

    public bool AbsoluteAlpha { get; private set; }

    public byte[][]? ImageData { get; private set; }

    public ImageCompression ImageCompression { get; private set; }

    private List<ImageResource> _imageResources;

    /// <summary>
    /// The Image resource blocks for the file
    /// </summary>
    public IEnumerable<ImageResource> ImageResources => _imageResources;

    public ResolutionInfo? Resolution
    {
        get => (ResolutionInfo?)_imageResources.Find(x => x.ID == (int)ResourceIDs.ResolutionInfo);

        private set
        {
            ImageResource? oldValue = _imageResources.Find(x => x.ID == (int)ResourceIDs.ResolutionInfo);
            if (oldValue != null)
            {
                _imageResources.Remove(oldValue);
            }

            if (value != null)
                _imageResources.Add(value);
        }
    }
}