using TrLynxLib.Enums;

namespace TrLynxLib.FileHandling
{
    /// <summary>
    /// Single source of truth mapping each <see cref="FileTypes"/> flag to its extension and
    /// display name. Both the WPF and Avalonia file services build their dialog filters from
    /// here, so neither needs format-specific code.
    /// </summary>
    public static class FileTypeRegistry
    {
        public static IReadOnlyDictionary<FileTypes, FileTypeInfo> Lookup { get; } =
            new Dictionary<FileTypes, FileTypeInfo>
            {
                [FileTypes.TGA] = new("tga", "Targa Files"),
                [FileTypes.BMP] = new("bmp", "Bitmap Files"),
                [FileTypes.PNG] = new("png", "PNG Files"),
                [FileTypes.JPG] = new("jpg", "JPEG Files"),
                [FileTypes.JPEG] = new("jpeg", "JPEG Files"),
                [FileTypes.PSD] = new("psd", "Photoshop Files"),
                [FileTypes.KRA] = new("kra", "Krita Files"),
                [FileTypes.DDS] = new("dds", "DirectDraw Surface Files"),
                [FileTypes.PHD] = new("phd", "PHD Files"),
                [FileTypes.TR2] = new("tr2", "TR2 Files"),
                [FileTypes.TR4] = new("tr4", "TR4 Files"),
                [FileTypes.TRC] = new("trc", "TR5 Files"),
                [FileTypes.TEN] = new("ten", "TEN Files"),
            };

        // Flag groupings used only for friendly open-dialog category labels.
        private static readonly FileTypes ImageTypes =
            FileTypes.TGA | FileTypes.BMP | FileTypes.PNG | FileTypes.JPG
            | FileTypes.JPEG | FileTypes.PSD | FileTypes.KRA | FileTypes.DDS;

        private static readonly FileTypes LevelTypes =
            FileTypes.PHD | FileTypes.TR2 | FileTypes.TR4 | FileTypes.TRC | FileTypes.TEN;

        /// <summary>Yields each individual flag set in <paramref name="flags"/>, in enum order.</summary>
        public static IEnumerable<FileTypes> Enumerate(FileTypes flags)
        {
            foreach (FileTypes type in Enum.GetValues<FileTypes>())
            {
                if (type == FileTypes.None)
                    continue;

                if (flags.HasFlag(type))
                    yield return type;
            }
        }

        public static FileTypeInfo? Get(FileTypes single)
            => Lookup.TryGetValue(single, out var info) ? info : null;

        /// <summary>Extension (without dot) of a single flag, or empty if unknown.</summary>
        public static string GetExtension(FileTypes single)
            => Lookup.TryGetValue(single, out var info) ? info.Extension : string.Empty;

        /// <summary>A friendly label for a combined filter group ("Image Files", "Level Files", …).</summary>
        public static string GetCategoryLabel(FileTypes types)
        {
            bool hasImage = (types & ImageTypes) != 0;
            bool hasLevel = (types & LevelTypes) != 0;

            if (hasLevel && !hasImage) return "Level Files";
            if (hasImage && !hasLevel) return "Image Files";
            return "Supported Files";
        }
    }
}
