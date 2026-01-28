using Serilog;
using System;
using System.IO;
using System.Text.Json;

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

        public bool IsLoaded { get; private set; } = false;
        public string LoadError { get; private set; } = "Unknown error";

        private ConfigLoader()
        {
            LoadConfig();
        }

        public string IPAddress { get => _ipAddress; private set => _ipAddress = ValidateIp(value); }
        public int AppPort { get => _appPort; private set => _appPort = ValidatePort(value); }
        public int TimeoutTime { get => _timeoutTime; private set => _timeoutTime = value > 0 ? value : throw new ArgumentException("TimeoutTime must be greater than 0"); }
        public int MaxConnectionCount { get => _maxConnectionCount; private set => _maxConnectionCount = value > 0 ? value : throw new ArgumentException("MaxConnectionCount must be greater than 0"); }
        public string ScanIpStart { get => _scanIpStart; private set => _scanIpStart = ValidateIp(value); }
        public string ScanIpEnd
        {
            get => _scanIpEnd;
            private set
            {
                string val = ValidateIp(value);
                if (ConvertIpToNumber(val) < ConvertIpToNumber(ScanIpStart)) throw new ArgumentException("ScanIpEnd must be higher than ScanIpStart");
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
                if (val < _scanPortStart) throw new ArgumentException("ScanPortEnd must be higher than ScanPortStart");
                _scanPortEnd = val;
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    CreateTemplateConfig();
                    LoadError = $"Config file was missing. A template has been created at {ConfigFilePath}. Please fill it out.";
                    IsLoaded = false;
                    return;
                }

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

                IsLoaded = true;
                LoadError = string.Empty;
            }
            catch (Exception ex)
            {
                IsLoaded = false;
                LoadError = ex.Message;
                Log.Fatal(ex, "ER Failed to load configuration.");
            }
        }

        private void CreateTemplateConfig()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
                var template = new
                {
                    IPAddress = "127.0.0.1",
                    AppPort = 8080,
                    TimeoutTime = 5000,
                    MaxConnectionCount = 10,
                    ScanIpStart = "127.0.0.1",
                    ScanIpEnd = "127.0.0.1",
                    ScanPortStart = 8080,
                    ScanPortEnd = 8081
                };
                string json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex) { Log.Error(ex, "ER Could not create template."); }
        }

        private string ValidateIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) throw new ArgumentException("ER Failed to load configuration: IP address cannot be empty.");
            string[] segments = ip.Split('.');
            if (segments.Length != 4) throw new ArgumentException($"ER Failed to load configuration: Invalid IPv4 format: {ip}");
            foreach (string segment in segments)
            {
                if (!int.TryParse(segment, out int value) || value < 0 || value > 255)
                    throw new ArgumentException($"ER Failed to load configuration: Invalid IPv4 address");
            }
            return ip;
        }

        private int ValidatePort(int port)
        {
            if (port < 1024 || port > 65535)
                throw new ArgumentException($"ER Failed to load configuration: Port {port} is invalid. Use a range between 1024 and 65535.");
            return port;
        }

        private long ConvertIpToNumber(string ip)
        {
            string[] s = ip.Split('.');
            return (long.Parse(s[0]) << 24) | (long.Parse(s[1]) << 16) | (long.Parse(s[2]) << 8) | long.Parse(s[3]);
        }
    }
}