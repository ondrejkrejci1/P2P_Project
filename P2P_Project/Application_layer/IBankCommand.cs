using System;

public interface IBankCommand
{
    void Execute(string placeHolderForConnectionReference, string[] args);
}
