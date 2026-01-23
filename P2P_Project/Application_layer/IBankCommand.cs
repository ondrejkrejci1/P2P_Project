using System;
using System.Net.Sockets;

namespace P2P_Project.Application_layer
{
    public interface IBankCommand
    {
        void Execute(TcpClient client, string[] args);
    };
}