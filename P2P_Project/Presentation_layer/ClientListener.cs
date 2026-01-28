using P2P_Project.Data_access_layer;
using Serilog;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;

namespace P2P_Project.Presentation_layer
{
    /// <summary>
    /// Handles listening for incoming TCP client connections and managing their lifecycle on the server.
    /// Manages the acceptance thread, tracks active connections, and updates the server UI with client status.
    /// </summary>
    public class ClientListener
    {
        private TcpListener _listener;
        private bool _isRunning;
        private Thread _clientAcceptor;
        private List<TcpConnection> _clients;

        private string _ipAddress;
        private int _port;
        private int _timeoutTime;

        private int _clientCounter = 0;

        private StackPanel _clientPanel;
        private TextBlock _clientCount;
        private TextBlock _numberOfClients;
        private TextBlock _bankAmount;

        /// <summary>
        /// Initializes a new instance of the ClientListener class.
        /// Sets up the TCP listener on the specified IP and port and prepares UI references for real-time updates.
        /// </summary>
        /// <param name="ipAddress">The IP address to bind the listener to.</param>
        /// <param name="port">The port number to listen on.</param>
        /// <param name="_timeoutTime">The keep-alive timeout setting for client connections.</param>
        /// <param name="clientPanel">The UI panel where connected client details will be listed.</param>
        /// <param name="clientCounter">The UI text block displaying the current count of active connections.</param>
        /// <param name="numberOfClients">The UI text block displaying total registered clients (from DB/File).</param>
        /// <param name="bankAmount">The UI text block displaying the total bank funds.</param>
        public ClientListener(string ipAddress, int port, int _timeoutTime, StackPanel clientPanel, TextBlock clientCounter, TextBlock numberOfClients, TextBlock bankAmount)
        {
            _listener = new TcpListener(IPAddress.Parse(ipAddress), port);
            _clients = new List<TcpConnection>();
            _ipAddress = ipAddress;
            _port = port;
            this._timeoutTime = _timeoutTime;

            _clientPanel = clientPanel;
            _clientCount = clientCounter;
            _numberOfClients = numberOfClients;
            _bankAmount = bankAmount;
            _clientAcceptor = new Thread(AcceptClient);
        }

        /// <summary>
        /// Starts the TCP listener and the background thread responsible for accepting incoming client connections.
        /// Logs the server start status.
        /// </summary>
        public void Start()
        {
            try
            {
                _listener.Start();
                _isRunning = true;
                _clientAcceptor.Start();
                Log.Information("Server started on {IP}:{Port}", _ipAddress, _port);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to start TcpListener.");
            }
        }


        private readonly object _listLock = new object();

        /// <summary>
        /// The main loop for accepting new clients. Runs on a dedicated background thread.
        /// It waits for a connection, configures keep-alive settings, and creates a new TcpConnection instance.
        /// If the maximum connection count is exceeded, it rejects the client with a message; otherwise, it adds the client to the active list.
        /// </summary>
        private void AcceptClient()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();

                    ConfigureKeepAlive(client.Client);

                    TcpConnection connection = new TcpConnection(client, this, _clientPanel, _clientCount, _numberOfClients, _bankAmount);

                    if (_clients.Count >= ConfigLoader.Instance.MaxConnectionCount)
                    {
                        connection.SendMessageCapacityFull();
                        Log.Debug($"Client ({client.Client.RemoteEndPoint.ToString()}) - attempted to connect, but the connection was rejected because the bank's maximum capacity was exceeded.");
                        Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(5000);
                            }
                            catch (Exception e)
                            {

                            }
                            finally
                            {
                                connection.Stop();
                            }
                        });
                    }
                    else
                    {
                        _clients.Add(connection);
                        connection.Start();
                        DisplayClient(client.Client);
                        Log.Debug($"Client connected - {client.Client.RemoteEndPoint.ToString()}");
                    }

                }
                catch (SocketException socketEx)
                {
                    if (!_isRunning) return;
                    Log.Error(socketEx, "Socket error during client acceptance.");
                    if (socketEx.SocketErrorCode == SocketError.NetworkUnreachable)
                    {
                        Log.Error($"Network connection lost: {socketEx.Message}");
                        Stop();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Configures low-level TCP Keep-Alive options on the client socket to detect dead connections.
        /// </summary>
        /// <param name="socket">The socket to configure.</param>
        private void ConfigureKeepAlive(Socket socket)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            int timeout = ConfigLoader.Instance.TimeoutTime / 1000;

            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, timeout);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, timeout/2);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);
        }

        /// <summary>
        /// Updates the UI to display the newly connected client. 
        /// Adds a text entry to the client panel and increments the displayed connection counter.
        /// </summary>
        /// <param name="socket">The socket of the connected client used for identification.</param>
        private void DisplayClient(Socket socket)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {

                int displyedCount = int.Parse(_clientCount.Text);
                displyedCount++;
                _clientCount.Text = displyedCount.ToString();

                _clientCounter++;

                TextBlock client = new TextBlock
                {
                    Text = $"{_clientCounter} - Client: {socket.RemoteEndPoint.ToString()}",
                    Margin = new Thickness(10, 5, 0, 0),
                    Tag = socket.RemoteEndPoint.ToString()
                };

                _clientPanel.Children.Add(client);
            });
        }

        /// <summary>
        /// Stops the listener, terminates the acceptor thread, and disconnects all currently active clients.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            if (_clientAcceptor.IsAlive)
                _clientAcceptor.Join(1000);
            

            foreach (var client in _clients)
            {
                client.Stop();
            }
        }

        /// <summary>
        /// Handles the logic when a client disconnects. 
        /// Removes the client from the active list and updates the UI (removes the client entry and decrements the counter).
        /// </summary>
        /// <param name="client">The TcpConnection instance that has disconnected.</param>
        public void ClientDisconected(TcpConnection client)
        {
            try
            {
                if (!_clients.Contains(client)) return;

                _clients.Remove(client);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    int displyedCount = int.Parse(_clientCount.Text);
                    displyedCount--;
                    _clientCount.Text = displyedCount.ToString();

                    TextBlock[] clientTextboxes = _clientPanel.Children.OfType<TextBlock>().ToArray();

                    foreach (var item in clientTextboxes)
                    {
                        if (item.Tag.ToString() == client.Client.Client.RemoteEndPoint.ToString())
                        {
                            _clientPanel.Children.Remove(item);
                            break;
                        }
                    }
                });

                Log.Debug($"Client disconected - {client.Client.Client.RemoteEndPoint.ToString()}");
            }
            catch (Exception ex)
            {

            }
        }

    }
}
