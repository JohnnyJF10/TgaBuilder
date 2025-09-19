using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;

namespace TgaBuilderAvaloniaUi.Services
{
    internal class FileService : IFileService
    {
        private const string DEFAULT_OPEN_FILE_TITLE = "Select an Image File";
        private const string DEFAULT_SAVE_FILE_TITLE = "Save TGA File";
        private const string DEFAULT_OPEN_FOLDER_TITLE = "Select a Folder";

        private string _selectedPath = string.Empty;

        public string SelectedPath
        {
            get => _selectedPath;
            set => _selectedPath = value;
        }

        public async Task<bool> OpenFileDialog(FileTypes types, string? initDir = null, string? title = null)
        {
            if (Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                throw new InvalidOperationException("The application is not running in desktop mode.");

            if (TopLevel.GetTopLevel(desktop.MainWindow) is not TopLevel topLevel)
                throw new InvalidOperationException("Could not get the top-level window.");

                            var suggestedStartLocation = initDir != null && Directory.Exists(initDir)
                ? await topLevel.StorageProvider.TryGetFolderFromPathAsync(initDir)
                : null;

            // Start async operation to open the dialog.
            var filesResult = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title ?? DEFAULT_OPEN_FILE_TITLE,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new ("Supported Files")
                    {
                        Patterns = GetPattern(types)
                    }
                },
                AllowMultiple = false,
                SuggestedStartLocation = suggestedStartLocation
            });

            if (filesResult.Count >= 1)
            {

                SelectedPath = filesResult[0].Path.LocalPath;
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> OpenFileDialog(List<FileTypes> typesList, string? initDir = null, string? title = null)
        {
            if (Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                throw new InvalidOperationException("The application is not running in desktop mode.");

            if (TopLevel.GetTopLevel(desktop.MainWindow) is not TopLevel topLevel)
                throw new InvalidOperationException("Could not get the top-level window.");

            var fileTypeFilters = new List<FilePickerFileType>();

            foreach (var fileTypes in typesList)
            {
                var patterns = GetPattern(fileTypes);

                var fpft = new FilePickerFileType("Test")
                {
                    Patterns = patterns
                };

                fileTypeFilters.Add(fpft);
            }

            var suggestedStartLocation = initDir != null && Directory.Exists(initDir)
                ? await topLevel.StorageProvider.TryGetFolderFromPathAsync(initDir)
                : null;

            var filesResult = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title ?? DEFAULT_OPEN_FILE_TITLE,
                FileTypeFilter = fileTypeFilters,
                AllowMultiple = false,
                SuggestedStartLocation = suggestedStartLocation
            });

            if (filesResult.Count >= 1)
            {
                SelectedPath = filesResult[0].Path.LocalPath;
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> SaveFileDialog(FileTypes types, string? initDir = null, string? title = null)
        {

            if (Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                throw new InvalidOperationException("The application is not running in desktop mode.");

            if (TopLevel.GetTopLevel(desktop.MainWindow) is not TopLevel topLevel)
                throw new InvalidOperationException("Could not get the top-level window.");

            var suggestedStartLocation = initDir != null && Directory.Exists(initDir)
                ? await topLevel.StorageProvider.TryGetFolderFromPathAsync(initDir)
                : null;

            var fileTypeChoices = new List<FilePickerFileType>
                {
                    new ("Supported Files")
                };

            var fileResult = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title ?? DEFAULT_OPEN_FILE_TITLE,
                FileTypeChoices = fileTypeChoices,
                SuggestedStartLocation = suggestedStartLocation
            });


            if (fileResult != null)
            {
                SelectedPath = fileResult.Path.LocalPath;
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> SelectFolderDialog(string? initDir = null, string? title = null)
        {

            if (Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                throw new InvalidOperationException("The application is not running in desktop mode.");

            if (TopLevel.GetTopLevel(desktop.MainWindow) is not TopLevel topLevel)
                throw new InvalidOperationException("Could not get the top-level window.");

                var suggestedStartLocation = initDir != null && Directory.Exists(initDir)
                ? await topLevel.StorageProvider.TryGetFolderFromPathAsync(initDir)
                : null;

            var folderResult = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = title ?? DEFAULT_OPEN_FOLDER_TITLE,
                SuggestedStartLocation = suggestedStartLocation
            });

            if (folderResult.Count >= 1)
            {
                SelectedPath = folderResult[0].Path.LocalPath;
                return true;
            }
            else
            {
                return false;
            }
        }

        private List<string> GetPattern(FileTypes types)
        {
            var result = new List<string>();

            if (types.HasFlag(FileTypes.TGA)) result.Add("*.tga");
            if (types.HasFlag(FileTypes.BMP)) result.Add("*.bmp");
            if (types.HasFlag(FileTypes.PNG)) result.Add("*.png");
            if (types.HasFlag(FileTypes.JPG)) result.Add("*.jpg");
            if (types.HasFlag(FileTypes.JPEG)) result.Add("*.jpeg");
            if (types.HasFlag(FileTypes.PSD)) result.Add("*.psd");
            if (types.HasFlag(FileTypes.DDS)) result.Add("*.dds");
            if (types.HasFlag(FileTypes.PHD)) result.Add("*.phd");
            if (types.HasFlag(FileTypes.TR2)) result.Add("*.tr2");
            if (types.HasFlag(FileTypes.TR4)) result.Add("*.tr4");
            if (types.HasFlag(FileTypes.TRC)) result.Add("*.trc");
            if (types.HasFlag(FileTypes.TEN)) result.Add("*.ten");

            return result;
        }
    }
}
