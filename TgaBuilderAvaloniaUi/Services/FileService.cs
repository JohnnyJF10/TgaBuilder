using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;
using static System.Drawing.PSD.ResolutionInfo;

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
            {
                Debug.WriteLine("The application is not running in desktop mode.");
                return false;
            }

            if (TopLevel.GetTopLevel(desktop.MainWindow) is not TopLevel topLevel)
            {
                Debug.WriteLine("Could not get the top-level window.");
                return false;
            }

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
            {
                Debug.WriteLine("The application is not running in desktop mode.");
                return false;
            }

            if (TopLevel.GetTopLevel(desktop.MainWindow) is not TopLevel topLevel)
            {
                Debug.WriteLine("Could not get the top-level window.");
                return false;
            }

            var fileTypeFilters = new List<FilePickerFileType>();

            foreach (var fileTypes in typesList)
            {
                var patterns = GetPattern(fileTypes);

                string optionName = "Others";

                string displayExt = GetExtensionNames(fileTypes);

                optionName = GetSpecialOptionHeaderStrings(fileTypes);

                var fpft = new FilePickerFileType($"{optionName} ({displayExt})")
                {
                    Patterns = patterns
                };

                fileTypeFilters.Add(fpft);
            }

            fileTypeFilters.Add(new FilePickerFileType("All Files")
            {
                Patterns = new[] { "*.*" }
            });

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
            {
                Debug.WriteLine("The application is not running in desktop mode.");
                return false;
            }

            if (TopLevel.GetTopLevel(desktop.MainWindow) is not TopLevel topLevel)
            {
                Debug.WriteLine("Could not get the top-level window.");
                return false;
            }

            var suggestedStartLocation = initDir != null && Directory.Exists(initDir)
                ? await topLevel.StorageProvider.TryGetFolderFromPathAsync(initDir)
                : null;

            var fileTypeChoices = new List<FilePickerFileType>
                {
                    new("Png Files (*.png)") { Patterns = new[] { "*.png" }},
                    new("Tga Files (*.tga)") { Patterns = new[] { "*.tga" }}
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
            {
                Debug.WriteLine("The application is not running in desktop mode.");
                return false;
            }

            if (TopLevel.GetTopLevel(desktop.MainWindow) is not TopLevel topLevel)
            {
                Debug.WriteLine("Could not get the top-level window.");
                return false;
            }

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

        private void AddPattern(string ext, List<string> patternList)
        {
            patternList.Add($"*.{ext.ToLower()}");
            patternList.Add($"*.{ext.ToUpper()}");
            patternList.Add($"*.{char.ToUpper(ext[0]) + ext.Substring(1).ToLower()}");
        }

        private List<string> GetPattern(FileTypes types)
        {
            var result = new List<string>();

            if (types.HasFlag(FileTypes.TGA)) AddPattern("tga", result);
            if (types.HasFlag(FileTypes.BMP)) AddPattern("bmp", result);
            if (types.HasFlag(FileTypes.PNG)) AddPattern("png", result);
            if (types.HasFlag(FileTypes.JPG)) AddPattern("jpg", result);
            if (types.HasFlag(FileTypes.JPEG)) AddPattern("jpeg", result);
            if (types.HasFlag(FileTypes.PSD)) AddPattern("psd", result);
            if (types.HasFlag(FileTypes.DDS)) AddPattern("dds", result);
            if (types.HasFlag(FileTypes.PHD)) AddPattern("phd", result);
            if (types.HasFlag(FileTypes.TR2)) AddPattern("tr2", result);
            if (types.HasFlag(FileTypes.TR4)) AddPattern("tr4", result);
            if (types.HasFlag(FileTypes.TRC)) AddPattern("trc", result);
            if (types.HasFlag(FileTypes.TEN)) AddPattern("ten", result);

            return result;
        }

        private string GetExtensionNames(FileTypes types)
        {
            var result = string.Empty;

            if (types.HasFlag(FileTypes.TGA)) result += "*.tga, ";
            if (types.HasFlag(FileTypes.BMP)) result += "*.bmp, ";
            if (types.HasFlag(FileTypes.PNG)) result += "*.png, ";
            if (types.HasFlag(FileTypes.JPG)) result += "*.jpg, ";
            if (types.HasFlag(FileTypes.JPEG)) result += "*.jpeg, ";
            if (types.HasFlag(FileTypes.PSD)) result += "*.psd, ";
            if (types.HasFlag(FileTypes.DDS)) result += "*.dds, ";
            if (types.HasFlag(FileTypes.PHD)) result += "*.phd, ";
            if (types.HasFlag(FileTypes.TR2)) result += "*.tr2, ";
            if (types.HasFlag(FileTypes.TR4)) result += "*.tr4, ";
            if (types.HasFlag(FileTypes.TRC)) result += "*.trc, ";
            if (types.HasFlag(FileTypes.TEN)) result += "*.ten, ";
            if (result.EndsWith(", ")) result = result.Substring(0, result.Length - 2);

            return result;
        }

        private string GetSpecialOptionHeaderStrings(FileTypes types)
        {
            if (types.HasFlag(FileTypes.TGA)
                && types.HasFlag(FileTypes.PNG)
                && types.HasFlag(FileTypes.JPG)
                && types.HasFlag(FileTypes.JPEG)
                && types.HasFlag(FileTypes.BMP)
                && types.HasFlag(FileTypes.PSD)
                && types.HasFlag(FileTypes.DDS))
                return "Image Files";
            if (types.HasFlag(FileTypes.PHD)
                && types.HasFlag(FileTypes.TR2)
                && types.HasFlag(FileTypes.TR4)
                && types.HasFlag(FileTypes.TRC)
                && types.HasFlag(FileTypes.TEN))
                return "Level Files";
            return "Others";
        }
    }
}
