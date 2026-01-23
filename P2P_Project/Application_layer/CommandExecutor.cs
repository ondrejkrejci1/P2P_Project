using static Commands;

namespace P2P_Project.Application_layer
{
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
}
