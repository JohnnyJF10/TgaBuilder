namespace TrLynxLib.Psd;

public class PsdFileService : IPsdFileService
{
    public string OutputPath { get; set; } = "output.psd";

    public List<PsdLayerInfo> LayerInfos { get; } = new();

    public void WriteFile()
    {
        if (LayerInfos.Count == 0)
            throw new InvalidOperationException("No layers to write.");

        var dir = Path.GetDirectoryName(OutputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        new PsdFile().Save(OutputPath, LayerInfos);
    }

    public void CleanUp()
    {
        LayerInfos.Clear();
    }
}
