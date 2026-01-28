using P2P_Project.Data_access_layer;
using P2P_Project.Presentation_layer;
using Serilog;
using Serilog.Events;
using System.Windows;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Text.Json;
using System.IO;
using System.Windows.Input;


namespace P2P_Project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// Represents the main entry point of the banking server application, handling UI updates, 
    /// logging configuration, and the lifecycle of the client listener.
    /// </summary>
    public partial class MainWindow : Window
    {
        private ClientListener _clientListener;
        private ConfigLoader _configLoader;

        /// <summary>
        /// Configures the Serilog logger to output logs to both the UI panel and a text file.
        /// </summary>
        private void CreateLogger()
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()            
            .WriteTo.Observers(events => events
                .Do(evt => {
                    Application.Current.Dispatcher.Invoke(() => {
                        TextBlock loggerText = new TextBlock
                        {
                            Text = $"[{evt.Level}] {evt.RenderMessage()}",
                            Margin = new Thickness(10, 5, 5, 0),
                            TextWrapping = TextWrapping.Wrap
                        };
                        LoggerPanel.Children.Add(loggerText);
                    });
                }).Subscribe())

            .WriteTo.File(
                path: "logs/errors.txt",
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();
        }

        /// <summary>
        /// Initializes the main window, validates configuration, starts the client listener, and loads initial data.
        /// </summary>
        public MainWindow()
        {

            InitializeComponent();

            CreateLogger();

            if (!ConfigLoader.Instance.IsLoaded)
            {
                MessageBox.Show(
                    $"Critical Configuration Error:\n\n{ConfigLoader.Instance.LoadError}",
                    "Startup Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);

                Application.Current.Shutdown();
                return;
            }

            _clientListener = new ClientListener(ConfigLoader.Instance.IPAddress, ConfigLoader.Instance.AppPort, ConfigLoader.Instance.TimeoutTime, ClientPanel, ActiveClients, NumberOfClients, BankAmount);
            _clientListener.Start();

            LoadNumberOfClients();
            LoadBankAmount();
       
        }

        /// <summary>
        /// Stops the client listener when the window is closing.
        /// </summary>
        /// <param name="e">Event data for the closing event.</param>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_clientListener != null)
            {
                _clientListener.Stop();
            }

            base.OnClosing(e);
        }

        /// <summary>
        /// Loads the total number of clients from the JSON storage and updates the UI.
        /// </summary>
        private void LoadNumberOfClients()
        {
            try
            {
                if (!File.Exists("accounts.json")) NumberOfClients.Text = "0";

                string jsonString = File.ReadAllText("accounts.json");

                if (string.IsNullOrWhiteSpace(jsonString)) NumberOfClients.Text = "0";

                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        NumberOfClients.Text = doc.RootElement.GetArrayLength().ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading number of clients: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates the total funds from all accounts in storage and updates the UI.
        /// </summary>
        private void LoadBankAmount()
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

                BankAmount.Text = totalSum.ToString();
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading bank amount: {ex.Message}");
            }

            FormatBankAmount();
        }

        /// <summary>
        /// Adjusts the font size of the bank amount text based on the number of digits.
        /// </summary>
        private void FormatBankAmount()
        {
            int pocetCifer = BankAmount.Text.Length;

            if (pocetCifer <= 3) BankAmount.FontSize = 50;
            else if (pocetCifer == 4) BankAmount.FontSize = 38;
            else if (pocetCifer == 5) BankAmount.FontSize = 30;
            else if (pocetCifer == 6) BankAmount.FontSize = 24;
            else if (pocetCifer == 7) BankAmount.FontSize = 22;
            else if (pocetCifer == 8) BankAmount.FontSize = 18;
            else if (pocetCifer == 9) BankAmount.FontSize = 16;
            else if (pocetCifer == 10) BankAmount.FontSize = 14;
            else
            {
                BankAmount.FontSize = 14;
                BankAmount.TextWrapping = TextWrapping.NoWrap;
            }
        }

        /// <summary>
        /// Shows the hover effect for active clients.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Mouse event arguments.</param>
        private void ShowActiveClients(object sender, MouseEventArgs e)
        {
            ActiveClientsHover.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Hides the hover effect for active clients.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Mouse event arguments.</param>
        private void HideActiveClients(object sender, MouseEventArgs e)
        {
            ActiveClientsHover.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Shows the hover effect for number of clients.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Mouse event arguments.</param>
        private void ShowNumberOfClients(object sender, MouseEventArgs e)
        {
            NumberOfClientsHover.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Hides the hover effect for number of clients.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Mouse event arguments.</param>
        private void HideNumberOfClients(object sender, MouseEventArgs e)
        {
            NumberOfClientsHover.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Shows the hover effect for bank amount.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Mouse event arguments.</param>
        private void ShowBankAmount(object sender, MouseEventArgs e)
        {
            BankAmountHover.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Hides the hover effect for bank amount.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Mouse event arguments.</param>
        private void HideBankAmount(object sender, MouseEventArgs e)
        {
            BankAmountHover.Visibility = Visibility.Hidden;
        }

    }
}