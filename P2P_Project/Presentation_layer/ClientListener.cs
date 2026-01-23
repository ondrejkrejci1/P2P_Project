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
        private List<ConnectionManager> _clients;

        private string _ipAddress;
        private int _port;
        private int _timeoutTime;

        private StackPanel _errorPanel;

        public ClientListener(string ipAddress, int port, int _timeoutTime, StackPanel errorPanel)
        {
            _listener = new TcpListener(IPAddress.Parse(ipAddress), port);
            _clients = new List<ConnectionManager>();
            _ipAddress = ipAddress;
            _port = port;
            this._timeoutTime = _timeoutTime;

            _errorPanel = errorPanel;
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
                    ConnectionManager connectionManager = new ConnectionManager(client,_errorPanel);
                    _clients.Add(connectionManager);
                    connectionManager.Run();
                }
                catch (SocketException socketEx)
                {
                    ErrorLog("Socket", socketEx.Message);
                }
                catch (Exception ex)
                {
                    ErrorLog("General", ex.Message);
                }

            }
        }


        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

            if (ipAddress == null)
                ErrorLog("Unknown IP Address", "Automatic ip setting couldnt find IP address of this device");

            return ipAddress.ToString();
        }

        private void ErrorLog(string erronName, string errorMessage)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                TextBlock errorText = new TextBlock
                {
                    Text = $"{erronName} error (Client Listener): {errorMessage}",
                    Foreground = System.Windows.Media.Brushes.Red
                };
                _errorPanel.Children.Add(errorText);
            });
        }

    }
}
