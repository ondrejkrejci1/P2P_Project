using P2P_Project.Application_layer;
using P2P_Project.Data_access_layer;
using Serilog;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace P2P_Project.Presentation_layer
{
    /// <summary>
    /// Manages an individual TCP connection with a client.
    /// Handles the communication lifecycle, command parsing/execution, and updates the server UI based on client actions.
    /// </summary>
    public class TcpConnection
    {
        /// <summary>
        /// Gets the underlying <see cref="TcpClient"/> instance associated with this connection.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpConnection"/> class.
        /// Sets up command parsers, UI references, and prepares the communication thread.
        /// </summary>
        /// <param name="client">The connected TCP client instance.</param>
        /// <param name="listener">The listener instance that created this connection (used for callbacks).</param>
        /// <param name="clientPanel">UI panel for displaying client information.</param>
        /// <param name="clientCounter">UI element displaying the count of active connections.</param>
        /// <param name="numberOfClients">UI element displaying the total number of registered accounts.</param>
        /// <param name="bankAmount">UI element displaying the total bank funds.</param>
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

        /// <summary>
        /// Starts the dedicated background thread for handling client communication.
        /// </summary>
        public void Start()
        {
            _clientThread.Start();
        }

        /// <summary>
        /// The main execution loop running on a background thread. It continuously processes incoming data 
        /// by calling the Do() method until the connection is terminated or an error occurs. 
        /// Ensures that the client is properly disconnected and removed from the listener's list when the loop ends.
        /// </summary>
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

        /// <summary>
        /// Performs a single iteration of the communication logic. It reads a line from the network stream, 
        /// parses it into a command, executes the command using CommandExecutor, and triggers UI updates 
        /// (refreshing client count or bank amount) if specific commands (AC, AR, AD, AW) are received.
        /// </summary>
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

                Log.Error($"ER: {ex.Message}");
                _isRunning = false;
            }
            catch (Exception ex)
            {
                Log.Error($"ER: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely terminates the connection by stopping the communication loop, closing the network readers/writers, 
        /// closing the TCP client connection, and interrupting the background thread to release resources immediately.
        /// </summary>
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
            catch { }

            if (_clientThread != null && _clientThread.IsAlive)
                _clientThread.Interrupt();

        }

        /// <summary>
        /// Reads the "accounts.json" file to determine the current number of registered clients and updates 
        /// the corresponding UI element. This method uses the Dispatcher to ensure the UI update happens on the main thread.
        /// </summary>
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

        /// <summary>
        /// Calculates the total sum of funds across all accounts in "accounts.json" and updates the Bank Amount UI element. 
        /// This method executes on the UI thread via Dispatcher and also calls FormatBankAmount to adjust the font size.
        /// </summary>
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

        /// <summary>
        /// Dynamically adjusts the font size of the Bank Amount text block based on the number of digits 
        /// to ensure the text fits within the UI container without overflowing.
        /// </summary>
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

        /// <summary>
        /// Sends a predefined rejection message to the client indicating that the bank server has reached 
        /// its maximum connection capacity and cannot accept new connections at this time.
        /// </summary>
        public void SendMessageCapacityFull()
        {
            byte[] data = Encoding.UTF8.GetBytes("The maximum number of clients allowed by the bank is connected to the bank. Please try connecting later." + Environment.NewLine);
            Client.GetStream().Write(data, 0, data.Length);
        }

    }
}
