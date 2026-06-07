using System.Text;

namespace TgaBuilderLib.Krita;

internal class LayerData
{
    private const int TileW = 64;
    private const int TileH = 64;
    private const int KritaPixelSize = 4; // RGBA8 -> 4 bytes, stored as B,G,R,A (Krita's native order)

    internal LayerSource Layer { get; set; }

    internal LayerData(LayerSource layer)
    {
        Layer = layer;
    }

    /// <summary>
    /// Serializes a paint layer's pixels into Krita's tiled paint-device format:
    ///   VERSION/TILEWIDTH/TILEHEIGHT/PIXELSIZE/DATA header, then one block per
    ///   non-empty 64x64 tile: "&lt;x&gt;,&lt;y&gt;,LZF,&lt;size&gt;\n" followed by
    ///   [compressionFlag][payload].
    /// </summary>
    internal byte[] Build()
    {
        int tilesX = (Layer.Width + TileW - 1) / TileW;
        int tilesY = (Layer.Height + TileH - 1) / TileH;

        const int tileBytes = TileW * TileH * KritaPixelSize; // Assumes KritaPixelSize is 4 for BGRA
        var compressBuf = new byte[tileBytes + tileBytes / 16 + 64];
        var tile = new byte[tileBytes];

        var bodies = new List<byte[]>();
        int channelSize = TileW * TileH; // Size of a single channel plane within the tile

        int pixelSize = Layer.HasAlpha ? 4 : 3; // Source pixel size (BGRA or RGB)

        byte b, g, r, a;

        for (int ty = 0; ty < tilesY; ty++)
        {
            for (int tx = 0; tx < tilesX; tx++)
            {
                Array.Clear(tile, 0, tile.Length); // areas outside the image stay transparent

                bool anyContent = false;
                for (int row = 0; row < TileH; row++)
                {
                    int srcY = ty * TileH + row;
                    if (srcY >= Layer.Height) break;

                    int maxCol = Math.Min(TileW, Layer.Width - tx * TileW);
                    int srcBase = (srcY * Layer.Width + tx * TileW) * pixelSize;

                    for (int col = 0; col < maxCol; col++)
                    {
                        int srcIdx = srcBase + col * pixelSize;
                        int pixelIndex = row * TileW + col;

                        // Read interleaved from source
                        if (Layer.HasAlpha) // BGRA32 source
                        {
                            b = Layer.PixelBytes[srcIdx + 0]; //B
                            g = Layer.PixelBytes[srcIdx + 1]; //G
                            r = Layer.PixelBytes[srcIdx + 2]; //R
                            a = Layer.PixelBytes[srcIdx + 3]; //A
                        }
                        else // RGB24 source
                        {
                            r = Layer.PixelBytes[srcIdx + 0]; //R
                            g = Layer.PixelBytes[srcIdx + 1]; //G
                            b = Layer.PixelBytes[srcIdx + 2]; //B
                            a = 255; // Opaque if no alpha channel
                        }

                        // Write into planar BBB...GGG...RRR...AAA... destination
                        tile[/* 0 *    channelSize + */ pixelIndex] = b;
                        tile[/* 1 *  */channelSize +    pixelIndex] = g;
                        tile[   2 *    channelSize +    pixelIndex] = r;
                        tile[   3 *    channelSize +    pixelIndex] = a;

                        if (!anyContent && (r != 0 || g != 0 || b != 0 || a != 0))
                        {
                            anyContent = true;
                        }
                    }
                }

                if (!anyContent)
                    continue; // fully transparent -> falls back to the default pixel

                int xPix = tx * TileW;
                int yPix = ty * TileH;

                int compressed = Lzf.Compress(tile, tileBytes, compressBuf);

                byte[] payload;
                byte flag;
                if (compressed > 0 && compressed < tileBytes)
                {
                    flag = 1; // LZF compressed
                    payload = new byte[compressed];
                    Buffer.BlockCopy(compressBuf, 0, payload, 0, compressed);
                }
                else
                {
                    flag = 0; // stored raw
                    payload = (byte[])tile.Clone();
                }

                int storedSize = payload.Length + 1; // +1 for the flag byte
                byte[] header = Encoding.ASCII.GetBytes($"{xPix},{yPix},LZF,{storedSize}\n");

                var block = new byte[header.Length + storedSize];
                Buffer.BlockCopy(header, 0, block, 0, header.Length);
                block[header.Length] = flag;
                Buffer.BlockCopy(payload, 0, block, header.Length + 1, payload.Length);
                bodies.Add(block);
            }
        }
        byte[] headerBytes = BuildHead(bodies.Count);

        int total = headerBytes.Length + bodies.Sum(b => b.Length);
        var result = new byte[total];
        int pos = 0;
        Buffer.BlockCopy(headerBytes, 0, result, pos, headerBytes.Length);
        pos += headerBytes.Length;
        foreach (var body in bodies)
        {
            Buffer.BlockCopy(body, 0, result, pos, body.Length);
            pos += body.Length;
        }
        return result;
    }

    private static byte[] BuildHead(int bodiesCount)
    {
        var head = new StringBuilder();
        head.Append("VERSION 2\n");
        head.Append($"TILEWIDTH {TileW}\n");
        head.Append($"TILEHEIGHT {TileH}\n");
        head.Append($"PIXELSIZE {KritaPixelSize}\n");
        head.Append($"DATA {bodiesCount}\n");
        byte[] headerBytes = Encoding.ASCII.GetBytes(head.ToString());
        return headerBytes;
    }
}