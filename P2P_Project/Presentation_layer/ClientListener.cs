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

        private StackPanel _errorPanel;
        private StackPanel _clientPanel;
        private TextBlock _clientCounter;

        public ClientListener(string ipAddress, int port, int _timeoutTime, StackPanel errorPanel, StackPanel clientPanel, TextBlock clientCounter)
        {
            _listener = new TcpListener(IPAddress.Parse(ipAddress), port);
            _clients = new List<TcpConnection>();
            _ipAddress = ipAddress;
            _port = port;
            this._timeoutTime = _timeoutTime;

            _errorPanel = errorPanel;
            _clientPanel = clientPanel;
            _clientCounter = clientCounter;
            _clientAcceptor = new Thread(AcceptClient);
        }

        public void Start()
        {
            _listener.Start();
            _isRunning = true;
            _clientAcceptor.Start();
        }


        private void AcceptClient()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();

                    ConfigureKeepAlive(client.Client);

                    TcpConnection connection = new TcpConnection(client, this, _errorPanel, _clientPanel, _clientCounter);
                    _clients.Add(connection);
                    connection.Start();

                    DisplayClient(client.Client);

                }
                catch (SocketException socketEx)
                {
                    if (!_isRunning)
                    {
                        return;
                    }

                    ErrorLog("Socket", socketEx.Message);
                }
                catch (Exception ex)
                {
                    ErrorLog("General", ex.Message);
                }

            }
        }

        private void ConfigureKeepAlive(Socket socket)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 20);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 2);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);
        }

        private void ErrorLog(string erronName, string errorMessage)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                TextBlock errorText = new TextBlock
                {
                    Text = $"{erronName} error (Client Listener): {errorMessage}",
                    Foreground = System.Windows.Media.Brushes.Red,
                    Margin = new Thickness(10, 5, 0, 0)
                };
                _errorPanel.Children.Add(errorText);
            });
        }

        private void DisplayClient(Socket socket)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {

                int displyedCount = int.Parse(_clientCounter.Text);
                displyedCount++;
                _clientCounter.Text = displyedCount.ToString();

                TextBlock client = new TextBlock
                {
                    Text = $"{displyedCount} - Client: {socket.RemoteEndPoint.ToString()}",
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
                    int displyedCount = int.Parse(_clientCounter.Text);
                    displyedCount--;
                    _clientCounter.Text = displyedCount.ToString();

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
            }
            catch (Exception ex)
            {

            }
        }

    }
}
