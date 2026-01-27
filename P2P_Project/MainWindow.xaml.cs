using P2P_Project.Data_access_layer;
using P2P_Project.Presentation_layer;
using Serilog;
using Serilog.Events;
using System.Windows;
using System.Reactive.Linq;
using System.Windows.Controls;


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
            CreateLogger();

            InitializeComponent();

            _clientListener = new ClientListener(ConfigLoader.Instance.IPAddress, ConfigLoader.Instance.AppPort, ConfigLoader.Instance.TimeoutTime, ErrorPanel, ClientPanel, ClientCounter);
            _clientListener.Start();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_clientListener != null)
            {
                _clientListener.Stop();
            }

            base.OnClosing(e);
        }
    }
}