using Microsoft.Win32;

using TrLynxLib.Abstraction;
using TrLynxLib.Enums;
using TrLynxLib.FileHandling;


namespace TrLynxWpfUi.Services
{
    public class FileService : IFileService
    {
        private const string DEFAULT_OPEN_FILE_TITLE = "Select an Image File";
        private const string DEFAULT_SAVE_FILE_TITLE = "Save File";
        private const string DEFAULT_OPEN_FOLDER_TITLE = "Select a Folder";

        private const string ALL_FILES_FILTER = "All Files (*.*)|*.*";

        public string SelectedPath { get; set; } = "";

        public Task<bool> OpenFileDialog(
            FileTypes types,
            string? initDir = null,
            string? title = null)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = ConvergedFilter(types) + "|" + ALL_FILES_FILTER,
                DefaultExt = FirstExtension(types),
                Title = title ?? DEFAULT_OPEN_FILE_TITLE,
            };

            if (initDir != null) openFileDialog.InitialDirectory = initDir;

            var result = openFileDialog.ShowDialog() == true;

            if (result)
                SelectedPath = openFileDialog.FileName;

            return Task.FromResult(result);
        }

        public Task<bool> OpenFileDialog(
            List<FileTypes> typesList,
            string? initDir = null,
            string? title = null)
        {
            var filterParts = typesList.Select(ConvergedFilter).ToList();
            filterParts.Add(ALL_FILES_FILTER);

            var openFileDialog = new OpenFileDialog
            {
                Filter = string.Join("|", filterParts),
                DefaultExt = FirstExtension(typesList.FirstOrDefault()),
                Title = title ?? DEFAULT_OPEN_FILE_TITLE,
            };

            if (initDir != null) openFileDialog.InitialDirectory = initDir;

            var result = openFileDialog.ShowDialog() == true;

            if (result)
                SelectedPath = openFileDialog.FileName;

            return Task.FromResult(result);
        }

        public Task<bool> SaveFileDialog(
            FileTypes types,
            string? initDir = null,
            string? title = null,
            FileTypes? defaultType = null)
        {
            // One filter entry per format; the preferred format leads so it is preselected.
            var ordered = OrderWithDefaultFirst(types, defaultType);

            var saveFileDialog = new SaveFileDialog
            {
                Filter = string.Join("|", ordered.Select(SeparateFilterEntry)),
                FilterIndex = 1,
                Title = title ?? DEFAULT_SAVE_FILE_TITLE,
            };

            if (ordered.Count > 0)
                saveFileDialog.DefaultExt = FileTypeRegistry.GetExtension(ordered[0]);

            if (initDir != null) saveFileDialog.InitialDirectory = initDir;

            var result = saveFileDialog.ShowDialog() == true;

            if (result)
                SelectedPath = saveFileDialog.FileName;

            return Task.FromResult(result);
        }

        public Task<bool> SelectFolderDialog(string? initDir = null, string? title = null)
        {
            string? folder = FolderPicker.ShowDialog(title ?? DEFAULT_OPEN_FOLDER_TITLE, null);

            var result = !string.IsNullOrWhiteSpace(folder);

            if (result)
                SelectedPath = folder!;

            return Task.FromResult(result);
        }

        // --- filter building (registry-driven, no per-format code) ---

        // "Image Files (*.tga;*.png;...)|*.tga;*.png;..."
        private static string ConvergedFilter(FileTypes types)
        {
            var patterns = FileTypeRegistry.Enumerate(types)
                .Select(t => $"*.{FileTypeRegistry.GetExtension(t)}")
                .ToList();

            string joined = string.Join(";", patterns);
            return $"{FileTypeRegistry.GetCategoryLabel(types)} ({joined})|{joined}";
        }

        // "Photoshop Files (*.psd)|*.psd"
        private static string SeparateFilterEntry(FileTypes type)
        {
            var info = FileTypeRegistry.Get(type)!;
            return $"{info.DisplayName} (*.{info.Extension})|*.{info.Extension}";
        }

        private static List<FileTypes> OrderWithDefaultFirst(FileTypes types, FileTypes? defaultType)
        {
            var ordered = FileTypeRegistry.Enumerate(types).ToList();

            if (defaultType is FileTypes preferred && ordered.Remove(preferred))
                ordered.Insert(0, preferred);

            return ordered;
        }

        private static string FirstExtension(FileTypes types)
        {
            var first = FileTypeRegistry.Enumerate(types).FirstOrDefault();
            return first == FileTypes.None ? string.Empty : FileTypeRegistry.GetExtension(first);
        }
    }
}
