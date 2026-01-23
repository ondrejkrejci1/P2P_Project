using System;
using System.Net.Sockets;

namespace P2P_Project.Application_layer
{
    public class Commands
    {
        public class BankCode : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                throw new NotImplementedException();
            }
        }

        public class AccountCreate : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                throw new NotImplementedException();
            }
        }

        public class AccountDeposit : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                throw new NotImplementedException();
            }
        }

        public class AccountWithdrawal : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                throw new NotImplementedException();
            }
        }

        public class AccountBalance : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                throw new NotImplementedException();
            }
        }

        public class AccountRemove : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                throw new NotImplementedException();
            }
        }

        public class BankTotalAmounth : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                throw new NotImplementedException();
            }
        }

        public class BankClients : IBankCommand
        {
            public void Execute(TcpClient client, string[] args)
            {
                throw new NotImplementedException();
            }
        }
    }
}
