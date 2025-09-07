using TgaBuilderLib.Enums;

namespace TgaBuilderLib.Abstraction
{
    public interface IFileService
    {
        string SelectedPath { get; set; }

        public bool OpenFileDialog(
            FileTypes types,
            string? initDir = null,
            string? title = null);

        public bool OpenFileDialog(
            List<FileTypes> typesList,
            string? initDir = null,
            string? title = null);

        public bool SaveFileDialog(
            FileTypes types,
            string? initDir = null,
            string? title = null);

        bool SelectFolderDialog(
            string? initDir = null, 
            string? title = null);
    }
}
