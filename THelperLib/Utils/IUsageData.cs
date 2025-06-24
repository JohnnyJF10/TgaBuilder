
namespace THelperLib.Utils
{
    public interface IUsageData
    {
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