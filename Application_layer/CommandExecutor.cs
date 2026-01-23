using System;
using System.Collections.Generic;

public class CommandExecutor
{
    private Dictionary<string, IBankCommand> _commands = new()
    {
        ["BC"] = new BankCode(),
        ["AC"] = new AccountCreate(),
        ["AD"] = new AccountDeposit(),
        ["AW"] = new AccountWithdrawal(),
        ["AB"] = new AccountBalance(),
        ["AR"] = new AccountRemove(),
        ["BA"] = new BankTotalAmounth(),
        ["BN"] = new BankClients()
    };
}
