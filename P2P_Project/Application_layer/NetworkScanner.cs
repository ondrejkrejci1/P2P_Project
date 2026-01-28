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
    /// <summary>
    /// Responsible for discovering other active nodes in the P2P network.
    /// It scans a defined range of IP addresses to find reachable devices.
    /// </summary>
    public class NetworkScanner
    {
        private long _startIp;
        private long _endIp;
        private int _timeoutTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkScanner"/> class.
        /// Loads the scan range (Start/End IPs) and timeout settings from the global configuration.
        /// </summary>
        public NetworkScanner()
        {
            var config = ConfigLoader.Instance;
            _timeoutTime = 100;
        }

        /// <summary>
        /// Asynchronously scans the configured network range for active devices.
        /// Launches parallel ping tasks for every IP in the range to maximize performance.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of active IP address strings.</returns>
        public async Task<List<string>> ScanNetworkAsync()
        {
            Log.Information("Starting network scan");

            List<Task<string?>> scanTasks = new List<Task<string?>>();

            foreach (var range in ConfigLoader.Instance.ScanIpRanges)
            {
                long start = ConvertIpToNumber(range.Start);
                long end = ConvertIpToNumber(range.End);

                for (long i = start; i <= end; i++)
                {
                    string ip = ConvertNumberToIp(i);
                    scanTasks.Add(PingDeviceAsync(ip, _timeoutTime));
                }
            }

            var results = await Task.WhenAll(scanTasks);
            List<string> activeDevices = new List<string>();

            foreach (var result in results)
            {
                if (result != null) activeDevices.Add(result);
            }
            Log.Information("Network scan finished. Found {Count} active devices.", activeDevices.Count);
            return activeDevices;
        }

        /// <summary>
        /// Sends an ICMP ping to a specific IP address to check its availability.
        /// </summary>
        /// <param name="ip">The IPv4 address to ping.</param>
        /// <param name="timeout">The maximum time to wait for a reply, in milliseconds.</param>
        /// <returns>The IP address string if the ping was successful; otherwise, null.</returns>
        private async Task<string?> PingDeviceAsync(string ip, int timeout)
        {
            try
            {
                Log.Debug($"Pinging {ip}");
                using Ping pingSender = new Ping();
                PingReply reply = await pingSender.SendPingAsync(ip, timeout);

                if (reply.Status == IPStatus.Success)
                {
                    Log.Debug($"Device found at {ip}: Ping successful.");
                    return ip;
                }
                else
                {
                    Log.Debug($"Ping to {ip} failed: {reply.Status}");
                }

            }
            catch {}

            return null;
        }

        /// <summary>
        /// Converts an IPv4 string (e.g., "192.168.1.1") to its numeric equivalent.
        /// Useful for iterating through a range of IP addresses in a loop.
        /// </summary>
        /// <param name="ip">The string representation of the IP address.</param>
        /// <returns>The IP address as a long integer.</returns>
        private long ConvertIpToNumber(string ip)
        {
            byte[] bytes = IPAddress.Parse(ip).GetAddressBytes();

            Array.Reverse(bytes);

            return BitConverter.ToUInt32(bytes, 0);
        }

        /// <summary>
        /// Converts a numeric IP representation back to its standard IPv4 string format.
        /// </summary>
        /// <param name="number">The numeric representation of the IP.</param>
        /// <returns>The formatted IP address string (e.g., "192.168.1.1").</returns>
        private string ConvertNumberToIp(long number)
        {
            byte[] bytes = BitConverter.GetBytes((uint)number);

            Array.Reverse(bytes);

            return new IPAddress(bytes).ToString();
        }
    }
}