using Serilog;
using System.IO;
using System.Text.Json;
using System.Net;

namespace P2P_Project.Data_access_layer
{
    public class ConfigLoader
    {
        private static readonly ConfigLoader _instance = new ConfigLoader();
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

        private ConfigLoader()
        {
            LoadConfig();
        }

        public string IPAddress { get => _ipAddress; private set => _ipAddress = ValidateIp(value); }
        public int AppPort { get => _appPort; private set => _appPort = ValidatePort(value); }
        public int TimeoutTime { get => _timeoutTime; private set => _timeoutTime = value > 0 ? value : throw new ArgumentException("ER TimeoutTime must be a number greater than 0"); }
        public int MaxConnectionCount { get => _maxConnectionCount; private set => _maxConnectionCount = value > 0 ? value : throw new ArgumentException("ER MaxConnectionCount must be a number greater than 0"); }

        public string ScanIpStart { get => _scanIpStart; private set => _scanIpStart = ValidateIp(value); }
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

        public int ScanPortStart { get => _scanPortStart; private set => _scanPortStart = ValidatePort(value); }
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

        private int ValidatePort(int port)
        {
            if (port < 1024 || port > 65535)
                throw new ArgumentException("ER Invalid port");
            return port;
        }

        private long ConvertIpToNumber(string ip)
        {
            string[] s = ip.Split('.');
            return (long.Parse(s[0]) << 24) | (long.Parse(s[1]) << 16) | (long.Parse(s[2]) << 8) | long.Parse(s[3]);
        }
    }
}