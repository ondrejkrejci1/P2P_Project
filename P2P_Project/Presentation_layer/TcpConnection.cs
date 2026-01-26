using P2P_Project.Application_layer;
using System.IO;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;

namespace P2P_Project.Presentation_layer
{
    public class TcpConnection
    {
        public TcpClient Client { private set; get; }
        private Thread _clientThread;
        private bool _isRunning;
        private ClientListener _clientListener;

        private StreamReader _reader;
        private StreamWriter _writer;

        private StackPanel _errorPanel;
        private StackPanel _clientPanel;
        private TextBlock _clientCounter;

        private CommandParser _commandParser;
        private CommandExecutor _commandExecutor;

        public TcpConnection(TcpClient client, ClientListener listener, StackPanel errorPanel, StackPanel clientPanel, TextBlock clientCounter)
        {
            _commandParser = new CommandParser();
            _commandExecutor = new CommandExecutor();
            Client = client;
            _clientListener = listener;
            _reader = new StreamReader(Client.GetStream());
            _writer = new StreamWriter(Client.GetStream()) { AutoFlush = true };
            _isRunning = true;
            _errorPanel = errorPanel;
            _clientPanel = clientPanel;
            _clientCounter = clientCounter;

            _clientThread = new Thread(Run);
        }


        public void Start()
        {
            _clientThread.Start();
        }

        private void Run()
        {
            try
            {
                do
                {
                    Do();
                } 
                while (_isRunning);
            }
            catch (Exception ex)
            {
                ErrorLog("RunLoop", ex.Message);
            }
            finally
            {
                _clientListener.ClientDisconected(this);
                Stop();
            }

        }

        private void Do()
        {
            try
            {
                string clientInput = _reader.ReadLine();

                if (clientInput == null)
                {
                    _isRunning = false;
                    return;
                }

                string[] parsedCommand = _commandParser.Parse(clientInput);

                _commandExecutor.ExecuteCommand(Client, parsedCommand);

            }
            catch (IOException)
            {
                if (!_isRunning)
                {
                    return;
                }

                ErrorLog("Communication", "Connection unexpectedly terminated.");
                _isRunning = false;
            }
            catch (Exception ex)
            {
                ErrorLog("Communication", ex.Message);
            }

        }

        public void Stop()
        {
            // poslani erroru uzivatelum o necekane chybe serveru

            _isRunning = false;
            if (_clientThread != null )
                _clientThread.Join(1000);
            Client.Close();
        }

        private void ErrorLog(string erronName, string errorMessage)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                TextBlock errorText = new TextBlock
                {
                    Text = $"{erronName} error (Client Connection): {errorMessage}",
                    Foreground = System.Windows.Media.Brushes.Red,
                    Margin = new Thickness(10,5,0,0)

                };
                _errorPanel.Children.Add(errorText);
            });
        }


    }
}
