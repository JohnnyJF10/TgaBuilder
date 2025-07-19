
namespace TgaBuilderLib.Utils
{
    public interface IUsageData
    {
        int UndoRedoMemoryBytes { get; set; }
        List<string> RecentInputFiles { get; set; }
        List<string> RecentOutputFiles { get; set; }
        List<string> RecentBatchLoaderFolders { get; set; }
        bool WasLoadingUnsuccessful { get; }

        void AddRecentInputFile(string path);
        void AddRecentOutputFile(string path);
        void AddRecentBatchLoaderFolder(string path);
        void Save();
    }
}