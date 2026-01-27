using System;

namespace P2P_Project.Data_access_layer
{
    public class BankAccount
    {
        private int _accountNumber;
        private long _balance;

        public BankAccount() { }
        public BankAccount(int accountNumber, long balance)
        {
            AccountNumber = accountNumber;
            Balance = balance;
        }

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
