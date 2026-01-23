using P2P_Project.Presentation_layer;
using P2P_Project.Data_access_layer;
using System.Configuration;
using System.Windows;


namespace P2P_Project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ClientListener _clientListener;
        private ConfigurationManager _configManager;

        public MainWindow()
        {
            InitializeComponent();



            _clientListener = new ClientListener();
        }
    }
}