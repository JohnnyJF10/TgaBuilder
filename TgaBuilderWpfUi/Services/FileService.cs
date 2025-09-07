using Microsoft.Win32;

using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;


namespace TgaBuilderWpfUi.Services
{
    public partial class FileService : IFileService
    {
        private record FileTypeInfo
        {
            public string Extension { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
        }

        private const string DEFAULT_OPEN_FILE_TITLE = "Select an Image File";
        private const string DEFAULT_SAVE_FILE_TITLE = "Save TGA File";
        private const string DEFAULT_OPEN_FOLDER_TITLE = "Select a Folder";

        public string SelectedPath { get; set; } = "";

        public bool OpenFileDialog(
            FileTypes types,
            string? initDir = null,
            string? title = null)
        {
            var openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = GetConvergedFilter(types);
            openFileDialog.Filter += "|All Files (*.*)|*.*";
            openFileDialog.DefaultExt = GetDefaultExt(types);
            openFileDialog.Title = title ?? DEFAULT_OPEN_FILE_TITLE;

            if (initDir != null) openFileDialog.InitialDirectory = initDir;

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedPath = openFileDialog.FileName;
                return true;
            }

            return false;
        }

        public bool OpenFileDialog(
            List<FileTypes> typesList,
            string? initDir = null,
            string? title = null)
        {
            var openFileDialog = new OpenFileDialog();

            var filterParts = new List<string>();
            foreach (var fileTypes in typesList)
            {
                string filter = GetConvergedFilter(fileTypes);
                filterParts.Add(filter);
            }

            openFileDialog.Filter = string.Join("|", filterParts);
            openFileDialog.Filter += "|All Files (*.*)|*.*"; 
            openFileDialog.DefaultExt = GetDefaultExt(typesList.First()); 
            openFileDialog.Title = title ?? DEFAULT_OPEN_FILE_TITLE;

            if (initDir != null) openFileDialog.InitialDirectory = initDir;

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedPath = openFileDialog.FileName;
                return true;
            }

            return false;
        }

        public bool SaveFileDialog(
            FileTypes types,
            string? initDir = null, 
            string? title = null)
        {
            var saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = GetSeperatedFilter(types);
            saveFileDialog.Title = title ?? DEFAULT_SAVE_FILE_TITLE;
            if (initDir != null) saveFileDialog.InitialDirectory = initDir;
            if (saveFileDialog.ShowDialog() == true)
            {
                SelectedPath = saveFileDialog.FileName;
                return true;
            }

            return false;
        }

        public bool SelectFolderDialog(string? initDir = null, string? title = null)
        {
            string? folder = FolderPicker.ShowDialog(title ?? DEFAULT_OPEN_FOLDER_TITLE, null);
            if (folder != null)
            {
                SelectedPath = folder;
                return true;
            }
            return false;
        }

        private string GetConvergedFilter(FileTypes selectedTypes, string OptionName = "All supported files")
        {
            List<string> extensions = new List<string>();

            foreach (var kvp in FileTypeLookup)
            {
                if (selectedTypes.HasFlag(kvp.Key))
                {
                    var ext = kvp.Value.Extension;
                    if (!string.IsNullOrWhiteSpace(ext))
                    {
                        if (!ext.StartsWith("*."))
                            ext = "*." + ext.TrimStart('.');
                        extensions.Add(ext);
                    }
                }
            }

            OptionName = CheckForSpecificExpressions(selectedTypes);

            string allSupportedExtensions = string.Join(";", extensions);

            return $"{OptionName} ({allSupportedExtensions})|{allSupportedExtensions}";
        }

        private string GetSeperatedFilter(FileTypes types)
        {
            var filters = new List<string>();

            foreach (FileTypes type in Enum.GetValues(typeof(FileTypes)))
            {
                if (type == FileTypes.None)
                    continue;

                if (types.HasFlag(type))
                {
                    string extension = type.ToString().ToLower(); 
                    string name = type.ToString().ToUpper();      
                    filters.Add($"{name} (*.{extension})|*.{extension}");
                }
            }

            return string.Join("|", filters);
        }

        private string GetDefaultExt(FileTypes types)
        {
            var reducedType = ReduceToSingleSelection(types);
            return FileTypeLookup.TryGetValue(reducedType, out var info) ? info.Extension : "";
        }

        private string GetTitle(FileTypes types)
        {
            var reducedType = ReduceToSingleSelection(types);
            return FileTypeLookup.TryGetValue(reducedType, out var info) ? info.Title : "";
        }
        private FileTypes ReduceToSingleSelection(FileTypes types)
        {
            if (types == FileTypes.None) return FileTypes.None;

            foreach (FileTypes type in Enum.GetValues(typeof(FileTypes)).Cast<FileTypes>().Reverse())
                if (types.HasFlag(type)) return type;

            return FileTypes.None;
        }

        private const FileTypes DEF_FILE_TYPES =
            FileTypes.TGA | FileTypes.BMP | FileTypes.PNG | FileTypes.JPG
            | FileTypes.JPEG | FileTypes.PSD | FileTypes.DDS;

        private const FileTypes TR_FILE_TYPES = FileTypes.PHD | FileTypes.TR2
            | FileTypes.TR4 | FileTypes.TRC | FileTypes.TEN;

        private string CheckForSpecificExpressions(FileTypes selectedTypes)
        => selectedTypes switch
            {
                DEF_FILE_TYPES => "Image Files",
                TR_FILE_TYPES => "TR Level Files",
                _ => "All supported files"
            };
    }
}
