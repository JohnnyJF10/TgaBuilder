using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Psd;

public partial class PsdFile
{
    /// <summary>
    /// Saves a 32-bit RGBA PSD file containing the given layers.
    /// The merged/composite background image is computed from the layers.
    /// </summary>
    /// <param name="filename">Destination file path.</param>
    /// <param name="layerInfos">
    /// Layers to include in the PSD, listed from bottom to top.
    /// Each layer bitmap must be in BGRA 32-bit format.
    /// </param>
    public void Save(string filename, IEnumerable<PsdLayerInfo> layerInfos)
    {
        using var stream = new FileStream(filename, FileMode.Create, FileAccess.Write);
        Save(stream, layerInfos);
    }

    /// <summary>
    /// Writes a 32-bit RGBA PSD file to the given stream.
    /// The merged/composite background image is computed from the layers.
    /// </summary>
    /// <param name="stream">Output stream (must be writable and seekable).</param>
    /// <param name="layerInfos">
    /// Layers to include in the PSD, listed from bottom to top.
    /// Each layer bitmap must be in BGRA 32-bit format.
    /// </param>
    public void Save(Stream stream, IEnumerable<PsdLayerInfo> layerInfos)
    {
        var infoList = layerInfos.ToList();

        if (infoList.Count == 0)
            throw new ArgumentException("Error, list of Layer Infos cannot be empty");

        Version = 1;

        // The canvas spans the bounding box of all layer rectangles.
        // Smaller Images will put to the top-left corner of the canvas.
        Columns = infoList.Max(info => info.Rect.Right);
        Rows = infoList.Max(info => info.Rect.Bottom);

        Depth = 8;
        Channels = 4; // RGBA

        var writer = new BinaryReverseWriter(stream);
        var layerList = infoList.Select(info => new Layer(info, this)).ToList();

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

        // The merged image is the straight-alpha composite of all layers.
        var bgra = CompositeLayers(infoList, Columns, Rows);

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

    /// <summary>
    /// Composites the layers (index 0 = bottom) onto a transparent
    /// <paramref name="width"/> x <paramref name="height"/> canvas using the
    /// straight-alpha "source over" operator. Each layer is placed at its
    /// <see cref="PsdLayerInfo.Rect"/> offset, its alpha is scaled by
    /// <see cref="PsdLayerInfo.Opacity"/>, and invisible layers are skipped.
    /// Layer bitmaps are expected to be BGRA 32-bit. Blend modes other than
    /// Normal are not applied to the merged result.
    /// </summary>
    private static byte[] CompositeLayers(IReadOnlyList<PsdLayerInfo> layers, int width, int height)
    {
        var canvas = new byte[width * height * 4]; // BGRA, fully transparent

        foreach (var info in layers)
        {
            if (!info.Visible)
                continue;

            int layerW = info.Rect.Width;
            int layerH = info.Rect.Height;
            int offsetX = info.Rect.X;
            int offsetY = info.Rect.Y;
            float layerOpacity = info.Opacity / 255f;

            var src = new byte[layerW * layerH * 4];
            info.Bitmap.CopyPixels(new PixelRect(0, 0, layerW, layerH), src, layerW * 4, 0);

            for (int y = 0; y < layerH; y++)
            {
                int dy = y + offsetY;
                if (dy < 0 || dy >= height)
                    continue;

                int sRow = y * layerW * 4;
                int dRow = dy * width * 4;

                for (int x = 0; x < layerW; x++)
                {
                    int dx = x + offsetX;
                    if (dx < 0 || dx >= width)
                        continue;

                    int sPix = sRow + x * 4;
                    int dPix = dRow + dx * 4;

                    float sourceAlpha = (src[sPix + 3] / 255f) * layerOpacity;

                    if (sourceAlpha <= 0f)
                        continue;

                    float destinationAlpha = canvas[dPix + 3] / 255f;

                    float oa = sourceAlpha + destinationAlpha * (1f - sourceAlpha);

                    if (oa <= 0f)
                        continue;

                    for (int c = 0; c < 3; c++)
                    {
                        float sc = src[sPix + c];
                        float dc = canvas[dPix + c];
                        float oc = (sc * sourceAlpha + dc * destinationAlpha * (1f - sourceAlpha)) / oa;
                        canvas[dPix + c] = (byte)Math.Clamp(oc + 0.5f, 0f, 255f);
                    }

                    canvas[dPix + 3] = (byte)Math.Clamp(oa * 255f + 0.5f, 0f, 255f);
                }
            }
        }

        return canvas;
    }
}
