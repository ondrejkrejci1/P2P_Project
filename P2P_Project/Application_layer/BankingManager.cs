using P2P_Project.Data_access_layer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using P2P_Project.Data_access_layer;
using System.Linq;

namespace P2P_Project.Application_layer
{
    public sealed class BankingManager
    {
        private static BankingManager _instance = new BankingManager();
        public static BankingManager Instance => _instance;

        private BankRepository _repository;
        private ConfigLoader _configLoader;

        private BankingManager()
        {
            _repository = BankRepository.Instance;
            _configLoader = ConfigLoader.Instance;
        }

        public string GetBankCode()
        {
            return $"BC {_configLoader.IPAddress}";
        }
        public string CreateAccount()
        {
            int accNum = _repository.CreateAccount();
            return $"AC {accNum}/{_configLoader.IPAddress}";
        }

        public string Deposit(int accNum, string ip, double amount)
        {
            var account = _repository.GetBankAccount(accNum);
            if (account == null) return "ER AD Failed: Account not found";

            account.Balance += amount;
            _repository.SaveAccounts();
            return "AD";
        }

        public string Withdraw(int accNum, string ip, double amount)
        {
            var account = _repository.GetBankAccount(accNum);
            if (account == null) return "ER AW Failed: Account not found";

            if (account.Balance < amount) return "ER AW: failed insufficient funds";

            account.Balance -= amount;
            _repository.SaveAccounts();
            return "AW";
        }

        public string GetBalance(int accNum, string ip)
        {
            var account = _repository.GetBankAccount(accNum);
            if (account == null) return "ER AB failed: Account not found";

            return $"AB {account.Balance}";
        }

        public string RemoveAccount(int accountNumer, string ip)
        {

            var account = _repository.GetBankAccount(accountNumer);
            if (account == null) return "ER AR Failed: Account not found";

            if (account.Balance > 0) return "ER AR Failed: Balance must be 0";

            _repository.DeleteAccount(accountNumer);
            return "AR";
        }

        public string GetTotalAmount()
        {
            var accounts = _repository.GetAllAccounts();
            long total = (long)accounts.Sum(a => a.Balance);
            return $"BA {total}";
        }

        public string GetClientCount()
        {
            var accounts = _repository.GetAllAccounts();
            return $"BN {_repository.GetAllAccounts().Count}";
        }

    }
}
