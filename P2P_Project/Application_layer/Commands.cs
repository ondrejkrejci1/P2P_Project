using P2P_Project.Data_access_layer;
using P2P_Project.Presentation_layer;
using System;
using System.Net.Sockets;

namespace P2P_Project.Application_layer
{
    public class Commands
    {
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

        public class BankCode : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                string result = BankingManager.Instance.GetBankCode();
                ConnectionManager.Instance.SendMessage(client, result);
            }
        }

        public class AccountCreate : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                string result = BankingManager.Instance.CreateAccount();
                ConnectionManager.Instance.SendMessage(client, result);
            }
        }

        public class AccountDeposit : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                if (args.Length < 3 ||
                    !TryParseAccountArg(args[1], out int accountNumber, out string ip) ||
                    !double.TryParse(args[2], out double amount))
                {
                    ConnectionManager.Instance.SendMessage(client, "ER AD Failed: Invalid arguments");
                    return;
                }

                string result = "ER AD Failed: Couldnt proccess the request";

                if (ip == ConfigLoader.Instance.IPAddress)
                {
                    result = BankingManager.Instance.Deposit(accountNumber, ip, amount);
                }
                else
                {
                    ProxyClient proxyClient = new ProxyClient(System.Net.IPAddress.Parse(ip));
                    string request = string.Join(" ",args);
                    result = proxyClient.ForwardRequest(request);
                }

                ConnectionManager.Instance.SendMessage(client, result);

            }
        }

        public class AccountWithdrawal : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                if (args.Length < 3 ||
                    !TryParseAccountArg(args[1], out int accountNumber, out string ip) ||
                    !double.TryParse(args[2], out double amount))
                {
                    ConnectionManager.Instance.SendMessage(client, "ER AW Failed: Invalid arguments");
                    return;
                }

                string result = "ER AW Failed: Couldnt proccess the request";

                if (ip == ConfigLoader.Instance.IPAddress)
                {
                    result = BankingManager.Instance.Withdraw(accountNumber, ip, amount);

                }
                else
                {
                    ProxyClient proxyClient = new ProxyClient(System.Net.IPAddress.Parse(ip));
                    string request = string.Join(" ", args);
                    result = proxyClient.ForwardRequest(request);
                }

                ConnectionManager.Instance.SendMessage(client, result);
            }
        }

        public class AccountBalance : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                if (args.Length < 2 ||
                    !TryParseAccountArg(args[1], out int accountNumber, out string ip))
                {
                    ConnectionManager.Instance.SendMessage(client, "ER AB Failed: Invalid arguments");
                    return;
                }

                string result = "ER AB Failed: Couldnt proccess the request";

                if (ip == ConfigLoader.Instance.IPAddress)
                {
                    result = BankingManager.Instance.GetBalance(accountNumber, ip);
                }
                else
                {
                    ProxyClient proxyClient = new ProxyClient(System.Net.IPAddress.Parse(ip));
                    string request = string.Join(" ", args);
                    result = proxyClient.ForwardRequest(request);
                }

                ConnectionManager.Instance.SendMessage(client, result);
            }
        }

        public class AccountRemove : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                if (args.Length < 2 ||
                    !TryParseAccountArg(args[1], out int accountNumber, out string ip))
                {
                    ConnectionManager.Instance.SendMessage(client, "ER AR Failed: Invalid arguments");
                    return;
                }

                string result = "ER AR Failed: Couldnt proccess the request";

                if (ip == ConfigLoader.Instance.IPAddress)
                {
                    result = BankingManager.Instance.RemoveAccount(accountNumber, ip);
                }
                else
                {
                    ProxyClient proxyClient = new ProxyClient(System.Net.IPAddress.Parse(ip));
                    string request = string.Join(" ", args);
                    result = proxyClient.ForwardRequest(request);
                }

                ConnectionManager.Instance.SendMessage(client, result);
            }
        }

        public class BankTotalAmounth : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                string result = BankingManager.Instance.GetTotalAmount();
                ConnectionManager.Instance.SendMessage(client, result);
            }
        }

        public class BankClients : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                string result = BankingManager.Instance.GetClientCount();
                ConnectionManager.Instance.SendMessage(client, result);
            }
        }


        private bool DiferentBankCommand(string command)
        {
            string localIP = ConfigLoader.Instance.IPAddress;

            return false;
        }
    }
}