using System.Runtime.InteropServices;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Krita;

public partial class KraFileService
{


    /// <summary>Encodes straight-alpha BGRA8888 pixels to a PNG byte array via Avalonia.</summary>
    public byte[] EncodePng(byte[] bgra, int width, int height)
    {
        var bitmap = _mediaFactory.CreateBitmapFromRaw(width, height, hasAlpha: true, bgra, stride: width * 4);
        return bitmap.ToMemoryStream().ToArray();
    }

    /// <summary>
    /// Composites layers (index 0 = bottom) onto a transparent WxH canvas using the
    /// straight-alpha "source over" operator. Smaller layers are placed at the top-left.
    /// </summary>
    private byte[] Composite(IReadOnlyList<LayerSource> layers, int w, int h)
    {
        var canvas = new byte[w * h * 4]; // BGRA, fully transparent

        foreach (var l in layers)
        {
            for (int y = 0; y < l.Height && y < h; y++)
            {
                int sRow = y * l.Width * 4;
                int dRow = y * w * 4;
                for (int x = 0; x < l.Width && x < w; x++)
                {
                    int s = sRow + x * 4;
                    int d = dRow + x * 4;

                    float sa = l.Bgra[s + 3] / 255f;
                    if (sa <= 0f) continue;
                    float da = canvas[d + 3] / 255f;
                    float oa = sa + da * (1f - sa);
                    if (oa <= 0f) continue;

                    for (int c = 0; c < 3; c++)
                    {
                        float sc = l.Bgra[s + c];
                        float dc = canvas[d + c];
                        float oc = (sc * sa + dc * da * (1f - sa)) / oa;
                        canvas[d + c] = (byte)Math.Clamp(oc + 0.5f, 0f, 255f);
                    }
                    canvas[d + 3] = (byte)Math.Clamp(oa * 255f + 0.5f, 0f, 255f);
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
