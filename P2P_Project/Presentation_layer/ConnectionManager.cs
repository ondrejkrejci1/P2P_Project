using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P2P_Project.Presentation_layer
{
    public class ConnectionManager
    {
        private static readonly ConnectionManager _instance = new ConnectionManager();

        public static ConnectionManager Instance => _instance;

        public List<TcpConnection> Connections { get; set; }

        private ConnectionManager()
        {
            Connections = new List<TcpConnection>();
        }

        public void SendMessage(TcpClient client, string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message + Environment.NewLine);
                client.GetStream().Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to send message({message}) to client: {ex.Message}");    
            }
        }
    }
}
