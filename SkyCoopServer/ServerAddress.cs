using System;
using System.Globalization;

namespace SkyCoopServer
{
    public static class ServerAddress
    {
        public static bool TryParse(string input, int defaultPort, out string host, out int port, out string key)
        {
            host = "";
            port = defaultPort;
            key = Packet.c_Key;

            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            input = input.Trim();
            if (input.Length == 0)
            {
                return false;
            }

            int atIndex = input.LastIndexOf('@');
            string address = input;
            if (atIndex >= 0)
            {
                string password = input.Substring(0, atIndex);
                address = input.Substring(atIndex + 1);
                if (password.Length > 0)
                {
                    key = password;
                }
            }

            if (address.Length == 0)
            {
                return false;
            }

            string parsedHost;
            string? portText = null;

            if (address[0] == '[')
            {
                int closing = address.IndexOf(']');
                if (closing < 0)
                {
                    return false;
                }

                parsedHost = address.Substring(1, closing - 1);
                string rest = address.Substring(closing + 1);
                if (rest.Length > 0)
                {
                    if (rest[0] != ':')
                    {
                        return false;
                    }
                    portText = rest.Substring(1);
                }
            }
            else
            {
                int colon = address.LastIndexOf(':');
                if (colon >= 0)
                {
                    parsedHost = address.Substring(0, colon);
                    portText = address.Substring(colon + 1);
                    if (parsedHost.IndexOf(':') >= 0)
                    {
                        return false;
                    }
                }
                else
                {
                    parsedHost = address;
                }
            }

            if (parsedHost.Length == 0 || ContainsWhitespace(parsedHost))
            {
                return false;
            }

            if (portText != null)
            {
                if (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedPort))
                {
                    return false;
                }
                if (parsedPort < 1 || parsedPort > 65535)
                {
                    return false;
                }
                port = parsedPort;
            }

            host = parsedHost;
            return true;
        }

        private static bool ContainsWhitespace(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i]))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
