using P2P_Project.Data_access_layer;
using P2P_Project.Presentation_layer;
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

        public MainWindow()
        {
            InitializeComponent();
            _configLoader = new ConfigLoader();


            _clientListener = new ClientListener("127.0.0.1",8000,50, ErrorPanel); //(_configLoader.IPAddress,_configLoader.Port, _configLoader.TimeoutTime, Errors);
            _clientListener.Start();
        }
    }
}