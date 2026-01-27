using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2P_Project.Application_layer
{
    public class BankNodeData
    {
        public string Ip { get; set; }
        public long Amount { get; set; }
        public long Clients { get; set; }
        public double AverageWealth { get; set; }

        public BankNodeData(string ip, long amount, long clients)
        {
            Ip = ip;
            Amount = amount;
            Clients = clients;
            AverageWealth = clients == 0 ? amount : (double)amount / clients;
        }
    }
}
