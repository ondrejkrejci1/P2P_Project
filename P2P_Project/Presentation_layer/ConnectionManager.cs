using P2P_Project.Application_layer;
using System.IO;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;

namespace P2P_Project.Presentation_layer
{
    public class ConnectionManager
    {
        private TcpClient _client;
        private Thread _clientThread;
        private bool _isRunning;

        private StreamReader _reader;
        private StreamWriter _writer;

        private StackPanel _errorPanel;

        private CommandParser _commandParser;
        private CommandExecutor _commandExecutor;

        public ConnectionManager(TcpClient client, StackPanel errorPanel)
        {
            _commandParser = new CommandParser();
            _commandExecutor = new CommandExecutor();
            _client = client;
            _errorPanel = errorPanel;
            _reader = new StreamReader(_client.GetStream());
            _writer = new StreamWriter(_client.GetStream()) { AutoFlush = true };
            _isRunning = true;
        }


        public void Run()
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
        }

        private void Do()
        {
            try
            {
                string clientInput = _reader.ReadLine();

                string[] parsedCommand = _commandParser.Parse(clientInput);

                _commandExecutor.ExecuteCommand(_client, parsedCommand);

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
            _client.Close();
        }

        private void ErrorLog(string erronName, string errorMessage)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                TextBlock errorText = new TextBlock
                {
                    Text = $"{erronName} error (Client Listener): {errorMessage}",
                    Foreground = System.Windows.Media.Brushes.Red,
                    Margin = new Thickness(10,5,0,0)

                };
                _errorPanel.Children.Add(errorText);
            });
        }


    }
}
