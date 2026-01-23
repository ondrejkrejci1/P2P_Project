using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2P_Project.Data_access_layer.Logging
{
    public interface ILogger
    {
        void Log(string message);
    }
}
