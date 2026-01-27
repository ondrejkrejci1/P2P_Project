using P2P_Project.Data_access_layer;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace P2P_Project.Application_layer
{
    /// <summary>
    /// Coordinator for the "Robbery" simulation protocol.
    /// This class scans the P2P network, gathers intelligence on other bank nodes, 
    /// and calculates an optimal strategy to acquire a specific target amount of funds.
    /// </summary>
    public class RobberyPlanner
    {
        private static readonly RobberyPlanner _instance = new RobberyPlanner();

        /// <summary>
        /// Gets the singleton instance of the RobberyPlanner.
        /// </summary>
        public static RobberyPlanner Instance => _instance;

        private readonly NetworkScanner _scanner;

        /// <summary>
        /// Private constructor to enforce the Singleton pattern.
        /// Initializes the network scanner component.
        /// </summary>
        private RobberyPlanner()
        {
            _scanner = new NetworkScanner();
        }

        /// <summary>
        /// Orchestrates the entire robbery planning process: scanning, data collection, and path calculation.
        /// </summary>
        /// <param name="targetAmount">The total amount of money the user wishes to "steal".</param>
        /// <returns>A string describing the plan (list of banks to rob) or an error message if the amount is unreachable.</returns>
        public async Task<string> ExecuteRobberyPlan(long targetAmount)
        {
            var networkData = await CollectNetworkData();

            if (networkData.Length == 0)
            {
                return "ER no other bank nodes were found";
            }

            var optimalPath = GetOptimalRobberyPath(networkData);
            return GeneratePlanResponse(optimalPath, targetAmount);
        }

        /// <summary>
        /// Scans the network range defined in the config and retrieves financial stats from every active node.
        /// Filters out the local host and unresponsive/invalid nodes.
        /// </summary>
        /// <returns>An array of valid <see cref="BankNodeData"/> objects containing IP, total wealth, and client counts.</returns>
        private async Task<BankNodeData[]> CollectNetworkData()
        {
            var activeIps = await _scanner.ScanNetworkAsync();
            var nodes = new List<BankNodeData>();

            foreach (string ip in activeIps)
            {
                if (ip == ConfigLoader.Instance.IPAddress) continue;

                var stats = GetBankStats(ip);

                if (stats.Amount >= 0 && stats.Clients >= 0)
                {
                    nodes.Add(stats);
                }
            }

            return nodes.ToArray();
        }

        /// <summary>
        /// Connects to a specific remote IP using a ProxyClient to request its "BA" (Bank Amount) and "BN" (Bank Number of clients).
        /// </summary>
        /// <param name="ip">The IP address of the target bank node.</param>
        /// <returns>A data object containing the parsed stats for that node.</returns>
        private BankNodeData GetBankStats(string ip)
        {
            ProxyClient proxy = new ProxyClient(IPAddress.Parse(ip));

            string totalAmountResponse = proxy.ForwardRequest("BA");
            string clientCountResponse = proxy.ForwardRequest("BN");

            return new BankNodeData(
                ip,
                ParseAmount(totalAmountResponse),
                ParseClients(clientCountResponse)
            );
        }

        /// <summary>
        /// Sorts the list of available banks to determine the most efficient robbery targets.
        /// The sorting logic relies on the 'AverageWealth' property (Amount / Clients) to maximize return per victim.
        /// </summary>
        /// <param name="networkData">The raw list of gathered bank data.</param>
        /// <returns>The array sorted by desirability (highest average wealth first).</returns>
        private BankNodeData[] GetOptimalRobberyPath(BankNodeData[] networkData)
        {
            Array.Sort(networkData, (a, b) => b.AverageWealth.CompareTo(a.AverageWealth));
            return networkData;
        }

        /// <summary>
        /// Constructs the final result string by iterating through the sorted bank list 
        /// until the cumulative stolen amount meets or exceeds the target.
        /// </summary>
        /// <param name="optimalPath">The sorted list of target banks.</param>
        /// <param name="targetAmount">The goal amount.</param>
        /// <returns>A formatted protocol string starting with "RP", or an error if funds are insufficient.</returns>
        private string GeneratePlanResponse(BankNodeData[] optimalPath, long targetAmount)
        {
            long currentTotal = 0;
            long totalClientsAffected = 0;
            var targetBanks = new List<string>();

            foreach (var node in optimalPath)
            {
                if (currentTotal >= targetAmount) break;

                currentTotal += node.Amount;
                totalClientsAffected += node.Clients;
                targetBanks.Add(node.Ip);
            }

            if (currentTotal < targetAmount)
            {
                return "RP Plan will fail: Insufficient funds in the network";
            }

            return $"RP To obtain {targetAmount} you will need to rob {string.Join(", ", targetBanks)} affecting {totalClientsAffected} clients.";
        }

        /// <summary>
        /// Parses the "BA {amount}" response string.
        /// </summary>
        /// <returns>The amount as a long, or -1 if parsing fails.</returns>
        private long ParseAmount(string response)
        {
            if (string.IsNullOrEmpty(response) || response.StartsWith("ER")) return -1;
            try
            {
                string[] parts = response.Split(' ');
                if (parts.Length == 2 && parts[0] == "BA") return long.Parse(parts[1]);
            }
            catch { }
            return -1;
        }

        /// <summary>
        /// Parses the "BN {count}" response string.
        /// </summary>
        /// <returns>The client count as a long, or -1 if parsing fails.</returns>
        private long ParseClients(string response)
        {
            if (string.IsNullOrEmpty(response) || response.StartsWith("ER")) return -1;
            try
            {
                string[] parts = response.Split(' ');
                if (parts.Length == 2 && parts[0] == "BN") return long.Parse(parts[1]);
            }
            catch { }
            return -1;
        }
    }
}