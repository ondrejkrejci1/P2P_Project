using System;

namespace P2P_Project.Application_layer
{
    public interface IBankCommand
    {
        void Execute(string placeHolderForConnectionReference, string[] args);
    }
}