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
        private ConfigLoader _configLoader;

        public ProxyClient(IPAddress ipAddress)
        {
            _ipAddress = ipAddress;
            Port = FindPort(_ipAddress);
        }

        private int FindPort(IPAddress iPAddress)
        {
            int port = 0;

            int startPort = ConfigLoader.Instance.ScanPortStart;
            int endPort = ConfigLoader.Instance.ScanPortEnd;


            for (int p = startPort; p <= endPort; p++)
            {
                try
                {
                    TcpClient connection = new TcpClient();
                    connection.Connect(iPAddress, p);

                    using (NetworkStream stream = connection.GetStream())
                    {
                        StreamReader reader = new StreamReader(stream);
                        StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

                        writer.WriteLine("BC");
                        string response = reader.ReadLine();

                        if (CorrectAnswer(response) == true)
                        {
                            connection.Close();
                        }

                    }

                    port = p;
                    break;

                }
                catch (SocketException)
                {
                    continue;
                }
            }

            if (port != 0)
            {
                return port;
            }

            startPort = 65525;
            endPort = ConfigLoader.Instance.ScanPortEnd;

            for (int p = startPort; p <= endPort; p++)
            {
                try
                {
                    TcpClient connection = new TcpClient();
                    connection.Connect(iPAddress, p);

                    using (NetworkStream stream = connection.GetStream())
                    {
                        StreamReader reader = new StreamReader(stream);
                        StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

                        writer.WriteLine("BC");
                        string response = reader.ReadLine();

                        if (!CorrectAnswer(response))
                        {
                            connection.Close();
                        }

                    }
                    connection.Close();

                    port = p;
                    break;

                }
                catch (SocketException)
                {
                    continue;
                }
            }

            return port;
        }


        private bool CorrectAnswer(string response)
        {
            string[] parts = response.Split(' ');

            try
            {
                if (parts[0] == "BC" && IPAddress.Parse(parts[1]) == _ipAddress)
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