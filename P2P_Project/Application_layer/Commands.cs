using P2P_Project.Data_access_layer;
using P2P_Project.Presentation_layer;
using Serilog;
using System;
using System.Net;
using System.Net.Sockets;

namespace P2P_Project.Application_layer
{
    /// <summary>
    /// Container for all bank protocol command implementations.
    /// Acts as a namespace for the specific logic required to execute operations like deposits, withdrawals, and robbery plans.
    /// </summary>
    public class Commands
    {
        /// <summary>
        /// Validates and parses the standard account argument format "AccountNumber/IP".
        /// </summary>
        /// <param name="arg">The raw string argument (e.g., "12345/192.168.1.1").</param>
        /// <param name="accountNumber">Output parameter for the parsed account integer.</param>
        /// <param name="ip">Output parameter for the parsed IP address string.</param>
        /// <returns>True if the argument was successfully parsed; otherwise, false.</returns>
        private static bool TryParseAccountArg(string arg, out int accountNumber, out string ip)
        {
            accountNumber = 0;
            ip = null;

            if (string.IsNullOrWhiteSpace(arg)) return false;

            string[] parts = arg.Split('/');
            if (parts.Length != 2) return false;

            if (!int.TryParse(parts[0], out accountNumber)) return false;

            ip = parts[1];
            return true;
        }

        /// <summary>
        /// Command: BC
        /// Retrieves the unique Bank Code (BC) identifier for this server instance.
        /// </summary>
        public class BankCode : IBankCommand
        {
            /// <summary>
            /// Executes the Bank Code retrieval.
            /// </summary>
            public void Execute(TcpClient client, string[] args)
            {
                Log.Information("Processing BC (Bank Code) request.");
                string result = BankingManager.Instance.GetBankCode();
                Log.Debug("BC Result: {Result}", result);
                ConnectionManager.Instance.SendMessage(client, result);
            }
        }

        /// <summary>
        /// Command: AC
        /// Creates a new bank account on this server.
        /// </summary>
        public class AccountCreate : IBankCommand
        {
            /// <summary>
            /// Executes the creation of a new account and returns the new Account Number.
            /// </summary>
            public void Execute(TcpClient client, string[] args)
            {
                Log.Information("Processing AC (Account Create) request.");
                string result = BankingManager.Instance.CreateAccount();
                Log.Debug("Account created: {Result}", result);
                ConnectionManager.Instance.SendMessage(client, result);
            }
        }

        /// <summary>
        /// Command: AD
        /// Deposits funds into a specific account. 
        /// Supports forwarding the request to a proxy peer if the account resides on a different node.
        /// </summary>
        public class AccountDeposit : IBankCommand
        {
            /// <summary>
            /// Validates arguments and executes a deposit locally or forwards it via proxy.
            /// </summary>
            public async void Execute(TcpClient client, string[] args)
            {
                if (args.Length < 3 ||
                    !TryParseAccountArg(args[1], out int accountNumber, out string ip) ||
                    !long.TryParse(args[2], out long amount) || amount <= 0)
                {
                    Log.Warning("AD Command rejected: Invalid arguments.");
                    ConnectionManager.Instance.SendMessage(client, "ER AD Failed: Invalid arguments");
                    return;
                }

                string result = "ER AD Failed: Could not proccess request";

                if (ip == ConfigLoader.Instance.IPAddress)
                {
                    Log.Information("Executing local deposit for Account {Acc}.", accountNumber);
                    result = BankingManager.Instance.Deposit(accountNumber, ip, amount);
                }
                else
                {
                    Log.Information("Forwarding deposit for Account {Acc} to remote node {IP}.", accountNumber, ip);
                    ProxyClient proxyClient = await ProxyClient.CreateClient(IPAddress.Parse(ip));
                    string request = string.Join(' ', args);
                    result = proxyClient.ForwardRequest(request);
                }

                ConnectionManager.Instance.SendMessage(client, result);
            }
        }

        /// <summary>
        /// Command: AW
        /// Withdraws funds from a specific account.
        /// Supports forwarding the request to a proxy peer if the account resides on a different node.
        /// </summary>
        public class AccountWithdrawal : IBankCommand
        {
            /// <summary>
            /// Validates arguments and executes a withdrawal locally or forwards it via proxy.
            /// </summary>
            public async void Execute(TcpClient client, string[] args)
            {
                if (args.Length < 3 ||
                    !TryParseAccountArg(args[1], out int accountNumber, out string ip) ||
                    !long.TryParse(args[2], out long amount) || amount <= 0)
                {
                    Log.Warning("AW Command rejected: Invalid arguments.");
                    ConnectionManager.Instance.SendMessage(client, "ER AW Failed: Invalid arguments");
                    return;
                }

                string result = "ER AW Failed: Could not proccess request";

                if (ip == ConfigLoader.Instance.IPAddress)
                {
                    Log.Information("Executing local withdrawal for Account {Acc}.", accountNumber);
                    result = BankingManager.Instance.Withdraw(accountNumber, ip, amount);
                }
                else
                {
                    Log.Information("Forwarding withdrawal for Account {Acc} to remote node {IP}.", accountNumber, ip);
                    ProxyClient proxyClient = await ProxyClient.CreateClient(IPAddress.Parse(ip));
                    string request = string.Join(' ', args);
                    result = proxyClient.ForwardRequest(request);
                }

                ConnectionManager.Instance.SendMessage(client, result);
            }
        }

