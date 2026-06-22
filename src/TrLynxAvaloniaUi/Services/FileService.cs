using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TrLynxLib.Abstraction;
using TrLynxLib.Enums;
using TrLynxLib.FileHandling;

namespace TrLynxAvaloniaUi.Services
{
    internal class FileService : IFileService
    {
        private const string DEFAULT_OPEN_FILE_TITLE = "Select an Image File";
        private const string DEFAULT_SAVE_FILE_TITLE = "Save File";
        private const string DEFAULT_OPEN_FOLDER_TITLE = "Select a Folder";

        public string SelectedPath { get; set; } = string.Empty;

        public async Task<bool> OpenFileDialog(FileTypes types, string? initDir = null, string? title = null)
        {
            if (GetTopLevel() is not TopLevel topLevel)
                return false;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title ?? DEFAULT_OPEN_FILE_TITLE,
                FileTypeFilter = new List<FilePickerFileType> { CategoryFilter(types) },
                AllowMultiple = false,
                SuggestedStartLocation = await GetStartLocation(topLevel, initDir),
            });

            return SetSelectedFromFirst(files);
        }

        public async Task<bool> OpenFileDialog(List<FileTypes> typesList, string? initDir = null, string? title = null)
        {
            if (GetTopLevel() is not TopLevel topLevel)
                return false;

            var filters = typesList.Select(CategoryFilter).ToList();
            filters.Add(new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } });

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title ?? DEFAULT_OPEN_FILE_TITLE,
                FileTypeFilter = filters,
                AllowMultiple = false,
                SuggestedStartLocation = await GetStartLocation(topLevel, initDir),
            });

            return SetSelectedFromFirst(files);
        }

        public async Task<bool> SaveFileDialog(
            FileTypes types,
            string? initDir = null,
            string? title = null,
            FileTypes? defaultType = null)
        {
            if (GetTopLevel() is not TopLevel topLevel)
                return false;

            // Only the requested formats are offered; the preferred one leads so it is preselected.
            var ordered = OrderWithDefaultFirst(types, defaultType);

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title ?? DEFAULT_SAVE_FILE_TITLE,
                FileTypeChoices = ordered.Select(SingleFileType).ToList(),
                DefaultExtension = ordered.Count > 0 ? FileTypeRegistry.GetExtension(ordered[0]) : null,
                SuggestedStartLocation = await GetStartLocation(topLevel, initDir),
            });

            if (file == null)
                return false;

            SelectedPath = file.Path.LocalPath;
            return true;
        }

        public async Task<bool> SelectFolderDialog(string? initDir = null, string? title = null)
        {
            if (GetTopLevel() is not TopLevel topLevel)
                return false;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = title ?? DEFAULT_OPEN_FOLDER_TITLE,
                SuggestedStartLocation = await GetStartLocation(topLevel, initDir),
            });

            return SetSelectedFromFirst(folders);
        }

        // --- helpers (registry-driven, no per-format code) ---

        private static TopLevel? GetTopLevel()
        {
            if (Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return null;

            return TopLevel.GetTopLevel(desktop.MainWindow);
        }

        private static async Task<IStorageFolder?> GetStartLocation(TopLevel topLevel, string? initDir)
            => initDir != null && Directory.Exists(initDir)
                ? await topLevel.StorageProvider.TryGetFolderFromPathAsync(initDir)
                : null;

        private bool SetSelectedFromFirst(IReadOnlyList<IStorageItem> items)
        {
            if (items.Count < 1)
                return false;

            SelectedPath = items[0].Path.LocalPath;
            return true;
        }

        // One combined "Image Files"/"Level Files" entry covering every set flag.
        private static FilePickerFileType CategoryFilter(FileTypes types)
            => new(FileTypeRegistry.GetCategoryLabel(types))
            {
                Patterns = FileTypeRegistry.Enumerate(types).SelectMany(PatternsOf).ToList()
            };

        // "Photoshop Files (*.psd)"
        private static FilePickerFileType SingleFileType(FileTypes type)
        {
            var info = FileTypeRegistry.Get(type)!;
            return new($"{info.DisplayName} (*.{info.Extension})")
            {
                Patterns = PatternsOf(type).ToList()
            };
        }

        private static IEnumerable<string> PatternsOf(FileTypes type)
        {
            string ext = FileTypeRegistry.GetExtension(type);
            yield return $"*.{ext}";
            yield return $"*.{ext.ToUpperInvariant()}";
        }

        private static List<FileTypes> OrderWithDefaultFirst(FileTypes types, FileTypes? defaultType)
        {
            var ordered = FileTypeRegistry.Enumerate(types).ToList();

            if (defaultType is FileTypes preferred && ordered.Remove(preferred))
                ordered.Insert(0, preferred);

            return ordered;
        }
    }
}
