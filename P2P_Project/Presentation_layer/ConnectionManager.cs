using System.IO;
using System.Net.Sockets;
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

        public ConnectionManager(TcpClient client, StackPanel errorPanel)
        {
            _commandParser = new CommandParser();
            _client = client;
            _reader = new StreamReader(_client.GetStream());
            _writer = new StreamWriter(_client.GetStream()) { AutoFlush = true };
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
                TextBlock errorText = new TextBlock
                {
                    Text = $"Connection error: {ex.Message}",
                    Foreground = System.Windows.Media.Brushes.Red,
                    Margin = new System.Windows.Thickness(5)
                };
                _errorPanel.Children.Add(errorText);
            }
        }

        private void Do()
        {
            try
            {
                string clientInput = _reader.ReadLine();

                string[] parsedCommand = _commandParser.Parse(clientInput);

                // odeslani na command executor - parsnuty komand a tcpclient

            }
            catch (Exception ex)
            {
                TextBlock errorText = new TextBlock
                {
                    Text = $"Communication error (Connection Manager): {ex.Message}",
                    Foreground = System.Windows.Media.Brushes.Red,
                    Margin = new System.Windows.Thickness(5)
                };
                _errorPanel.Children.Add(errorText);
            }

        }

        private void Stop()
        {
            _isRunning = false;
            _client.Close();
            _clientThread.Join();
        }

    }
}
