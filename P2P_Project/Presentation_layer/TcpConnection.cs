using P2P_Project.Application_layer;
using P2P_Project.Data_access_layer;
using Serilog;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
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

        private StackPanel _clientPanel;
        private TextBlock _clientCounter;
        private TextBlock _numberOfClients;
        private TextBlock _bankAmount;

        private CommandParser _commandParser;
        private CommandExecutor _commandExecutor;

        public TcpConnection(TcpClient client, ClientListener listener, StackPanel clientPanel, TextBlock clientCounter, TextBlock numberOfClients, TextBlock bankAmount)
        {
            _commandParser = new CommandParser();
            _commandExecutor = new CommandExecutor();
            Client = client;
            _clientListener = listener;
            _reader = new StreamReader(Client.GetStream());
            _writer = new StreamWriter(Client.GetStream()) { AutoFlush = true };
            _isRunning = true;
            _clientPanel = clientPanel;
            _clientCounter = clientCounter;
            _numberOfClients = numberOfClients;
            _bankAmount = bankAmount;

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
                Log.Error($"RunLoop - {ex.Message}");
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

                if (parsedCommand[0] == "AC" || parsedCommand[0] == "AR")
                {
                    LoadNumberOfClients();
                }
                else if(parsedCommand[0] == "AD" || parsedCommand[0] == "AW")
                {
                    LoadBankAmount();
                }
            }
            catch (IOException ex)
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
                Log.Error($"Communication {ex.Message}");
            }
        }

        public void Stop()
        {

            _isRunning = false;

        private void LoadNumberOfClients()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (!File.Exists("accounts.json")) _numberOfClients.Text = "0";

                    string jsonString = File.ReadAllText("accounts.json");

                    if (string.IsNullOrWhiteSpace(jsonString)) _numberOfClients.Text = "0";

                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            _numberOfClients.Text = doc.RootElement.GetArrayLength().ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error loading number of clients: {ex.Message}");
                }
            });
        }

        private void LoadBankAmount()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                decimal totalSum = 0;

                try
                {
                    if (!File.Exists("accounts.json")) return;

                    string jsonString = File.ReadAllText("accounts.json");

                    if (string.IsNullOrWhiteSpace(jsonString)) return;

                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement element in doc.RootElement.EnumerateArray())
                            {
                                if (element.TryGetProperty("Balance", out JsonElement balanceElement))
                                {
                                    totalSum += balanceElement.GetDecimal();
                                }
                            }
                        }
                    }

                    _bankAmount.Text = totalSum.ToString();
                }
                catch (Exception ex)
                {
                    Log.Error($"Error loading bank amount: {ex.Message}");
                }

                FormatBankAmount();
            });
        }

        private void FormatBankAmount()
        {
            int pocetCifer = _bankAmount.Text.Length;

            if (pocetCifer <= 3) _bankAmount.FontSize = 50;
            else if (pocetCifer == 4) _bankAmount.FontSize = 38;
            else if (pocetCifer == 5) _bankAmount.FontSize = 30;
            else if (pocetCifer == 6) _bankAmount.FontSize = 24;
            else if (pocetCifer == 7) _bankAmount.FontSize = 22;
            else if (pocetCifer == 8) _bankAmount.FontSize = 18;
            else if (pocetCifer == 9) _bankAmount.FontSize = 16;
            else if (pocetCifer == 10) _bankAmount.FontSize = 14;
            else
            {
                _bankAmount.FontSize = 14;
                _bankAmount.TextWrapping = TextWrapping.NoWrap;
            }
        }

    }
}
