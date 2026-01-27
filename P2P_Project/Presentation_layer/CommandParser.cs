using Serilog;

namespace P2P_Project.Presentation_layer
{
    public class CommandParser
    {
        /// <summary>
        /// Parses input by trimming spaces, then splitting by space character.
        /// </summary>
        /// <param name="input"></param>
        /// <returns>An array of strings where the first element is the command and subsequent elements are arguments; returns null if input is empty.</returns>
        public string[] Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            Log.Debug($"Parsing client input: {input}");

            string[] parsedCommand = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            parsedCommand[0] = parsedCommand[0].ToUpper();

            Log.Debug($"Parsed input: [{string.Join(", ", parsedCommand)}]");
            return parsedCommand;
        }

    }
}
