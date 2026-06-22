using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.FileHandling
{
    /// <summary>
    /// Writes a set of transition layers to a multi-layer image file. The format is
    /// chosen from the file extension (.psd or .kra).
    /// </summary>
    public interface ITransitionLayerExporter
    {
        /// <param name="filePath">Destination path; its extension (.psd / .kra) selects the format.</param>
        /// <param name="layers">Layers ordered bottom-to-top.</param>
        void Export(string filePath, IReadOnlyList<TransitionExportLayer> layers);
    }
}
