using System;
using System.IO;

namespace Next.Accounts_Server.Controllers
{
    public class DefaultLogger : ILogger
    {

        public string ErrorLogName { get; set; } = "errors.txt";

        public string EventsLogName { get; set; } = "database_events.txt";

        public string UsementLogName { get; set; } = "usement.txt";

        private string _logPath;

        public DefaultLogger()
        {
            _logPath = $"{Environment.CurrentDirectory}\\Logs";
        }

        public void Log(string message)
        {
            var filename = $"{_logPath}\\{EventsLogName}";
            WriteToFile(filename, message);
        }

        public void LogError(string error)
        {
            var filename = $"{_logPath}\\{ErrorLogName}";
            WriteToFile(filename, error);
        }

        public void LogAccountUsement(string message)
        {
            UsementLogName = $"usement-{DateTime.Now:dd-MM-yyyy}.txt";
            var filename = $"{_logPath}\\{UsementLogName}";
            WriteToFile(filename, message);
        }

        public async void WriteToFile(string filename, string write)
        {
            //if (!File.Exists(filename)) File.Create(filename);
            StreamWriter stream = null;
            try
            {
                stream = File.AppendText(filename);
                var text = $"[{DateTime.Now}] {write}";
                await stream.WriteLineAsync(text);
            }
            catch (Exception ex)
            {
                // ignored
            }
            finally
            {
                stream?.Close();
            }
        }
    }
}