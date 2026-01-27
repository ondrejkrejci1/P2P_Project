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
                    !long.TryParse(args[2], out long amount))
                {
                    ConnectionManager.Instance.SendMessage(client, "ER AD Failed: Invalid arguments");
                    return;
                }

                string result = BankingManager.Instance.Deposit(accountNumber, ip, amount);
                ConnectionManager.Instance.SendMessage(client, result);
            }
        }

        public class AccountWithdrawal : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                if (args.Length < 3 ||
                    !TryParseAccountArg(args[1], out int accountNumber, out string ip) ||
                    !long.TryParse(args[2], out long amount))
                {
                    ConnectionManager.Instance.SendMessage(client, "ER AW Failed: Invalid arguments");
                    return;
                }

                string result = BankingManager.Instance.Withdraw(accountNumber, ip, amount);
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

                string result = BankingManager.Instance.GetBalance(accountNumber, ip);
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

                string result = BankingManager.Instance.RemoveAccount(accountNumber, ip);
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

        public class RobberyPlan : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {

            }
        }
    }
}