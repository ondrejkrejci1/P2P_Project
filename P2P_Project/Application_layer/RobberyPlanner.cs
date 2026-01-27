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

        private readonly NetworkScanner _networkScanner;

        /// <summary>
        /// Private constructor to enforce the Singleton pattern.
        /// Initializes the network scanner component.
        /// </summary>
        private RobberyPlanner()
        {
            _networkScanner = new NetworkScanner();
        }

        /// <summary>
        /// Orchestrates the entire robbery planning process: scanning, data collection, and path calculation.
        /// </summary>
        /// <param name="targetMoneyAmount">The total amount of money the user wishes to "steal".</param>
        /// <returns>A string describing the plan (list of banks to rob) or an error message if the amount is unreachable.</returns>
        public async Task<string> ExecuteRobberyPlan(long targetMoneyAmount)
        {
            var availableBankNodes = await CollectNetworkData();

            if (availableBankNodes.Length == 0)
            {
                return "ER no other bank nodes were found";
            }

            return CalculateOptimalKnapsackPlan(availableBankNodes, targetMoneyAmount);
        }

        /// <summary>
        /// Calculates the optimal set of banks to rob using a sparse Dynamic Programming (Knapsack) approach.
        /// Unlike a greedy sort, this method guarantees finding the minimum number of affected clients 
        /// to reach the specific <paramref name="requiredMoneyGoal"/>.
        /// </summary>
        /// <param name="availableNodes">The list of valid bank nodes discovered in the network.</param>
        /// <param name="requiredMoneyGoal">The target amount of money to acquire.</param>
        /// <returns>A formatted protocol string starting with "RP", or an error if funds are insufficient.</returns>
        private string CalculateOptimalKnapsackPlan(BankNodeData[] availableNodes, long requiredMoneyGoal)
        {
            // Dictionary: Klíč = součet klientů (váha), Hodnota = (dosažené peníze, seznam IP adres bank)
            // Uses a sparse approach (Dictionary) to only track reachable client counts, avoiding large array allocations.
            var possibleRobberyStates = new Dictionary<int, (long TotalMoney, List<string> BankIpList)>();

            // Výchozí stav: 0 klientů a 0 peněz
            possibleRobberyStates[0] = (0, new List<string>());

            foreach (var currentNode in availableNodes)
            {
                // Vytvoříme kopii aktuálních stavů pro bezpečnou iteraci a přidání nové banky
                var existingCombinations = possibleRobberyStates.ToList();

                foreach (var state in existingCombinations)
                {
                    int combinedClientCount = state.Key + (int)currentNode.Clients;
                    long combinedMoneyAmount = state.Value.TotalMoney + currentNode.Amount;

                    // Pokud tato kombinace počtu klientů neexistuje, nebo nabízí více peněz než předchozí známá cesta
                    // Update state if we found a new reachable client count OR a richer path to an existing count
                    if (!possibleRobberyStates.ContainsKey(combinedClientCount) ||
                        possibleRobberyStates[combinedClientCount].TotalMoney < combinedMoneyAmount)
                    {
                        var updatedBankIpList = new List<string>(state.Value.BankIpList) { currentNode.Ip };
                        possibleRobberyStates[combinedClientCount] = (combinedMoneyAmount, updatedBankIpList);
                    }
                }
            }

            var bestFoundOption = possibleRobberyStates
                .Where(option => option.Value.TotalMoney >= requiredMoneyGoal)
                .OrderBy(option => option.Key)
                .ThenByDescending(option => option.Value.TotalMoney)
                .FirstOrDefault();

            if (bestFoundOption.Value.BankIpList == null)
            {
                return "RP Plan will fail: Insufficient funds in the network";
            }

            return $"RP K dosazeni {requiredMoneyGoal} je treba vyloupit banky {string.Join(", ", bestFoundOption.Value.BankIpList)} a bude poskozeno jen {bestFoundOption.Key} klientu.";
        }

        /// <summary>
        /// Scans the network range defined in the config and retrieves financial stats from every active node.
        /// Filters out the local host and unresponsive/invalid nodes.
        /// </summary>
        /// <returns>An array of valid <see cref="BankNodeData"/> objects containing IP, total wealth, and client counts.</returns>
        private async Task<BankNodeData[]> CollectNetworkData()
        {
            var discoveredActiveIps = await _networkScanner.ScanNetworkAsync();
            var validBankNodes = new List<BankNodeData>();

            foreach (string remoteIp in discoveredActiveIps)
            {
                if (remoteIp == ConfigLoader.Instance.IPAddress) continue;

                var remoteBankStats = GetRemoteBankStats(remoteIp);

                if (remoteBankStats.Amount > 0 && remoteBankStats.Clients >= 0)
                {
                    validBankNodes.Add(remoteBankStats);
                }
            }

            return validBankNodes.ToArray();
        }

        /// <summary>
        /// Connects to a specific remote IP using a ProxyClient to request its "BA" (Bank Amount) and "BN" (Bank Number of clients).
        /// </summary>
        /// <param name="ipAddress">The IP address of the target bank node.</param>
        /// <returns>A data object containing the parsed stats for that node, or -1 values on failure.</returns>
        private BankNodeData GetRemoteBankStats(string ipAddress)
        {
            try
            {
                ProxyClient proxyConnection = new ProxyClient(IPAddress.Parse(ipAddress));

                string amountResponse = proxyConnection.ForwardRequest("BA");
                string clientsResponse = proxyConnection.ForwardRequest("BN");

                return new BankNodeData(
                    ipAddress,
                    ExtractValueFromResponse(amountResponse, "BA"),
                    ExtractValueFromResponse(clientsResponse, "BN")
                );
            }
            catch
            {
                return new BankNodeData(ipAddress, -1, -1);
            }
        }

        /// <summary>
        /// Parses a protocol response string (e.g., "BA 1000" or "BN 5") to extract the numeric value.
        /// </summary>
        /// <param name="protocolResponse">The raw response string from the remote server.</param>
        /// <param name="expectedPrefix">The expected protocol prefix (e.g., "BA" or "BN").</param>
        /// <returns>The parsed long value, or -1 if the response is invalid or an error.</returns>
        private long ExtractValueFromResponse(string protocolResponse, string expectedPrefix)
        {
            if (string.IsNullOrEmpty(protocolResponse) || protocolResponse.StartsWith("ER")) return -1;

            try
            {
                string[] messageParts = protocolResponse.Split(' ');
                if (messageParts.Length == 2 && messageParts[0] == expectedPrefix)
                {
                    return long.Parse(messageParts[1]);
                }
            }
            catch { }

            return -1;
        }
    }
}