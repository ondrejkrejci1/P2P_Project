using static Commands;

namespace P2P_Project.Application_layer
{
    public class CommandExecutor
    {
        private delegate void BankCommandAction(TcpClient client, string[] args);

        private readonly Dictionary<string, BankCommandAction> _commands = new()
        {
            ["BC"] = new BankCode().Execute,
            ["AC"] = new AccountCreate().Execute,
            ["AD"] = new AccountDeposit().Execute,
            ["AW"] = new AccountWithdrawal().Execute,
            ["AB"] = new AccountBalance().Execute,
            ["AR"] = new AccountRemove().Execute,
            ["BA"] = new BankTotalAmounth().Execute,
            ["BN"] = new BankClients().Execute
        };

        public void ExecuteCommand(TcpClient client, string[] parsedInput)
        {
            if (parsedInput == null || parsedInput.Length == 0) return;

            try
            {
                _commands[parsedInput[0]](client, parsedInput);
            }
            catch (KeyNotFoundException)
            {
                SendMessage(client, $"ER command not found: {parsedInput[0]}");
            }
            catch (Exception)
            {
                SendMessage(client, $"ER Internal server error");
            }
        }

        private void SendMessage(TcpClient client, string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message + Environment.NewLine);
                client.GetStream().Write(data, 0, data.Length);
            }
            catch { }
        }
    }
}
