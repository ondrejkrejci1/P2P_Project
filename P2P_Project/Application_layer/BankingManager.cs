using P2P_Project.Data_access_layer;
using System;
using System.Linq;

namespace P2P_Project.Application_layer
{
    /// <summary>
    /// Serves as the primary Business Logic Layer (BLL) entry point for banking operations.
    /// Acts as a Facade between the network commands and the data repository.
    /// Responsible for translating Repository exceptions into standard protocol error messages.
    /// </summary>
    public class BankingManager
    {
        private static BankingManager _instance = new BankingManager();

        /// <summary>
        /// Gets the single, globally accessible instance of the <see cref="BankingManager"/>.
        /// </summary>
        public static BankingManager Instance => _instance;

        private BankRepository _repository;
        private ConfigLoader _configLoader;

        /// <summary>
        /// Private constructor to enforce the Singleton pattern.
        /// Initializes references to the Data Access Layer components.
        /// </summary>
        private BankingManager()
        {
            _repository = BankRepository.Instance;
            _configLoader = ConfigLoader.Instance;
        }

        /// <summary>
        /// Retrieves the Bank Code (BC) protocol string consisting of the "BC" prefix and the server's IP.
        /// </summary>
        /// <returns>A formatted string: "BC {IPAddress}".</returns>
        public string GetBankCode() => $"BC {_configLoader.IPAddress}";

        /// <summary>
        /// Creates a new account via the repository and formats the success response.
        /// </summary>
        /// <returns>A formatted string: "AC {AccountNumber}/{IPAddress}".</returns>
        public string CreateAccount()
        {
            int accNum = _repository.CreateAccount();
            return $"AC {accNum}/{_configLoader.IPAddress}";
        }

        /// <summary>
        /// Attempts to deposit funds into a local account.
        /// Delegates atomicity to the <see cref="BankRepository"/>.
        /// </summary>
        /// <param name="accountNumber">The target account identifier.</param>
        /// <param name="ip">The IP address (unused for local processing but part of protocol).</param>
        /// <param name="amount">The amount to deposit.</param>
        /// <returns>
        /// "AD" on success.
        /// "ER AD Failed: Account not found" if the account does not exist.
        /// "ER Internal server error" for unexpected failures.
        /// </returns>
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

        /// <summary>
        /// Attempts to withdraw funds from a local account.
        /// Delegates balance checks and atomicity to the <see cref="BankRepository"/>.
        /// </summary>
        /// <param name="accountNumber">The target account identifier.</param>
        /// <param name="ip">The IP address (unused for local processing).</param>
        /// <param name="amount">The amount to withdraw.</param>
        /// <returns>
        /// "AW" on success.
        /// "ER AW Failed: Account not found" if the account does not exist.
        /// "ER AW Failed: Insufficient funds" if balance is too low.
        /// </returns>
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

        /// <summary>
        /// Retrieves the current balance for a local account.
        /// </summary>
        /// <param name="accountNumber">The account identifier.</param>
        /// <param name="ip">The IP address (unused for local processing).</param>
        /// <returns>
        /// "AB {Balance}" on success.
        /// "ER AB Failed: Account not found" on failure.
        /// </returns>
        public string GetBalance(int accountNumber, string ip)
        {
            try
            {
                var account = _repository.GetBankAccount(accountNumber);
                if (account == null) return "ER AB Failed: Account not found";

                return $"AB {account.Balance}";
            }
            catch (KeyNotFoundException)
            {
                return "ER AR Failed: Account not found";
            }
            catch
            {
                return "ER Internal server error";
            }
        }

        /// <summary>
        /// Removes a local account if the balance is zero.
        /// </summary>
        /// <param name="accountNumber">The account identifier.</param>
        /// <param name="ip">The IP address (unused for local processing).</param>
        /// <returns>
        /// "AR" on success.
        /// "ER AR Failed: Balance must be 0" if funds remain.
        /// "ER AR Failed: Account not found" if invalid ID.
        /// </returns>
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
            catch (KeyNotFoundException)
            {
                return "ER AR Failed: Account not found";
            }
            catch
            {
                return "ER Internal server error";
            }
        }

        /// <summary>
        /// Calculates the total sum of balances across all accounts on this node.
        /// </summary>
        /// <returns>A formatted string: "BA {TotalAmount}".</returns>
        public string GetTotalAmount()
        {
            var accounts = _repository.GetAllAccounts();
            long total = accounts.Sum(a => a.Balance);
            return $"BA {total}";
        }

        /// <summary>
        /// Counts the total number of accounts managed by this node.
        /// </summary>
        /// <returns>A formatted string: "BN {Count}".</returns>
        public string GetClientCount() => $"BN {_repository.GetAllAccounts().Count}";
    }
}