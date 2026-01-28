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
    /// Scans the P2P network, gathers intelligence on bank nodes, and calculates optimal strategies.
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
        /// </summary>
        private RobberyPlanner()
        {
            _networkScanner = new NetworkScanner();
        }

        /// <summary>
        /// Orchestrates the robbery planning process including scanning, data collection, and path calculation.
        /// </summary>
        /// <param name="targetMoneyAmount">The total amount of money to "steal".</param>
        /// <returns>A description of the plan or an error message if unreachable.</returns>
        public async Task<string> ExecuteRobberyPlan(long targetMoneyAmount)
        {
            Log.Information("Starting robbery plan calculation for target amount: {Amount}", targetMoneyAmount);

            var availableBankNodes = await CollectNetworkData();

            if (availableBankNodes.Length == 0)
            {
                Log.Warning("Robbery plan aborted: No other bank nodes were found on the network.");
                return "ER no other bank nodes were found";
            }

            string result = CalculateOptimalKnapsackPlan(availableBankNodes, targetMoneyAmount);
            Log.Information("Robbery plan execution completed.");
            return result;
        }

        /// <summary>
        /// Calculates the optimal set of banks to rob using a sparse Dynamic Programming approach.
        /// Minimizes the number of affected clients to reach the required money goal.
        /// </summary>
        /// <param name="availableNodes">List of discovered bank nodes.</param>
        /// <param name="requiredMoneyGoal">The target money amount.</param>
        /// <returns>A formatted protocol string or error message.</returns>
        private string CalculateOptimalKnapsackPlan(BankNodeData[] availableNodes, long requiredMoneyGoal)
        {
            Log.Debug("Calculating optimal plan using {Count} available nodes.", availableNodes.Length);

            var possibleRobberyStates = new Dictionary<int, (long TotalMoney, List<string> BankIpList)>();
            possibleRobberyStates[0] = (0, new List<string>());

            foreach (var currentNode in availableNodes)
            {
                var existingCombinations = possibleRobberyStates.ToList();

                foreach (var state in existingCombinations)
                {
                    int combinedClientCount = state.Key + (int)currentNode.Clients;
                    long combinedMoneyAmount = state.Value.TotalMoney + currentNode.Amount;

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
                Log.Error("Robbery plan failed: Insufficient funds in the network for goal {Goal}", requiredMoneyGoal);
                return "RP Plan will fail: Insufficient funds in the network";
            }

            return $"RP K dosazeni {requiredMoneyGoal} je treba vyloupit banky {string.Join(", ", bestFoundOption.Value.BankIpList)} a bude poskozeno jen {bestFoundOption.Key} klientu.";
        }

        /// <summary>
        /// Scans the network and retrieves financial stats from active nodes.
        /// </summary>
        /// <returns>An array of valid BankNodeData objects.</returns>
        private async Task<BankNodeData[]> CollectNetworkData()
        {
            var discoveredActiveIps = await _networkScanner.ScanNetworkAsync();
            var validBankNodes = new List<BankNodeData>();

            Log.Debug("Network scan discovered {Count} active IPs.", discoveredActiveIps.Count);

            foreach (string remoteIp in discoveredActiveIps)
            {
                if (remoteIp == ConfigLoader.Instance.IPAddress) continue;

                var remoteBankStats = GetRemoteBankStats(remoteIp);

                if (remoteBankStats.Amount > 0 && remoteBankStats.Clients >= 0)
                {
                    validBankNodes.Add(remoteBankStats);
                }
                else
                {
                    Log.Warning("Skipping node {Ip}: Invalid stats (Amount: {Amount}, Clients: {Clients})", remoteIp, remoteBankStats.Amount, remoteBankStats.Clients);
                }
            }

            return validBankNodes.ToArray();
        }

        /// <summary>
        /// Connects to a remote IP to request its "BA" and "BN" statistics.
        /// </summary>
        /// <param name="ipAddress">Target IP address.</param>
        /// <returns>Parsed stats or -1 values on failure.</returns>
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
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve bank stats from {Ip}", ipAddress);
                return new BankNodeData(ipAddress, -1, -1);
            }
        }

        /// <summary>
        /// Parses protocol response strings to extract numeric values.
        /// </summary>
        /// <param name="response">Raw response string.</param>
        /// <param name="expectedPrefix">Expected protocol prefix (e.g., "BA").</param>
        /// <returns>The parsed value or -1 if invalid.</returns>
        private long ExtractValueFromResponse(string response, string expectedPrefix)
        {
            if (string.IsNullOrEmpty(response) || response.StartsWith("ER")) return -1;

            try
            {
                string[] messageParts = response.Split(' ');
                if (messageParts.Length == 2 && messageParts[0] == expectedPrefix)
                {
                    return long.Parse(messageParts[1]);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error parsing protocol response: {Response}", response);
            }

            return -1;
        }
    }
}