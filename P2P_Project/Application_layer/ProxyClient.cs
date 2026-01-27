using P2P_Project.Data_access_layer;
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

            int minPort = ConfigLoader.Instance.ScanPortStart;
            int maxPort = 8090;


            for (int p = minPort; p <= maxPort; p++)
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

            minPort = 65525;
            maxPort = ConfigLoader.Instance.ScanPortEnd;

            for (int p = minPort; p <= maxPort; p++)
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
                return $"ER Unable to connect to {_ipAddress}: No open port found";
            }

            string response = "";

            try
            {
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
            }
            catch (SocketException ex)
            {
                response = $"ER Unable to connect to {_ipAddress}:{Port} - {ex.Message}";
            }

            return response;
        }



    }
}
