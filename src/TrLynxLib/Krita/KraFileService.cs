using System.Runtime.InteropServices;
using TrLynxLib.Abstraction;
using TrLynxLib.BitmapOperations;
using TrLynxLib.Icc;

namespace TrLynxLib.Krita
{
    public partial class KraFileService : IKraFileService
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

        public List<LayerSource> LayerSources { get; set; } = new();

        public void CleanUp()
        {
            KraMainDoc = new();
            KraDocumentInfo = new();
            LayerSources.Clear();
        }

        public void WriteFile()
        {
            var dir = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            if (LayerSources.Count == 0)
            {
                throw new InvalidOperationException("No PNG files could be decoded.");
            }

            int canvasW = LayerSources.Max(l => l.Width);
            int canvasH = LayerSources.Max(l => l.Height);

            byte[]? mergedPng = null;
            byte[]? previewPng = null;

            var sRgbBuildInIccProfile = new IccProfile();

            sRgbBuildInIccProfile.PrimaryPlatformVal = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? PrimaryPlatform.Microsoft
                : PrimaryPlatform.Apple;

            byte[] merged = Composite(LayerSources, canvasW, canvasH);
            mergedPng = EncodePng(merged, canvasW, canvasH);

            byte[] small = Downscale(merged, canvasW, canvasH, 256, out int pw, out int ph);
            previewPng = EncodePng(small, pw, ph);

            string imageName = SanitizeName(Path.GetFileNameWithoutExtension(OutputPath));

            var kraWriter = new KraWriter();

            kraWriter.Write(outputPath: OutputPath,
                            imageName: imageName,
                            canvasW: canvasW,
                            canvasH: canvasH,
                            layers: LayerSources,
                            iccProfile: sRgbBuildInIccProfile.Build(),
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