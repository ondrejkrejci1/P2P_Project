using Serilog;
using System.IO;
using System.Text.Json;

namespace P2P_Project.Data_access_layer
{
    /// <summary>
    /// centralized configuration manager for the application.
    /// Implements the Singleton pattern to ensure global access to immutable settings loaded from disk.
    /// Responsible for validating network parameters (IPs, Ports) upon loading.
    /// </summary>
    public class ConfigLoader
    {
        private static readonly ConfigLoader _instance = new ConfigLoader();

        /// <summary>
        /// Gets the single, globally accessible instance of the <see cref="ConfigLoader"/>.
        /// </summary>
        public static ConfigLoader Instance => _instance;

        private readonly string ConfigFilePath = Path.Combine("config", "config.json");

        private string _ipAddress;
        private int _appPort;
        private int _timeoutTime;
        private int _maxConnectionCount;
        private string _scanIpStart;
        private string _scanIpEnd;
        private int _scanPortStart;
        private int _scanPortEnd;

        /// <summary>
        /// Private constructor to enforce the Singleton pattern.
        /// Triggers the loading of configuration data immediately upon instantiation.
        /// </summary>
        private ConfigLoader()
        {
            LoadConfig();
        }

        /// <summary>
        /// Gets the local IP address this server is bound to.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if the IP format is invalid.</exception>
        public string IPAddress { get => _ipAddress; private set => _ipAddress = ValidateIp(value); }

        /// <summary>
        /// Gets the port number this server listens on.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if the port is outside the allowed range (1024-65535).</exception>
        public int AppPort { get => _appPort; private set => _appPort = ValidatePort(value); }

        /// <summary>
        /// Gets the timeout duration (in milliseconds) for network operations.
        /// </summary>
        public int TimeoutTime { get => _timeoutTime; private set => _timeoutTime = value > 0 ? value : throw new ArgumentException("ER TimeoutTime must be a number greater than 0"); }

        /// <summary>
        /// Gets the maximum number of concurrent client connections allowed.
        /// </summary>
        public int MaxConnectionCount { get => _maxConnectionCount; private set => _maxConnectionCount = value > 0 ? value : throw new ArgumentException("ER MaxConnectionCount must be a number greater than 0"); }

        /// <summary>
        /// Gets the starting IP address for the network discovery scan range.
        /// </summary>
        public string ScanIpStart { get => _scanIpStart; private set => _scanIpStart = ValidateIp(value); }

        /// <summary>
        /// Gets the ending IP address for the network discovery scan range.
        /// Must be numerically greater than or equal to <see cref="ScanIpStart"/>.
        /// </summary>
        public string ScanIpEnd
        {
            get => _scanIpEnd;
            private set
            {
                string val = ValidateIp(value);
                if (ConvertIpToNumber(val) < ConvertIpToNumber(ScanIpStart)) throw new ArgumentException("ER ScanIpEnd must be higher than ScanIpStart");
                _scanIpEnd = val;
            }
        }

        /// <summary>
        /// Gets the starting port number for the network discovery scan range.
        /// </summary>
        public int ScanPortStart { get => _scanPortStart; private set => _scanPortStart = ValidatePort(value); }

        /// <summary>
        /// Gets the ending port number for the network discovery scan range.
        /// Must be greater than or equal to <see cref="ScanPortStart"/>.
        /// </summary>
        public int ScanPortEnd
        {
            get => _scanPortEnd;
            private set
            {
                int val = ValidatePort(value);
                if (val < _scanPortStart) throw new ArgumentException("ER ScanPortEnd must be higher than ScanPortStart");
                _scanPortEnd = val;
            }
        }

        /// <summary>
        /// Reads and parses the 'config.json' file.
        /// Populates the class properties with values from the JSON document.
        /// </summary>
        /// <exception cref="Exception">Thrown if the file is missing or the configuration is invalid.</exception>
        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigFilePath)) throw new Exception();

                string jsonString = File.ReadAllText(ConfigFilePath);
                using JsonDocument doc = JsonDocument.Parse(jsonString);
                JsonElement root = doc.RootElement;

                IPAddress = root.GetProperty("IPAddress").GetString();
                AppPort = root.GetProperty("AppPort").GetInt32();
                TimeoutTime = root.GetProperty("TimeoutTime").GetInt32();
                MaxConnectionCount = root.GetProperty("MaxConnectionCount").GetInt32();

                ScanIpStart = root.GetProperty("ScanIpStart").GetString();
                ScanIpEnd = root.GetProperty("ScanIpEnd").GetString();

                ScanPortStart = root.GetProperty("ScanPortStart").GetInt32();
                ScanPortEnd = root.GetProperty("ScanPortEnd").GetInt32();
            }
            catch (Exception)
            {
                Log.Error("ER Failed to load app configuration");
                throw new Exception("ER Failed to load app configuration");
            }
        }

        /// <summary>
        /// Validates that a string is a correctly formatted IPv4 address.
        /// </summary>
        /// <param name="ip">The IP string to check.</param>
        /// <returns>The valid IP string.</returns>
        /// <exception cref="ArgumentException">Thrown if the format is incorrect or segments are out of range.</exception>
        private string ValidateIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) throw new ArgumentException("Invalid IP address");
            string[] segments = ip.Split('.');
            if (segments.Length != 4) throw new ArgumentException("Invalid IP address");
            foreach (string segment in segments)
            {
                if (!int.TryParse(segment, out int value) || value < 0 || value > 255)
                    throw new ArgumentException("Invalid IP address");
            }
            return ip;
        }

        /// <summary>
        /// Validates that a port number is within the allowable range (1024 - 65535).
        /// </summary>
        /// <param name="port">The port number to check.</param>
        /// <returns>The valid port number.</returns>
        /// <exception cref="ArgumentException">Thrown if the port is reserved or out of range.</exception>
        private int ValidatePort(int port)
        {
            if (port < 1024 || port > 65535)
                throw new ArgumentException("ER Invalid port");
            return port;
        }

        /// <summary>
        /// Converts an IPv4 string into a long integer for numerical comparison.
        /// Used to ensure the Start IP is lower than the End IP.
        /// </summary>
        /// <param name="ip">The IP address string.</param>
        /// <returns>The numeric representation of the IP.</returns>
        private long ConvertIpToNumber(string ip)
        {
            string[] s = ip.Split('.');
            return (long.Parse(s[0]) << 24) | (long.Parse(s[1]) << 16) | (long.Parse(s[2]) << 8) | long.Parse(s[3]);
        }
    }
}