using System;

namespace P2P_Project.Data_access_layer
{
    /// <summary>
    /// Represents a single bank account within the system.
    /// Encapsulates account identity and financial state with validation logic.
    /// </summary>
    public class BankAccount
    {
        private int _accountNumber;
        private long _balance;

        /// <summary>
        /// Initializes a new instance of the <see cref="BankAccount"/> class.
        /// Required for serialization purposes.
        /// </summary>
        public BankAccount() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BankAccount"/> class with a specified account number and starting balance.
        /// </summary>
        /// <param name="accountNumber">The unique 5-digit identifier for the account.</param>
        /// <param name="balance">The initial funds available in the account.</param>
        public BankAccount(int accountNumber, long balance)
        {
            AccountNumber = accountNumber;
            Balance = balance;
        }

        /// <summary>
        /// Gets or sets the unique account identifier.
        /// The value must be a 5-digit integer between 10,000 and 99,999.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the value is outside the valid range.</exception>
        public int AccountNumber
        {
            get => _accountNumber;
            set
            {
                if (value < 10000 || value > 99999)
                {
                    throw new ArgumentException("Account number must be between 10000 and 99999.");
                }
                _accountNumber = value;
            }
        }

        /// <summary>
        /// Gets or sets the current funds available in the account.
        /// The balance cannot be negative.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when a negative value is assigned.</exception>
        public long Balance
        {
            get => _balance;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Balance cannot be negative.");
                }
                _balance = value;
            }
        }

    }
}