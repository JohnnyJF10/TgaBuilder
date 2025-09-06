namespace TgaBuilderLib.Utils
{
    public class Logger : ILogger
    {
        public void LogError(Exception ex)
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TgaBuilder.errorLog.txt");
            string logMessage = $"{DateTime.Now}: {ex}\n";

            File.AppendAllText(logFilePath, logMessage);
        }
    }
}
