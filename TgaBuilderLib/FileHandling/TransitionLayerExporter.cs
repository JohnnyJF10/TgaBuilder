using System.Collections.Generic;
using System.IO;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Krita;
using TgaBuilderLib.Psd;
using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.FileHandling
{
    /// <summary>
    /// Writes transition layers to a layered PSD or KRA file, reusing the existing
    /// <see cref="IPsdFileService"/> / <see cref="IKraFileService"/> save paths. Mirrors the
    /// single-layer pattern in BitmapBytesIO.ToPsd / BitmapBytesIO.ToKrita.
    /// </summary>
    public class TransitionLayerExporter : ITransitionLayerExporter
    {
        private readonly IPsdFileService _psdFileService;
        private readonly IKraFileService _kritaFileService;
        private readonly IMediaFactory _mediaFactory;

        public TransitionLayerExporter(
            IPsdFileService psdFileService,
            IKraFileService kritaFileService,
            IMediaFactory mediaFactory)
        {
            _psdFileService = psdFileService;
            _kritaFileService = kritaFileService;
            _mediaFactory = mediaFactory;
        }

        public void Export(string filePath, IReadOnlyList<TransitionExportLayer> layers)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File name cannot be null or empty.", nameof(filePath));

            if (layers is null || layers.Count == 0)
                throw new ArgumentException("No layers to export.", nameof(layers));

            string extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();

            switch (extension)
            {
                case "psd":
                    ExportPsd(filePath, layers);
                    break;

                case "kra":
                    ExportKrita(filePath, layers);
                    break;

                default:
                    throw new NotSupportedException($"Unsupported export format: {extension}");
            }
        }

        private void ExportPsd(string filePath, IReadOnlyList<TransitionExportLayer> layers)
        {
            _psdFileService.OutputPath = filePath;

            foreach (var layer in layers)
            {
                var bitmap = _mediaFactory.CreateBitmapFromRaw(
                    layer.Width, layer.Height, hasAlpha: true, layer.Bgra, layer.Width * 4);

                _psdFileService.LayerInfos.Add(new PsdLayerInfo(
                    bitmap: bitmap,
                    rect: new PixelRect(0, 0, layer.Width, layer.Height),
                    name: layer.Name,
                    visible: layer.Visible));
            }

            try
            {
                _psdFileService.WriteFile();
            }
            finally
            {
                _psdFileService.CleanUp();
            }
        }

        private void ExportKrita(string filePath, IReadOnlyList<TransitionExportLayer> layers)
        {
            _kritaFileService.OutputPath = filePath;
            _kritaFileService.KraMainDoc.ImageName = Path.GetFileName(filePath);

            foreach (var layer in layers)
            {
                _kritaFileService.LayerSources.Add(new LayerSource(
                    bgra: layer.Bgra,
                    width: layer.Width,
                    height: layer.Height,
                    hasAlpha: true,
                    name: layer.Name,
                    visible: layer.Visible));
            }

            try
            {
                _kritaFileService.WriteFile();
            }
            finally
            {
                _kritaFileService.CleanUp();
            }
        }
    }
}
