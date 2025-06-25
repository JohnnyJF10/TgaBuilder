namespace THelperLib.Abstraction
{
    public interface IFileService
    {
        string SelectedPath { get; set; }

        public bool OpenFileDialog(
            List<FileTypes> typesList,
            string? InitDir = null,
            string? Title = null,
            bool UseConvergedFilters = false);

        public bool SaveFileDialog(
            List<FileTypes> typesList,
            string? InitDir = null,
            string? Title = null);

        bool SelectFolderDialog(
            string? InitDir = null, 
            string? Title = null);
    }
}
