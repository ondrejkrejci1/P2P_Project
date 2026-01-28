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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ClientListener _clientListener;
        private ConfigLoader _configLoader;

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
                            Margin = new Thickness(10, 5, 0, 0)

                        };
                        LoggerPanel.Children.Add(loggerText);
                    });
                }).Subscribe())

            .WriteTo.File(
                path: "logs/status.txt",
                restrictedToMinimumLevel: LogEventLevel.Information,
                rollingInterval: RollingInterval.Day,
                flushToDiskInterval: TimeSpan.FromSeconds(1))

            .WriteTo.File(
                path: "logs/errors.txt",
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();
        }

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

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_clientListener != null)
            {
                _clientListener.Stop();
            }

            base.OnClosing(e);
        }


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

        private void ShowActiveClients(object sender, MouseEventArgs e)
        {
            ActiveClientsHover.Visibility = Visibility.Visible;
        }

        private void HideActiveClients(object sender, MouseEventArgs e)
        {
            ActiveClientsHover.Visibility = Visibility.Hidden;
        }

        private void ShowNumberOfClients(object sender, MouseEventArgs e)
        {
            NumberOfClientsHover.Visibility = Visibility.Visible;
        }

        private void HideNumberOfClients(object sender, MouseEventArgs e)
        {
            NumberOfClientsHover.Visibility = Visibility.Hidden;
        }

        private void ShowBankAmount(object sender, MouseEventArgs e)
        {
            BankAmountHover.Visibility = Visibility.Visible;
        }

        private void HideBankAmount(object sender, MouseEventArgs e)
        {
            BankAmountHover.Visibility = Visibility.Hidden;
        }

    }
}