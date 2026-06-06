using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;

namespace TgaBuilderLib.Krita
{
    public partial class KraFileService
    {
        private readonly IMediaFactory _mediaFactory;
        private readonly IBitmapOperations _bitmapOperations;

        public KraFileService(IMediaFactory mediaFactory, IBitmapOperations bitmapOperations)
        {
            _mediaFactory = mediaFactory;
            _bitmapOperations = bitmapOperations;
        }


        public string OutputPath { get; set; } = "output.kra";

        public MainDoc KraMainDoc { get; set; } = new();
        public DocumentInfo KraDocumentInfo { get; set; } = new();

        public List<IWriteableBitmap> LayerBitmaps { get; set; } = new();

        public void CleanUp()
        {
            KraMainDoc = new();
            KraDocumentInfo = new();
            LayerBitmaps.Clear();
        }

        public void WriteFile()
        {
            var dir = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            // Decode every PNG into straight-alpha BGRA via Avalonia.
            var layers = new List<LayerSource>();
            int n = 0;
            foreach (var layerBitmap in LayerBitmaps)
            {
                byte[] bgra = layerBitmap.ToMemoryStream().ToArray();
                n++;
                layers.Add(new LayerSource
                {
                    Name = $"Layer {n}",
                    FileName = $"layer{n}",
                    Bgra = bgra,
                    Width = layerBitmap.PixelWidth,
                    Height = layerBitmap.PixelHeight,
                });
            }

            if (layers.Count == 0)
            {
                throw new InvalidOperationException("No PNG files could be decoded.");
            }

            int canvasW = layers.Max(l => l.Width);
            int canvasH = layers.Max(l => l.Height);

            // Optional preview/merged image. Failure here must not abort the .kra itself.
            byte[]? mergedPng = null;
            byte[]? previewPng = null;

            byte[] merged = Composite(layers, canvasW, canvasH);
            mergedPng = EncodePng(merged, canvasW, canvasH);

            byte[] small = Downscale(merged, canvasW, canvasH, 256, out int pw, out int ph);
            previewPng = EncodePng(small, pw, ph);

            string imageName = SanitizeName(Path.GetFileNameWithoutExtension(OutputPath));

            var kraWriter = new KraWriter();

            kraWriter.Write(outputPath: OutputPath,
                            imageName: imageName,
                            canvasW: canvasW,
                            canvasH: canvasH,
                            layers: layers,
                            iccProfile: null,
                            mergedPng: mergedPng,
                            previewPng: previewPng);

            static string SanitizeName(string raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return "image";
                var chars = raw.Select(c => char.IsLetterOrDigit(c) || c is '_' or '-' ? c : '_').ToArray();
                return new string(chars);
            }
        }
    }
}