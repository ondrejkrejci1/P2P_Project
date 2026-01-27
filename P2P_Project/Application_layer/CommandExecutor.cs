using Serilog;
using System.Net.Sockets;
using System.Text;
using static P2P_Project.Application_layer.Commands;

namespace P2P_Project.Application_layer
{
    /// <summary>
    /// Coordinates the execution of financial commands received over the network.
    /// This class acts as a router, mapping protocol command strings to their corresponding logic.
    /// </summary>
    public class CommandExecutor
    {
        /// <summary>
        /// Delegate defining the signature for bank command execution methods.
        /// </summary>
        /// <param name="client">The TCP client that initiated the request.</param>
        /// <param name="args">The parsed command arguments, where the first element is the command key.</param>
        private delegate void BankCommandAction(TcpClient client, string[] args);

        /// <summary>
        /// Registry of available protocol commands and their associated execution logic.
        /// </summary>
        private readonly Dictionary<string, BankCommandAction> _commands = new()
        {
            ["BC"] = new BankCode().Execute,
            ["AC"] = new AccountCreate().Execute,
            ["AD"] = new AccountDeposit().Execute,
            ["AW"] = new AccountWithdrawal().Execute,
            ["AB"] = new AccountBalance().Execute,
            ["AR"] = new AccountRemove().Execute,
            ["BA"] = new BankTotalAmounth().Execute,
            ["BN"] = new BankClients().Execute,
            ["RP"] = new BankRobbery().Execute
        };

        /// <summary>
        /// Attempts to execute a command based on the provided input array.
        /// Handles unknown commands and execution failures by notifying the client.
        /// </summary>
        /// <param name="client">The <see cref="TcpClient"/> connection to respond to.</param>
        /// <param name="parsedInput">The array of strings containing the command key and its parameters.</param>
        public void ExecuteCommand(TcpClient client, string[] parsedInput)
        {
            Log.Debug($"Executing {string.Join(", ", parsedInput)}");
            if (parsedInput == null || parsedInput.Length == 0) return;

            try
            {
                _commands[parsedInput[0]](client, parsedInput);
            }
            catch (KeyNotFoundException)
            {
                Log.Debug($"Command not found");
                SendMessage(client, $"ER command not found: {parsedInput[0]}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to execute command: {ex.Message}");
                SendMessage(client, $"ER Internal server error");
            }
        }

        /// <summary>
        /// Transmits a string message back to the client over the network stream.
        /// Appends a newline to satisfy protocol requirements.
        /// </summary>
        /// <param name="client">The <see cref="TcpClient"/> whose stream will be used for writing.</param>
        /// <param name="message">The content string to send.</param>
        private void SendMessage(TcpClient client, string message)
        {
            Log.Debug($"Sending message to client: {message}");
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message + Environment.NewLine);
                client.GetStream().Write(data, 0, data.Length);
            }
            catch
            {
            }
        }
    }
}