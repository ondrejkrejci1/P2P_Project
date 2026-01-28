using P2P_Project.Data_access_layer;
using Serilog;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;

namespace P2P_Project.Presentation_layer
{
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

        private void ConfigureKeepAlive(Socket socket)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            int timeout = ConfigLoader.Instance.TimeoutTime / 1000;

            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, timeout);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, timeout/2);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);
        }

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
