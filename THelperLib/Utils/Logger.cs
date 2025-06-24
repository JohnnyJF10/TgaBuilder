using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THelperLib.Utils
{
    public class Logger : ILogger
    {
        public void LogError(Exception ex)
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "THelper.errorLog.txt");
            string logMessage = $"{DateTime.Now}: {ex}\n";

            File.AppendAllText(logFilePath, logMessage);
        }
    }
}
