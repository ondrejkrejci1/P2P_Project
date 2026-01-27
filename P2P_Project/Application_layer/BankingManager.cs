using P2P_Project.Data_access_layer;
using System;
using System.Linq;

namespace P2P_Project.Application_layer
{
    public class BankingManager
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

        public string GetBankCode() => $"BC {_configLoader.IPAddress}";

        public string CreateAccount()
        {
            int accNum = _repository.CreateAccount();
            return $"AC {accNum}/{_configLoader.IPAddress}";
        }

        public string Deposit(int accountNumber, string ip, long amount)
        {
            try
            {
                _repository.Deposit(accountNumber, amount);
                return "AD";
            }
            catch (KeyNotFoundException)
            {
                return "ER AD Failed: Account not found";
            }
            catch (Exception)
            {
                return "ER Internal server error";
            }
        }

        public string Withdraw(int accountNumber, string ip, long amount)
        {
            try
            {
                _repository.Withdraw(accountNumber, amount);
                return "AW";
            }
            catch (KeyNotFoundException)
            {
                return "ER AW Failed: Account not found";
            }
            catch (InvalidOperationException ex)
            {
                return $"ER AW Failed: {ex.Message}";
            }
            catch (Exception)
            {
                return "ER Internal server error";
            }
        }

        public string GetBalance(int accountNumber, string ip)
        {
            try
            {
                var account = _repository.GetBankAccount(accountNumber);
                if (account == null) return "ER AB Failed: Account not found";

                return $"AB {account.Balance}";
            }
            catch
            {
                return "ER Internal server error";
            }
        }

        public string RemoveAccount(int accountNumber, string ip)
        {
            try
            {
                var account = _repository.GetBankAccount(accountNumber);
                if (account == null) return "ER AR Failed: Account not found";

                if (account.Balance > 0) return "ER AR Failed: Balance must be 0";

                _repository.DeleteAccount(accountNumber);
                return "AR";
            }
            catch
            {
                return "ER Internal server error";
            }
        }

        public string GetTotalAmount()
        {
            var accounts = _repository.GetAllAccounts();
            long total = accounts.Sum(a => a.Balance);
            return $"BA {total}";
        }

        public string GetClientCount() => $"BN {_repository.GetAllAccounts().Count}";
    }
}