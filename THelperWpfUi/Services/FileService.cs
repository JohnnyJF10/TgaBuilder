using Microsoft.Win32;

using THelperLib.Abstraction;


namespace THelperWpfUi.Services
{
    public partial class FileService : IFileService
    {
        private const string DEFAULT_OPEN_FILE_TITLE = "Select an Image File";
        private const string DEFAULT_SAVE_FILE_TITLE = "Save TGA File";
        private const string DEFAULT_OPEN_FOLDER_TITLE = "Select a Folder";

        public string SelectedPath { get; set; } = "";

        public bool OpenFileDialog(
            List<FileTypes> typesList,
            string? InitDir = null,
            string? Title = null,
            bool UseConvergedFilters = false)
        {
            var openFileDialog = new OpenFileDialog();

            var filterParts = new List<string>();
            foreach (var fileTypes in typesList)
            {
                string filter = UseConvergedFilters
                    ? BuildConvergedFilter(fileTypes)
                    : GetFilter(fileTypes);
                filterParts.Add(filter);
            }

            openFileDialog.Filter = string.Join("|", filterParts);
            openFileDialog.Filter += "|All Files (*.*)|*.*"; 
            openFileDialog.DefaultExt = GetDefaultExt(typesList.First()); 
            openFileDialog.Title = Title ?? DEFAULT_OPEN_FILE_TITLE;

            if (InitDir != null) openFileDialog.InitialDirectory = InitDir;

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedPath = openFileDialog.FileName;
                return true;
            }

            return false;
        }

        public bool SaveFileDialog(
            List<FileTypes> typesList,
            string? InitDir = null, 
            string? Title = null)
        {
            var saveFileDialog = new SaveFileDialog();

            var filterParts = new List<string>();
            foreach (var fileTypes in typesList)
            {
                string filter = GetFilter(fileTypes);
                filterParts.Add(filter);
            }

            saveFileDialog.Filter = string.Join("|", filterParts);
            saveFileDialog.DefaultExt = GetDefaultExt(typesList.First());
            saveFileDialog.Title = Title ?? DEFAULT_SAVE_FILE_TITLE;
            if (InitDir != null) saveFileDialog.InitialDirectory = InitDir;
            if (saveFileDialog.ShowDialog() == true)
            {
                SelectedPath = saveFileDialog.FileName;
                return true;
            }

            return false;
        }

        public bool SelectFolderDialog(string? InitDir = null, string? Title = null)
        {
            string? folder = FolderPicker.ShowDialog(Title ?? DEFAULT_OPEN_FOLDER_TITLE, null);
            if (folder != null)
            {
                SelectedPath = folder;
                return true;
            }
            return false;
        }

        private static string GetFilter(FileTypes types)
        {
            var filters = FileTypeLookup
                .Where(kvp => types.HasFlag(kvp.Key))
                .Select(kvp => kvp.Value.Description);

            return string.Join("|", filters);
        }

        private static string GetDefaultExt(FileTypes types)
        {
            var reducedType = ReduceToSingleSelection(types);
            return FileTypeLookup.TryGetValue(reducedType, out var info) ? info.Extension : "";
        }

        private static string GetTitle(FileTypes types)
        {
            var reducedType = ReduceToSingleSelection(types);
            return FileTypeLookup.TryGetValue(reducedType, out var info) ? info.Title : "";
        }
        private static FileTypes ReduceToSingleSelection(FileTypes types)
        {
            if (types == FileTypes.None) return FileTypes.None;

            foreach (FileTypes type in Enum.GetValues(typeof(FileTypes)).Cast<FileTypes>().Reverse())
                if (types.HasFlag(type)) return type;

            return FileTypes.None;
        }

        private string BuildConvergedFilter(FileTypes selectedTypes, string OptionName = "All supported files")
        {
            // Liste für die Commands (*.xyz)
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

            // Beispiel: "*.tga;*.bmp;*.png"
            string allSupportedExtensions = string.Join(";", extensions);

            // Filter-String zusammensetzen
            string filter = $"{OptionName} ({allSupportedExtensions})|{allSupportedExtensions}";

            return filter;
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
