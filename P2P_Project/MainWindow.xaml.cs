using P2P_Project.Data_access_layer;
using P2P_Project.Presentation_layer;
using Serilog;
using Serilog.Events;
using System.Windows;


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
            //Logger for UI
            //.WriteTo.Observers(events => events
            //    .Do(evt => {
            //        Application.Current.Dispatcher.Invoke(() => {
            //            MyTextBox.AppendText($"[{evt.Level}] {evt.RenderMessage()}\n");
            //        });
            //    }).Subscribe())

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
            _configLoader = new ConfigLoader();

            _clientListener = new ClientListener("127.0.0.1", 8000, 50, ErrorPanel); //(_configLoader.IPAddress,_configLoader.Port, _configLoader.TimeoutTime, Errors);
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