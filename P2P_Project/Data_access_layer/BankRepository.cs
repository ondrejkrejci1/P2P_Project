using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace P2P_Project.Data_access_layer
{
    /// <summary>
    /// Manages the persistence and state of all bank accounts within the application.
    /// Implements the Singleton pattern to ensure a central point of access to data.
    /// Handles thread-safety using locks to prevent race conditions during concurrent transactions.
    /// </summary>
    public class BankRepository
    {
        /// <summary>
        /// The synchronization object used to lock critical sections and ensure thread safety.
        /// </summary>
        private static readonly object _lock = new object();

        private static readonly BankRepository _instance = new BankRepository();

        private const string FilePath = "accounts.json";
        private List<BankAccount> _accounts = new List<BankAccount>();
        private readonly Random _random = new Random();

        /// <summary>
        /// Private constructor to enforce the Singleton pattern.
        /// Automatically loads existing accounts from disk upon instantiation.
        /// </summary>
        private BankRepository()
        {
            LoadAccounts();
        }

        /// <summary>
        /// Gets the single, globally accessible instance of the <see cref="BankRepository"/>.
        /// </summary>
        public static BankRepository Instance => _instance;

        /// <summary>
        /// Generates a unique 5-digit account number and initializes a new account with a zero balance.
        /// </summary>
        /// <returns>The unique integer identifier for the newly created account.</returns>
        public int CreateAccount()
        {
            lock (_lock)
            {
                Log.Information("Requesting new account creation.");
                int newAccountNumber;

                do
                {
                    newAccountNumber = _random.Next(10000, 100000);
                }
                while (_accounts.Any(a => a.AccountNumber == newAccountNumber));

                var account = new BankAccount(newAccountNumber, 0);
                _accounts.Add(account);

                Log.Debug("Account {Acc} added to memory list.", newAccountNumber);
                SaveAccounts();

                return newAccountNumber;
            }
        }

        /// <summary>
        /// Reads account data from the 'accounts.json' file.
        /// If the file does not exist or is corrupted, initializes an empty list.
        /// </summary>
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
                    Log.Error($"Failed to load bank accounts: {ex.Message}");
                    throw new Exception($"Failed to load bank accounts: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Permanently removes an account from the system.
        /// </summary>
        /// <param name="accountNumber">The identifier of the account to delete.</param>
        /// <exception cref="KeyNotFoundException">Thrown if the account does not exist.</exception>
        public void DeleteAccount(int accountNumber)
        {
            lock (_lock)
            {
                Log.Information("Attempting to delete account {Acc}.", accountNumber);
                var account = _accounts.FirstOrDefault(a => a.AccountNumber == accountNumber);

                if (account == null)
                {
                    Log.Warning("Deletion failed: Account {Acc} not found.", accountNumber);
                    throw new KeyNotFoundException("Account not found.");
                }

                _accounts.Remove(account);
                Log.Debug("Account {Acc} removed from memory list.", accountNumber);
                SaveAccounts();
            }
        }

        /// <summary>
        /// Serializes the current state of all accounts to the JSON file.
        /// </summary>
        /// <exception cref="Exception">Thrown if file writing fails.</exception>
        public void SaveAccounts()
        {
            lock (_lock)
            {
                try
                {
                    string json = JsonSerializer.Serialize(_accounts, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(FilePath, json);
                    Log.Debug("Repo: Successfully saved {Count} accounts to disk", _accounts.Count);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Repo: Failed to save accounts to {Path}", FilePath);
                    throw new Exception($"Failed to save accounts: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Retrieves a thread-safe snapshot of all current bank accounts.
        /// Returns a copy of the list to prevent modification exceptions during iteration.
        /// </summary>
        /// <returns>A new List containing all <see cref="BankAccount"/> objects.</returns>
        public List<BankAccount> GetAllAccounts()
        {
            lock (_lock)
            {
                Log.Debug("Creating thread-safe snapshot of all accounts.");
                return _accounts.ToList();
            }
        }

        /// <summary>
        /// Retrieves a specific bank account by its number.
        /// </summary>
        /// <param name="accountNumber">The account identifier.</param>
        /// <returns>The <see cref="BankAccount"/> object.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the account is not found.</exception>
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

        /// <summary>
        /// Atomically deposits funds into an account.
        /// The read, modify, and save operations occur within a single lock to ensure data consistency.
        /// </summary>
        /// <param name="accountNumber">The target account identifier.</param>
        /// <param name="amount">The amount to add.</param>
        /// <exception cref="KeyNotFoundException">Thrown if the account does not exist.</exception>
        public void Deposit(int accountNumber, long amount)
        {
            lock (_lock)
            {
                Log.Information("Repo: Depositing {Amt} to Account {Acc}", amount, accountNumber);
                var account = _accounts.FirstOrDefault(a => a.AccountNumber == accountNumber);

                if (account == null)
                {
                    Log.Warning("Repo: Deposit failed, Account {Acc} not found", accountNumber);
                    throw new KeyNotFoundException($"Account {accountNumber} not found.");
                }

                account.Balance += amount;
                Log.Debug("Repo: New balance for {Acc} is {Bal}", accountNumber, account.Balance);
                SaveAccounts();
            }
        }

        /// <summary>
        /// Atomically withdraws funds from an account.
        /// Performs balance validation within the lock to prevent overdrawing during concurrent requests.
        /// </summary>
        /// <param name="accountNumber">The target account identifier.</param>
        /// <param name="amount">The amount to deduct.</param>
        /// <exception cref="KeyNotFoundException">Thrown if the account does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the account has insufficient funds.</exception>
        public void Withdraw(int accountNumber, long amount)
        {
            lock (_lock)
            {
                Log.Information("Processing Withdrawal: Account {Acc}, Amount {Amt}", accountNumber, amount);
                var account = _accounts.FirstOrDefault(a => a.AccountNumber == accountNumber);

                if (account == null)
                {
                    Log.Warning("Withdrawal failed: Account {Acc} not found.", accountNumber);
                    throw new KeyNotFoundException($"Account {accountNumber} not found.");
                }

                if (account.Balance < amount)
                {
                    Log.Warning("Withdrawal failed: Account {Acc} has insufficient funds ({Bal}).", accountNumber, account.Balance);
                    throw new InvalidOperationException("Insufficient funds.");
                }

                account.Balance -= amount;
                Log.Debug("New balance for {Acc}: {Bal}", accountNumber, account.Balance);
                SaveAccounts();
            }
        }
    }
}