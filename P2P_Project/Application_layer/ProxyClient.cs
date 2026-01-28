using P2P_Project.Data_access_layer;
using Serilog;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace P2P_Project.Application_layer
{
    public class ProxyClient
    {
        private IPAddress _ipAddress;
        public int Port { private set; get; }

        public ProxyClient(IPAddress ipAddress)
        {
            _ipAddress = ipAddress;
        }

        public static async Task<ProxyClient> CreateClient(IPAddress ipAddress)
        {
            ProxyClient client = new ProxyClient(ipAddress);

            client.Port = await client.FindPort(ipAddress);

            return client;
        }

        private async Task<int> FindPort(IPAddress iPAddress)
        {
            List<int> portsToScan = new List<int>();

            foreach (var range in ConfigLoader.Instance.ScanPortRanges)
            {
                for (int p = range.Start; p <= range.End; p++)
                {
                    portsToScan.Add(p);
                }
            }

            List<Task<int>> tasks = new List<Task<int>>();

            foreach (int port in portsToScan)
            {
                tasks.Add(CheckPortAsync(iPAddress, port));
            }

            while (tasks.Count > 0)
            {
                var completedTask = await Task.WhenAny(tasks);
                tasks.Remove(completedTask);

                int foundPort = await completedTask;
                if (foundPort != 0) return foundPort;
            }

            return 0;
        }

        private async Task<int> CheckPortAsync(IPAddress ip, int port)
        {
            using (TcpClient client = new TcpClient())
            {
                try
                {
                    var connectTask = client.ConnectAsync(ip, port);
                    int timeout = ConfigLoader.Instance.TimeoutTime * 10000;
                    var completedTask = await Task.WhenAny(connectTask, Task.Delay(timeout));

                    if (completedTask != connectTask)
                    {
                        return 0;
                    }

                    await connectTask;

                    if (!client.Connected) return 0;

                    using (NetworkStream stream = client.GetStream())
                    using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        await writer.WriteLineAsync("BC");

                        var readTask = reader.ReadLineAsync();
                        var completedRead = await Task.WhenAny(readTask, Task.Delay(ConfigLoader.Instance.TimeoutTime * 1000));

                        if (completedRead != readTask) return 0;

                        string response = await readTask;

                        if (CorrectAnswer(response) == true)
                        {
                            return port;
                        }
                    }
                }
                catch (Exception)
                {
                    return 0;
                }
            }
            return 0;
        }

        private bool CorrectAnswer(string response)
        {
            string[] parts = response.Split(' ');

            try
            {
                if (parts[0] == "BC" && parts[1] == _ipAddress.ToString())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

        }


        public string ForwardRequest(string request)
        {
            if (Port == 0)
            {
                Log.Error("ER Connection failure: No open port found for {IP}", _ipAddress);
                return $"ER Unable to connect to {_ipAddress}. No open port found";
            }

            string response = "ER bank was unable to process the request";

            try
            {
                Log.Information("Sending request to {IP}:{Port} | Content: {Request}", _ipAddress, Port, request);

                TcpClient connection = new TcpClient();
                connection.Connect(_ipAddress, Port);

                using (NetworkStream stream = connection.GetStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

                    writer.WriteLine(request);
                    response = reader.ReadLine();
                }

                connection.Close();

                Log.Information("Received response from {IP}:{Port} | Content: {Response}", _ipAddress, Port, response);
            }
            catch (SocketException ex)
            {
                response = $"ER Unable to connect to {_ipAddress}:{Port} - {ex.Message}";
                Log.Error(ex, "ER Communication error with {IP}:{Port}", _ipAddress, Port);
            }

            return response;
        }
    }
}