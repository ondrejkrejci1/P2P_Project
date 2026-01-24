using Serilog;
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
        private int _port;
        private int _timeoutTime;

        private ConfigLoader()
        {
            LoadConfig();
        }

        public string IPAddress
        {
            get => _ipAddress;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("IP address cannot be empty");
                _ipAddress = value;
            }
        }

        public int Port
        {
            get => _port;
            private set
            {
                if (value < 1024 || value > 65535)
                    throw new ArgumentException("Port must be from range of 1024 - 65535");
                _port = value;
            }
        }

        public int TimeoutTime
        {
            get => _timeoutTime;
            private set
            {
                if (value <= 0)
                    throw new ArgumentException("Timeout time must be a number greater than 0");
                _timeoutTime = value;
            }
        }

        private void LoadConfig()
        {
            try
            {

                string jsonString = File.ReadAllText(ConfigFilePath);
                var configDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);

                if (configDict.ContainsKey("IPAddress"))
                    IPAddress = configDict["IPAddress"].GetString();

                if (configDict.ContainsKey("Port"))
                    Port = configDict["Port"].GetInt32();

                if (configDict.ContainsKey("TimeoutTime"))
                    TimeoutTime = configDict["TimeoutTime"].GetInt32();
            }
            catch (Exception ex)
            {
                Log.Error($"ER Failed to load app configuration");
                throw new Exception($"ER failed to load app configuration: {ex.Message}");
            }
        }
    }
}