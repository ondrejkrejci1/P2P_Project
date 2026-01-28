using P2P_Project.Data_access_layer;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace P2P_Project.Application_layer
{
    /// <summary>
    /// Represents a client proxy that communicates with a remote bank node.
    /// It handles the discovery of the active port on a target IP address and forwards requests to that node.
    /// </summary>
    public class ProxyClient
    {
        private IPAddress _ipAddress;

        /// <summary>
        /// Gets the active port number discovered on the target IP address. 
        /// Returns 0 if no open port was found.
        /// </summary>
        public int Port { private set; get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyClient"/> class with a target IP address.
        /// </summary>
        /// <param name="ipAddress">The IP address of the remote node.</param>
        public ProxyClient(IPAddress ipAddress)
        {
            _ipAddress = ipAddress;
        }

        /// <summary>
        /// Asynchronously creates a new instance of <see cref="ProxyClient"/> and attempts to discover the active port on the target IP.
        /// </summary>
        /// <param name="ipAddress">The IP address of the target node.</param>
        /// <returns>An initialized <see cref="ProxyClient"/> with the discovered port set.</returns>
        public static async Task<ProxyClient> CreateClient(IPAddress ipAddress)
        {
            ProxyClient client = new ProxyClient(ipAddress);

            client.Port = await client.FindPort(ipAddress);

            return client;
        }

        /// <summary>
        /// Scans the configured port ranges in parallel to find an active service on the target IP.
        /// </summary>
        /// <param name="iPAddress">The IP address to scan.</param>
        /// <returns>The first active port number found, or 0 if no ports are open.</returns>
        private async Task<int> FindPort(IPAddress iPAddress)
        {
            List<int> portsToScan = new List<int>();

            List<PortRange> portRanges = ConfigLoader.Instance.ScanPortRanges;

            foreach (PortRange range in portRanges)
            {
                for (int port = range.Start; port <= range.End; port++)
                {
                    portsToScan.Add(port);
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

        /// <summary>
        /// Attempts to connect to a specific port and verifies the service identity via a handshake.
        /// </summary>
        /// <param name="ip">The target IP address.</param>
        /// <param name="port">The port to check.</param>
        /// <returns>The port number if the connection and handshake are successful; otherwise, 0.</returns>
        private async Task<int> CheckPortAsync(IPAddress ip, int port)
        {
            using (TcpClient client = new TcpClient())
            {
                try
                {
                    var connectTask = client.ConnectAsync(ip, port);
                    int timeout = ConfigLoader.Instance.TimeoutTime;
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
                        var completedRead = await Task.WhenAny(readTask, Task.Delay(ConfigLoader.Instance.TimeoutTime));

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

        /// <summary>
        /// Validates the handshake response from the remote node.
        /// </summary>
        /// <param name="response">The response string received from the remote node.</param>
        /// <returns><c>true</c> if the response confirms the node identity; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Establishes a temporary connection to the discovered port and forwards a text request.
        /// </summary>
        /// <param name="request">The request string to send.</param>
        /// <returns>The response from the remote node, or an error message starting with "ER" if communication fails.</returns>
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