using P2P_Project.Data_access_layer;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace P2P_Project.Application_layer
{
    public class RobberyPlanner
    {
        private static readonly RobberyPlanner _instance = new RobberyPlanner();
        public static RobberyPlanner Instance => _instance;
        private readonly NetworkScanner _scanner;
        private RobberyPlanner()
        {
            _scanner = new NetworkScanner();
        }

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

        private BankNodeData[] GetOptimalRobberyPath(BankNodeData[] networkData)
        {
            Array.Sort(networkData, (a, b) => b.AverageWealth.CompareTo(a.AverageWealth));
            return networkData;
        }

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