        /// <summary>
        /// Command: AB
        /// Retrieves the current balance of a specific account.
        /// Supports forwarding the request if the account is not local.
        /// </summary>
        public class AccountBalance : IBankCommand
        {
            /// <summary>
            /// Validates arguments and retrieves the balance locally or via proxy.
            /// </summary>
            public async void Execute(TcpClient client, string[] args)
            {
                if (args.Length < 2 ||
                    !TryParseAccountArg(args[1], out int accountNumber, out string ip))
                {
                    Log.Warning("AB Command rejected: Invalid arguments.");
                    ConnectionManager.Instance.SendMessage(client, "ER AB Failed: Invalid arguments");
                    return;
                }

                string result = "ER AB Failed: Could not proccess request";

                if (ip == ConfigLoader.Instance.IPAddress)
                {
                    Log.Information("Checking local balance for Account {Acc}.", accountNumber);
                    result = BankingManager.Instance.GetBalance(accountNumber, ip);
                }
                else
                {
                    Log.Information("Forwarding balance check for Account {Acc} to {IP}.", accountNumber, ip);
                    ProxyClient proxyClient = await ProxyClient.CreateClient(IPAddress.Parse(ip));
                    string request = string.Join(' ', args);
                    result = proxyClient.ForwardRequest(request);
                }

                ConnectionManager.Instance.SendMessage(client, result);
            }
        }

        /// <summary>
        /// Command: AR
        /// Removes (closes) a specific bank account.
        /// The account must have a balance of 0 to be removed.
        /// </summary>
        public class AccountRemove : IBankCommand
        {
            /// <summary>
            /// Validates arguments and removes the account locally or requests removal via proxy.
            /// </summary>
            public async void Execute(TcpClient client, string[] args)
            {
                if (args.Length < 2 ||
                    !TryParseAccountArg(args[1], out int accountNumber, out string ip))
                {
                    Log.Warning("AR Command rejected: Invalid arguments.");
                    ConnectionManager.Instance.SendMessage(client, "ER AR Failed: Invalid arguments");
                    return;
                }

                string result = "ER AB Failed: Could not proccess request";

                if (ip == ConfigLoader.Instance.IPAddress)
                {
                    Log.Information("Removing local Account {Acc}.", accountNumber);
                    result = BankingManager.Instance.RemoveAccount(accountNumber, ip);
                }
                else
                {
                    Log.Information("Forwarding removal request for Account {Acc} to {IP}.", accountNumber, ip);
                    ProxyClient proxyClient = await ProxyClient.CreateClient(IPAddress.Parse(ip));
                    string request = string.Join(' ', args);
                    result = proxyClient.ForwardRequest(request);
                }

                ConnectionManager.Instance.SendMessage(client, result);
            }
        }

        /// <summary>
        /// Command: BA
        /// Retrieves the total amount of funds held by this bank node.
        /// </summary>
        public class BankTotalAmounth : IBankCommand
        {
            /// <summary>
            /// Executes the total amount calculation.
            /// </summary>
            public void Execute(TcpClient client, string[] args)
            {
                Log.Information("Processing BA (Total Amount) request.");
                string result = BankingManager.Instance.GetTotalAmount();
                ConnectionManager.Instance.SendMessage(client, result);
            }
        }

        /// <summary>
        /// Command: BN
        /// Retrieves the total number of clients (accounts) managed by this bank node.
        /// </summary>
        public class BankClients : IBankCommand
        {
            /// <summary>
            /// Executes the client count retrieval.
            /// </summary>
            public void Execute(TcpClient client, string[] args)
            {
                Log.Information("Processing BN (Client Count) request.");
                string result = BankingManager.Instance.GetClientCount();
                ConnectionManager.Instance.SendMessage(client, result);
            }
        }

        /// <summary>
        /// Command: RP
        /// Initiates the Bank Robbery protocol.
        /// Scans the network to find the optimal path to steal a specific target amount.
        /// </summary>
        public class BankRobbery : IBankCommand
        {
            /// <summary>
            /// Asynchronously executes the robbery planning logic.
            /// </summary>
            public async void Execute(TcpClient client, string[] args)
            {
                if (args.Length < 2 || !long.TryParse(args[1], out long targetAmount) || targetAmount <= 0)
                {
                    Log.Warning("RP Command rejected: Invalid target amount.");
                    ConnectionManager.Instance.SendMessage(client, "ER RP Failed: Invalid target amount");
                    return;
                }

                Log.Information("Starting Robbery Planning for target amount: {Amt}", targetAmount);
                try
                {
                    string result = await RobberyPlanner.Instance.ExecuteRobberyPlan(targetAmount);
                    Log.Information("Robbery plan generated.");
                    ConnectionManager.Instance.SendMessage(client, result);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "RP Command execution failed.");
                    ConnectionManager.Instance.SendMessage(client, "ER RP Internal Error");
                }
            }
        }
    }
}