using TrLynxLib.Enums;

namespace TrLynxLib.Abstraction
{
    public interface IFileService
    {
        string SelectedPath { get; set; }

        public Task<bool> OpenFileDialog(
            FileTypes types,
            string? initDir = null,
            string? title = null);

        public Task<bool> OpenFileDialog(
            List<FileTypes> typesList,
            string? initDir = null,
            string? title = null);

        public Task<bool> SaveFileDialog(
            FileTypes types,
            string? initDir = null,
            string? title = null,
            FileTypes? defaultType = null);

        public Task<bool> SelectFolderDialog(
            string? initDir = null,
            string? title = null);
    }
}
