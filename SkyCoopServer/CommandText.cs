using System;

namespace SkyCoopServer
{
    public static class CommandText
    {
        public static bool TryParseLogin(string command, out string attempt)
        {
            attempt = "";
            if (string.IsNullOrEmpty(command))
            {
                return false;
            }

            string trimmed = command.TrimStart();
            int separator = IndexOfWhitespace(trimmed);
            string firstToken = separator < 0 ? trimmed : trimmed.Substring(0, separator);
            if (!string.Equals(firstToken, "login", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (separator >= 0)
            {
                attempt = trimmed.Substring(separator).TrimStart();
            }

            return true;
        }

        public static string NormalizeClientCommand(string command)
        {
            if (command == null)
            {
                return "";
            }

            if (TryParseLogin(command, out string attempt))
            {
                return "login " + attempt;
            }

            return command.ToLowerInvariant();
        }

        private static int IndexOfWhitespace(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i]))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
