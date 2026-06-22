namespace TrLynxLib.Psd;

/// <summary>
/// Accumulates layers and writes them to a PSD file. Mirrors
/// <see cref="Krita.IKraFileService"/> so the PSD and Krita save paths share the
/// same shape: add one or more layers, then <see cref="WriteFile"/>.
/// </summary>
public interface IPsdFileService
{
    string OutputPath { get; set; }

    /// <summary>Layers to write, ordered from bottom to top.</summary>
    List<PsdLayerInfo> LayerInfos { get; }

    void WriteFile();

    void CleanUp();
}
