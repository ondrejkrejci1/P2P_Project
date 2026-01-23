using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2P_Project.Data_access_layer.Logging
{
    public class UILogger : ILogger
    {
        private readonly Action<string> _updateUiAction;

        public UILogger(Action<string> updateUiAction) => _updateUiAction = updateUiAction;

        public void Log(string message)
        {
            _updateUiAction.Invoke($"STATUS: {message}");
        }
    }
}
