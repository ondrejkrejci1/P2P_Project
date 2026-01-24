using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace P2P_Project.Data_access_layer
{
    public class BankRepository
    {
        private static readonly object _lock = new object();
        private static readonly BankRepository _instance = new BankRepository();

        private const string FilePath = "accounts.json";
        private List<BankAccount> _accounts = new List<BankAccount>();
        private readonly Random _random = new Random();

        private BankRepository()
        {
            LoadAccounts();
        }

        public static BankRepository Instance => _instance;

        public int CreateAccount()
        {
            lock (_lock)
            {
                int newAccountNumber;
                do
                {
                    newAccountNumber = _random.Next(10000, 100000);
                }
                while (_accounts.Any(a => a.AccountNumber == newAccountNumber));

                var account = new BankAccount(newAccountNumber, 0.0f);
                _accounts.Add(account);
                SaveAccounts();

                return newAccountNumber;
            }
        }

        private void LoadAccounts()
        {
            lock (_lock)
            {
                if (!File.Exists(FilePath))
                {
                    _accounts = new List<BankAccount>();
                    return;
                }

                try
                {
                    string json = File.ReadAllText(FilePath);

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        _accounts = new List<BankAccount>();
                        return;
                    }

                    _accounts = JsonSerializer.Deserialize<List<BankAccount>>(json) ?? new List<BankAccount>();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to load bank accounts: {ex.Message}");
                }
            }
        }

        public void DeleteAccount(int accountNumber)
        {
            lock (_lock)
            {
                var account = _accounts.FirstOrDefault(a => a.AccountNumber == accountNumber);
                if (account == null) throw new KeyNotFoundException("Account not found.");

                _accounts.Remove(account);
                SaveAccounts();
            }
        }

        public void SaveAccounts()
        {
            lock (_lock)
            {
                try
                {
                    string json = JsonSerializer.Serialize(_accounts, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(FilePath, json);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to save accounts: {ex.Message}");
                }
            }
           
        }

        public List<BankAccount> GetAllAccounts()
        {
            lock (_lock)
            {
                return _accounts;
            }
        }

        public BankAccount GetBankAccount(int accountNumber)
        {
            lock (_lock)
            {
                var account = _accounts.FirstOrDefault(a => a.AccountNumber == accountNumber);

                if (account == null)
                {
                    throw new KeyNotFoundException($"Account {accountNumber} not found.");
                }

                return account;
            }
        }
    }
}