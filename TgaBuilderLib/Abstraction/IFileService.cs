namespace TgaBuilderLib.Abstraction
{
    public interface IFileService
    {
        string SelectedPath { get; set; }

        public bool OpenFileDialog(
            FileTypes types,
            string? InitDir = null,
            string? Title = null);

        public bool OpenFileDialog(
            List<FileTypes> typesList,
            string? InitDir = null,
            string? Title = null);

        public bool SaveFileDialog(
            FileTypes types,
            string? InitDir = null,
            string? Title = null);

        bool SelectFolderDialog(
            string? InitDir = null, 
            string? Title = null);
    }
}
