using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2P_Project.Data_access_layer.Logging
{
    public sealed class Logger : ILogger
    {
        private static readonly Logger _instance = new Logger();
        private readonly List<ILogger> _loggers = new();

        private Logger()
        {
            _loggers.Add(new FileLogger("log.txt"));
            
            //pripojit k ui
            //_loggers.Add(new UILogger(msg => App.CurrentUIAction?.Invoke(msg)));
        }

        public static Logger Instance => _instance;

        public void Log(string message)
        {
            foreach (var logger in _loggers)
            {
                logger.Log(message);
            }
        }
    }
}
