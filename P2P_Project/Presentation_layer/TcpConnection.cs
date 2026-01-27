using P2P_Project.Application_layer;
using Serilog;
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
            Log.Debug("Connection thread started for client.");
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
                Log.Error(ex, "Critical failure in TcpConnection.Run loop.");
            }
            finally
            {
                Log.Debug("Closing client connection and stopping thread.");
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
                    Log.Debug("Client closed the stream (received null).");
                    _isRunning = false;
                    return;
                }

                Log.Debug("Received raw input: {Input}", clientInput);
                string[] parsedCommand = _commandParser.Parse(clientInput);
                _commandExecutor.ExecuteCommand(Client, parsedCommand);
            }
            catch (IOException ex)
            {
                Log.Warning("Network connection reset by peer.");
                _isRunning = false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error during command processing.");
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;

            try
            {
                _reader?.Close();
                _writer?.Close();
                Client?.Close();
            }
            catch {}

            if (_clientThread != null && _clientThread.IsAlive)
                _clientThread.Interrupt();
        }
    }
}
