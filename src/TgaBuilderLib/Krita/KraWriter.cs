using System.IO.Compression;
using System.Text;

namespace TgaBuilderLib.Krita;

internal class KraWriter
{
    private const int PixelSize = 4; // RGBA8 -> 4 bytes, stored as B,G,R,A (Krita's native order)

    /// <summary>
    /// Writes a Krita .kra document combining <paramref name="layers"/> (index 0 = bottom layer)
    /// onto a canvas of <paramref name="canvasW"/> x <paramref name="canvasH"/> pixels. Smaller
    /// layers sit in the top-left corner (origin 0,0).
    /// </summary>
    public void Write(
        string outputPath,
        string imageName,
        int canvasW,
        int canvasH,
        IReadOnlyList<LayerSource> layers,
        byte[] iccProfile,
        byte[]? mergedPng = null,
        byte[]? previewPng = null)
    {
        var mainDoc = new MainDoc
        {
            ImageName = imageName,
            Width = canvasW,
            Height = canvasH,
            Layers = layers
        };

        var documentInfo = new DocumentInfo
        {
            ImageName = imageName,
            InitialCreator = "TgaBuilder"
        };

        using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);

        // 1. mimetype MUST be the first entry and stored uncompressed.
        WriteStored(zip, "mimetype", Encoding.ASCII.GetBytes("application/x-krita"));

        // 2. maindoc.xml + documentinfo.xml
        WriteDeflate(zip, "maindoc.xml", Encoding.UTF8.GetBytes(mainDoc.Build()));
        WriteDeflate(zip, "documentinfo.xml", Encoding.UTF8.GetBytes(documentInfo.Build()));

        // 3. per-layer pixel data + default pixel
        foreach (var layer in layers)
        {
            var layerData = new LayerData(layer);
            byte[] tiles = layerData.Build();
            WriteDeflate(zip, $"{imageName}/layers/{layer.Name}", tiles);
            // transparent default pixel (4 zero bytes -> order irrelevant)
            WriteDeflate(zip, $"{imageName}/layers/{layer.Name}.defaultpixel", new byte[PixelSize]);
            // icc per layer
            WriteDeflate(zip, $"{imageName}/layers/{layer.Name}.icc", iccProfile);
        }

        // 4. image colour profile annotation (Krita's built-in sRGB)
        WriteDeflate(zip, $"{imageName}/annotations/icc", iccProfile);

        // 5. optional previews
        if (mergedPng != null) WriteDeflate(zip, "mergedimage.png", mergedPng);
        if (previewPng != null) WriteDeflate(zip, "preview.png", previewPng);
    }

    private static void WriteStored(ZipArchive zip, string name, byte[] data)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.NoCompression);
        using var s = entry.Open();
        s.Write(data, 0, data.Length);
    }

    private static void WriteDeflate(ZipArchive zip, string name, byte[] data)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var s = entry.Open();
        s.Write(data, 0, data.Length);
    }
}