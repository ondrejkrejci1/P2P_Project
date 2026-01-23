using System;
using System.IO;

namespace P2P_Project.Data_access_layer.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _filePath;
        private static readonly object _lock = new object();

        public FileLogger(string filePath) => _filePath = filePath;

        public void Log(string message)
        {
            lock (_lock)
            {
                string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllLines(_filePath, new[] { entry });
            }
        }
    }
}