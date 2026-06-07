using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Krita
{
    public interface IKraFileService
    {
        DocumentInfo KraDocumentInfo { get; set; }
        MainDoc KraMainDoc { get; set; }
        List<LayerSource> LayerSources { get; set; }
        string OutputPath { get; set; }

        void CleanUp();
        void WriteFile();
    }
}