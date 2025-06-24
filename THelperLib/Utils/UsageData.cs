using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace THelperLib.Utils
{
    public class UsageData : IUsageData
    {
        private const string USAGE_DATA_FILENAME = "THelper.usagedata.json";


        public List<string> RecentInputFiles { get; set; } = new();
        public List<string> RecentOutputFiles { get; set; } = new();
        public List<string> RecentBatchLoaderFolders { get; set; } = new();

        [JsonIgnore]
        public static string FilePath
            => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, USAGE_DATA_FILENAME);

        [JsonIgnore]
        public bool WasLoadingUnsuccessful { get; private set; } = false;

        public static UsageData Load(ILogger? logger = null)
        {
            var result = new UsageData();
            if (File.Exists(FilePath))
            {
                try
                {
                    var json = File.ReadAllText(FilePath);
                    result = JsonSerializer.Deserialize<UsageData>(json) ?? new UsageData();
                    return result;
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex);
                    result.WasLoadingUnsuccessful = true;
                }
            }
            return result;
        }


        public void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(FilePath, json);
        }


        public void AddRecentInputFile(string path)
        {
            RecentInputFiles.Remove(path);
            RecentInputFiles.Insert(0, path);
            if (RecentInputFiles.Count > 10)
                RecentInputFiles.RemoveAt(10);

            Save();
        }


        public void AddRecentOutputFile(string path)
        {
            RecentOutputFiles.Remove(path);
            RecentOutputFiles.Insert(0, path);
            if (RecentOutputFiles.Count > 10)
                RecentOutputFiles.RemoveAt(10);

            Save();
        }

        public void AddRecentBatchLoaderFolder(string path)
        {
            RecentBatchLoaderFolders.Remove(path);
            RecentBatchLoaderFolders.Insert(0, path);
            if (RecentBatchLoaderFolders.Count > 10)
                RecentBatchLoaderFolders.RemoveAt(10);
            Save();
        }
    }
}
