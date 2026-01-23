using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ConfigLoader
{
    private readonly string ConfigFilePath = Path.Combine("config", "config.json");

    private string _ipAddress;
    private int _port;
    private int _timeoutTime;

    public string IPAddress
    {
        get => _ipAddress;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("IP address cannot be empty");

            string[] octets = value.Split('.');
            if (octets.Length != 4 || !octets.All(o => int.TryParse(o, out int v) && v >= 0 && v <= 255))
            {
                throw new ArgumentException($"Invalid IP address");
            }
            _ipAddress = value;
        }
    }

    public int Port
    {
        get => _port;
        set
        {
            if (value < 1024 || value > 65535)
            {
                throw new ArgumentException($"Port must be from range of 1024 - 65535");
            }
            _port = value;
        }
    }
    public int TimeoutTime
    {
        get => _timeoutTime;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentException("Timeout time must be a number greater than 0");
            }
            _timeoutTime = value;
        }
    }

    public void LoadConfig()
    {
        try
        {
            string jsonString = File.ReadAllText(ConfigFilePath);

            var configDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);

            string[] requiredKeys = { "IPAddress", "Port", "TimeoutTime" };

            foreach (var key in requiredKeys)
            {
                if (!configDict.ContainsKey(key))
                {
                    throw new ArgumentException($"Missing required configuration key: {key}");
                }
            }

            this.IPAddress = configDict["IPAddress"].GetString();
            this.Port = configDict["Port"].GetInt32();
            this.TimeoutTime = configDict["TimeoutTime"].GetInt32();
        }
        catch (FileNotFoundException)
        {
            throw new FileNotFoundException($"ER Configuration file at {ConfigFilePath} not found.");
        }
        catch (JsonException ex)
        {
            throw new Exception($"ER Incorrect configuration format: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            throw new Exception($"ER Invalid configuration: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new Exception($"ER Failed to load configuration: {ex.Message}");
        }
    }
}
