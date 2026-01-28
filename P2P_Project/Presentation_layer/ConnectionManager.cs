using Serilog;
using System.Net.Sockets;
using System.Text;

namespace P2P_Project.Presentation_layer
{
    /// <summary>
    /// A singleton class responsible for managing the collection of active TCP connections
    /// and providing a centralized utility for sending messages to connected clients.
    /// </summary>
    public class ConnectionManager
    {
        private static readonly ConnectionManager _instance = new ConnectionManager();

        /// <summary>
        /// Gets the single, globally accessible instance of the ConnectionManager.
        /// </summary>
        public static ConnectionManager Instance => _instance;

        /// <summary>
        /// Gets or sets the list of currently active TCP connections managed by the application.
        /// </summary>
        public List<TcpConnection> Connections { get; set; }

        /// <summary>
        /// Initializes a new instance of the ConnectionManager class.
        /// This constructor is private to enforce the Singleton pattern and initializes the internal list of connections.
        /// </summary>
        private ConnectionManager()
        {
            Connections = new List<TcpConnection>();
        }

        /// <summary>
        /// Encodes and sends a text message to a specified TCP client.
        /// The message is converted to UTF-8, appended with a newline character, and written to the client's network stream.
        /// Any exceptions occurring during the transmission are caught and logged as errors without crashing the application.
        /// </summary>
        /// <param name="client">The target TCP client to receive the message.</param>
        /// <param name="message">The string content of the message to be sent.</param>
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
