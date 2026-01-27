using P2P_Project.Data_access_layer;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace P2P_Project.Application_layer
{
    public class NetworkScanner
    {
        private long _startIp;
        private long _endIp;
        private int _timeoutTime;

        public NetworkScanner() 
        {
            var config = ConfigLoader.Instance;
            _startIp = ConvertIpToNumber(config.ScanIpStart);
            _endIp = ConvertIpToNumber(config.ScanIpEnd);
            _timeoutTime = config.TimeoutTime;
        }

        public async Task<List<string>> ScanNetworkAsync()
        {
            Log.Information("Starting network scan");

            List<Task<string?>> scanTasks = new List<Task<string?>>();

            for (long i = _startIp; i <= _endIp; i++)
            {
                string ip = ConvertNumberToIp(i);
                scanTasks.Add(PingDeviceAsync(ip, _timeoutTime));
            }

            var results = await Task.WhenAll(scanTasks);
            List<string> activeDevices = new List<string>();

            foreach (var result in results)
            {
                if (result != null) activeDevices.Add(result);
            }
            Log.Information("Network scan finished");
            return activeDevices;
        }

        private async Task<string?> PingDeviceAsync(string ip, int timeout)
        {
            try
            {
                using Ping pingSender = new Ping();
                PingReply reply = await pingSender.SendPingAsync(ip, timeout);

                if (reply.Status == IPStatus.Success)
                {
                    Log.Information($"Device found at {ip}: Ping successful.");
                    return ip;
                }

                Log.Debug($"IP {ip} rejected: {reply.Status}");
            }
            catch (Exception ex)
            {
                Log.Debug($"IP {ip} rejected: {ex.Message}");
            }

            return null;
        }

        private long ConvertIpToNumber(string ip)
        {
            byte[] bytes = IPAddress.Parse(ip).GetAddressBytes();

            Array.Reverse(bytes);

            return BitConverter.ToUInt32(bytes, 0);
        }

        private string ConvertNumberToIp(long number)
        {
            byte[] bytes = BitConverter.GetBytes((uint)number);

            Array.Reverse(bytes);

            return new IPAddress(bytes).ToString();
        }
    }
}