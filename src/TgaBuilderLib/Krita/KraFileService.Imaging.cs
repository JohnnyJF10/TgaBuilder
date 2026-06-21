namespace TgaBuilderLib.Krita;

public partial class KraFileService
{


    /// <summary>Encodes straight-alpha BGRA8888 pixels to a PNG byte array.</summary>
    public byte[] EncodePng(byte[] bgra, int width, int height)
    {
        var bitmap = _mediaFactory.CreateBitmapFromRaw(width, height, hasAlpha: true, bgra, stride: width * 4);
        return bitmap.ToMemoryStream().ToArray();
    }

    /// <summary>
    /// Composites layers (index 0 = bottom) onto a transparent WxH canvas using the
    /// straight-alpha "source over" operator. Smaller layers are placed at the top-left.
    /// </summary>
    private byte[] Composite(IReadOnlyList<LayerSource> layers, int width, int height)
    {
        var canvas = new byte[width * height * 4]; // BGRA, fully transparent

        foreach (var layer in layers)
        {
            // Hidden layers must not contribute to the flattened/preview image.
            if (!layer.Visible)
                continue;

            int layerBpp = layer.HasAlpha ? 4 : 3;

            for (int y = 0; y < layer.Height && y < height; y++)
            {
                int sRow = y * layer.Width * layerBpp;
                int dRow = y * width * 4;
                for (int x = 0; x < layer.Width && x < width; x++)
                {
                    int sPix = sRow + x * layerBpp;
                    int dPix = dRow + x * 4;

                    float sourceAlpha = layer.HasAlpha
                        ? layer.PixelBytes[sPix + 3] / 255f
                        : 1f;

                    if (sourceAlpha <= 0f)
                        continue;

                    float destinationAlpha = canvas[dPix + 3] / 255f;

                    float oa = sourceAlpha + destinationAlpha * (1f - sourceAlpha);

                    if (oa <= 0f)
                        continue;

                    for (int c = 0; c < 3; c++)
                    {
                        float sc = layer.PixelBytes[sPix + c];
                        float dc = canvas[dPix + c];
                        float oc = (sc * sourceAlpha + dc * destinationAlpha * (1f - sourceAlpha)) / oa;
                        canvas[dPix + c] = (byte)Math.Clamp(oc + 0.5f, 0f, 255f);
                    }

                    if (!layer.HasAlpha) // RGA: Swap R and B if no alpha, since source is probably RGB and canvas is BGRA
                    {
                        byte temp = canvas[dPix];
                        canvas[dPix] = canvas[dPix + 2];
                        canvas[dPix + 2] = temp;
                    }

                    canvas[dPix + 3] = (byte)Math.Clamp(oa * 255f + 0.5f, 0f, 255f);
                }
            }
        }
        return canvas;
    }

    /// <summary>Nearest-neighbour downscale (BGRA) so the longest side is at most <paramref name="maxSide"/>.</summary>
    private byte[] Downscale(byte[] bgra, int w, int h, int maxSide, out int outW, out int outH)
    {
        if (w <= maxSide && h <= maxSide)
        {
            outW = w; outH = h;
            return bgra;
        }
        float scale = maxSide / (float)Math.Max(w, h);
        outW = Math.Max(1, (int)(w * scale));
        outH = Math.Max(1, (int)(h * scale));
        var outBuf = new byte[outW * outH * 4];
        for (int y = 0; y < outH; y++)
        {
            int sy = Math.Min(h - 1, (int)(y / scale));
            for (int x = 0; x < outW; x++)
            {
                int sx = Math.Min(w - 1, (int)(x / scale));
                Buffer.BlockCopy(bgra, (sy * w + sx) * 4, outBuf, (y * outW + x) * 4, 4);
            }
        }
        return outBuf;
    }
}
