using System.Text;

namespace TgaBuilderLib.Krita;

internal class LayerData
{
    private const int TileW = 64;
    private const int TileH = 64;
    private const int PixelSize = 4; // RGBA8 -> 4 bytes, stored as B,G,R,A (Krita's native order)

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

        const int tileBytes = TileW * TileH * PixelSize; // Assumes PixelSize is 4 for BGRA
        var compressBuf = new byte[tileBytes + tileBytes / 16 + 64];
        var tile = new byte[tileBytes];

        var bodies = new List<byte[]>();
        int channelSize = TileW * TileH; // Size of a single channel plane within the tile

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
                    int srcBase = (srcY * Layer.Width + tx * TileW) * PixelSize;

                    for (int col = 0; col < maxCol; col++)
                    {
                        int srcIdx = srcBase + col * PixelSize;
                        int pixelIndex = row * TileW + col;

                        // Read interleaved BGRA from source
                        byte b = Layer.Bgra[srcIdx + 2];
                        byte g = Layer.Bgra[srcIdx + 1];
                        byte r = Layer.Bgra[srcIdx + 0];
                        byte a = Layer.Bgra[srcIdx + 3];

                        // Write into planar RRRR...GGGG...BBBB...AAAA... destination
                        tile[pixelIndex] = r;
                        tile[channelSize + pixelIndex] = g;
                        tile[2 * channelSize + pixelIndex] = b;
                        tile[3 * channelSize + pixelIndex] = a;

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
        foreach (var b in bodies)
        {
            Buffer.BlockCopy(b, 0, result, pos, b.Length);
            pos += b.Length;
        }
        return result;
    }

    private static byte[] BuildHead(int bodiesCount)
    {
        var head = new StringBuilder();
        head.Append("VERSION 2\n");
        head.Append($"TILEWIDTH {TileW}\n");
        head.Append($"TILEHEIGHT {TileH}\n");
        head.Append($"PIXELSIZE {PixelSize}\n");
        head.Append($"DATA {bodiesCount}\n");
        byte[] headerBytes = Encoding.ASCII.GetBytes(head.ToString());
        return headerBytes;
    }
}