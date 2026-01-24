using P2P_Project.Data_access_layer;
using P2P_Project.Presentation_layer;
using Serilog;
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
            .WriteTo.File(
                path: "logs/myapp.txt",
                rollingInterval: RollingInterval.Day,
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                shared: true
            )
            //.WriteTo.Observers(events => events
            //    .Do(evt =>
            //    {
            //    }).Subscribe())
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
    }
}