using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace SkyCoopServer
{
    public static class AdminAuth
    {
        public static readonly HashSet<string> PlayerCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "squad",
            "join"
        };

        private static readonly TimeSpan s_ThrottleWindow = TimeSpan.FromSeconds(60);
        private const int c_MaxFailures = 5;

        public static bool IsLoopback(IPAddress address)
        {
            if (address == null)
            {
                return false;
            }

            if (IPAddress.IsLoopback(address))
            {
                return true;
            }

            return address.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(address.MapToIPv4());
        }

        public static bool PasswordMatches(string configured, string attempt)
        {
            if (string.IsNullOrEmpty(configured) || attempt == null)
            {
                return false;
            }

            byte[] configuredBytes = Encoding.UTF8.GetBytes(configured);
            byte[] attemptBytes = Encoding.UTF8.GetBytes(attempt);
            if (configuredBytes.Length != attemptBytes.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(configuredBytes, attemptBytes);
        }

        public static bool IsAllowed(DataStr.PlayerData player, string command)
        {
            if (player == null || string.IsNullOrEmpty(command))
            {
                return false;
            }

            if (player.m_IsOperator)
            {
                return true;
            }

            string[] tokens = command.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            return tokens.Length > 0 && PlayerCommands.Contains(tokens[0]);
        }

        public sealed class LoginThrottle
        {
            private sealed class FailureWindow
            {
                public DateTime FirstFailureUtc;
                public int Count;
            }

            private readonly Dictionary<IPAddress, FailureWindow> m_Failures = new Dictionary<IPAddress, FailureWindow>();
            private readonly object m_Lock = new object();

            public bool RecordFailure(IPAddress? address, DateTime nowUtc)
            {
                if (address == null)
                {
                    return false;
                }

                lock (m_Lock)
                {
                    PruneExpired(nowUtc);

                    if (!m_Failures.TryGetValue(address, out FailureWindow? window) ||
                        window == null ||
                        nowUtc - window.FirstFailureUtc > s_ThrottleWindow)
                    {
                        window = new FailureWindow { FirstFailureUtc = nowUtc };
                        m_Failures[address] = window;
                    }

                    window.Count++;
                    return window.Count >= c_MaxFailures;
                }
            }

            public void Reset(IPAddress? address)
            {
                if (address == null)
                {
                    return;
                }

                lock (m_Lock)
                {
                    m_Failures.Remove(address);
                }
            }

            private void PruneExpired(DateTime nowUtc)
            {
                List<IPAddress> expired = new List<IPAddress>();
                foreach (KeyValuePair<IPAddress, FailureWindow> entry in m_Failures)
                {
                    if (nowUtc - entry.Value.FirstFailureUtc > s_ThrottleWindow)
                    {
                        expired.Add(entry.Key);
                    }
                }

                foreach (IPAddress address in expired)
                {
                    m_Failures.Remove(address);
                }
            }
        }
    }
}
