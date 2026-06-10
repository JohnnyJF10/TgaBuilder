using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Psd;

public partial class PsdFile
{
    /// <summary>
    /// Saves a 32-bit RGBA PSD file containing a merged background image and a set of layers.
    /// </summary>
    /// <param name="filename">Destination file path.</param>
    /// <param name="background">
    /// Merged/composite background bitmap in BGRA 32-bit format.
    /// Its dimensions define the PSD canvas size.
    /// </param>
    /// <param name="layerInfos">
    /// Layers to include in the PSD, listed from bottom to top.
    /// Each layer bitmap must be in BGRA 32-bit format.
    /// </param>
    public void Save(string filename, IReadableBitmap background, IEnumerable<PsdLayerInfo> layerInfos)
    {
        using var stream = new FileStream(filename, FileMode.Create, FileAccess.Write);
        Save(stream, background, layerInfos);
    }

    /// <summary>
    /// Writes a 32-bit RGBA PSD file to the given stream.
    /// </summary>
    /// <param name="stream">Output stream (must be writable and seekable).</param>
    /// <param name="background">
    /// Merged/composite background bitmap in BGRA 32-bit format.
    /// Its dimensions define the PSD canvas size.
    /// </param>
    /// <param name="layerInfos">
    /// Layers to include in the PSD, listed from bottom to top.
    /// Each layer bitmap must be in BGRA 32-bit format.
    /// </param>
    public void Save(Stream stream, IReadableBitmap background, IEnumerable<PsdLayerInfo> layerInfos)
    {
        Version = 1;

        Columns = background.PixelWidth;
        Rows = background.PixelHeight;

        Depth = 8;
        Channels = 4; // RGBA

        var writer = new BinaryReverseWriter(stream);
        var layerList = layerInfos.Select(info => new Layer(info, this)).ToList();

        if (!layerList.Any())
            throw new ArgumentException("Error, list of Layer Infos cannot be empty");

        // ── Header ──────────────────────────────────────────────────────────
        writer.Write("8BPS".ToCharArray());  // PSD signature
        writer.Write(Version);               // version (always 1)
        writer.Write(new byte[6]);           // reserved (6 zero bytes)
        writer.Write(Channels);              // number of channels (RGBA = 4)
        writer.Write(Rows);                  // rows
        writer.Write(Columns);               // columns
        writer.Write((short)Depth);          // bits per channel (8)
        writer.Write((short)ColorMode.RGB);  // colour mode

        // ── Colour Mode Data (empty for RGB) ────────────────────────────────
        writer.Write((uint)0);

        // ── Image Resources (ResolutionInfo: 100 DPI) ───────────────────────
        using (var imgResStream = new MemoryStream())
        {
            var imgResWriter = new BinaryReverseWriter(imgResStream);

            new ResolutionInfo(100, 100).Save(imgResWriter);

            new GridGuidesInfo().Save(imgResWriter);

            new IccResource().Save(imgResWriter);

            imgResWriter.Flush();

            byte[] imgResBytes = imgResStream.ToArray();

            writer.Write((uint)imgResBytes.Length);
            writer.Write(imgResBytes);
        }

        // ── Layer and Mask Info ──────────────────────────────────────────────
        using (new LengthWriter(writer))
        {
            // ── Layer Info ──
            using (new LengthWriter(writer))
            {
                // Negative count: signals that the merged image's first alpha
                // channel carries the transparency of the flattened result.
                writer.Write((short)-layerList.Count);

                // Layer record headers (rect, channel list, blend info, name …)
                foreach (var layer in layerList)
                    layer.Save(writer);

                // Channel pixel data for every layer
                foreach (var layer in layerList)
                    foreach (var channel in layer.Channels)
                        channel.SavePixelData(writer);

                // Pad the layer info to an even byte boundary
                if (writer.BaseStream.Position % 2 == 1)
                    writer.Write((byte)0);
            }

            // ── Global Layer Mask (none) ──
            writer.Write((uint)0);

            // ── Patterns block ──

            // Krita adds a patterns block here, even if no patterns are included. 
            // Might be mandaory for a valid PSD.
            string patterns = "8BIMPatt\0\0\0\0";
            writer.Write(patterns.ToCharArray());
        }

        // ── Merged Image Data (RLE compressed) ───────────────────────────────
        writer.Write((short)ImageCompression.Rle);

        int pixelCount = Columns * Rows;
        var bgra = new byte[pixelCount * 4];
        background.CopyPixels(new PixelRect(0, 0, Columns, Rows), bgra, Columns * 4, 0);

        // PSD stores channels as separate planar arrays: R, G, B, A
        // BGRA layout: [0]=B  [1]=G  [2]=R  [3]=A
        var rPlane = new byte[pixelCount];
        var gPlane = new byte[pixelCount];
        var bPlane = new byte[pixelCount];
        var aPlane = new byte[pixelCount];

        for (int i = 0; i < pixelCount; i++)
        {
            bPlane[i] = bgra[i * 4];
            gPlane[i] = bgra[i * 4 + 1];
            rPlane[i] = bgra[i * 4 + 2];
            aPlane[i] = bgra[i * 4 + 3];
        }

        var channelPlanes = new[] { rPlane, gPlane, bPlane, aPlane };
        var rowLengthTable = new ushort[Channels][];
        var compressedChannelData = new byte[Channels][];

        for (int ch = 0; ch < Channels; ch++)
        {
            rowLengthTable[ch] = new ushort[Rows];

            using var channelStream = new MemoryStream();

            for (int row = 0; row < Rows; row++)
            {
                int rowIndex = row * Columns;
                int encodedLength = RleHelper.EncodedRow(channelStream, channelPlanes[ch], rowIndex, Columns);

                if (encodedLength > ushort.MaxValue)
                    throw new InvalidDataException("RLE row length exceeds PSD 16-bit row length limit.");

                rowLengthTable[ch][row] = (ushort)encodedLength;
            }

            compressedChannelData[ch] = channelStream.ToArray();
        }

        for (int ch = 0; ch < Channels; ch++)
        {
            for (int row = 0; row < Rows; row++)
            {
                writer.Write(rowLengthTable[ch][row]);
            }
        }

        for (int ch = 0; ch < Channels; ch++)
        {
            writer.Write(compressedChannelData[ch]);
        }
    }
}
