using System;
using System.Net;
using Xunit;

namespace SkyCoopServer.Tests
{
    public class AdminAuthTests
    {
        [Fact]
        public void PasswordMatches_EmptyConfigured_IsFalse()
        {
            Assert.False(AdminAuth.PasswordMatches("", "anything"));
        }

        [Fact]
        public void PasswordMatches_Exact_IsTrue()
        {
            Assert.True(AdminAuth.PasswordMatches("s3cret", "s3cret"));
        }

        [Fact]
        public void PasswordMatches_DifferentLength_IsFalse()
        {
            Assert.False(AdminAuth.PasswordMatches("s3cret", "s3cret!"));
        }

        [Fact]
        public void IsLoopback_Ipv4AndIpv6AndMapped()
        {
            Assert.True(AdminAuth.IsLoopback(IPAddress.Parse("127.0.0.1")));
            Assert.True(AdminAuth.IsLoopback(IPAddress.Parse("::1")));
            Assert.True(AdminAuth.IsLoopback(IPAddress.Parse("::ffff:127.0.0.1")));
            Assert.False(AdminAuth.IsLoopback(IPAddress.Parse("192.168.1.5")));
        }

        [Fact]
        public void Throttle_FifthFailureWithin60s_Disconnects()
        {
            AdminAuth.LoginThrottle throttle = new AdminAuth.LoginThrottle();
            DateTime start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            IPAddress address = IPAddress.Parse("203.0.113.7");

            Assert.False(throttle.RecordFailure(address, start));
            Assert.False(throttle.RecordFailure(address, start.AddSeconds(10)));
            Assert.False(throttle.RecordFailure(address, start.AddSeconds(20)));
            Assert.False(throttle.RecordFailure(address, start.AddSeconds(30)));
            Assert.True(throttle.RecordFailure(address, start.AddSeconds(40)));
        }

        [Fact]
        public void Throttle_FailuresOutsideWindow_DoNotAccumulate()
        {
            AdminAuth.LoginThrottle throttle = new AdminAuth.LoginThrottle();
            DateTime start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            IPAddress address = IPAddress.Parse("203.0.113.7");

            Assert.False(throttle.RecordFailure(address, start));
            Assert.False(throttle.RecordFailure(address, start.AddSeconds(10)));
            Assert.False(throttle.RecordFailure(address, start.AddSeconds(20)));
            Assert.False(throttle.RecordFailure(address, start.AddSeconds(30)));
            Assert.False(throttle.RecordFailure(address, start.AddSeconds(61)));
        }

        [Fact]
        public void Throttle_SameAddressAccumulates()
        {
            AdminAuth.LoginThrottle throttle = new AdminAuth.LoginThrottle();
            DateTime start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            IPAddress address = IPAddress.Parse("203.0.113.7");

            Assert.False(throttle.RecordFailure(address, start));
            Assert.False(throttle.RecordFailure(address, start.AddSeconds(5)));
            Assert.False(throttle.RecordFailure(address, start.AddSeconds(10)));
            Assert.False(throttle.RecordFailure(address, start.AddSeconds(15)));
            Assert.True(throttle.RecordFailure(address, start.AddSeconds(20)));
        }

        [Fact]
        public void Throttle_DifferentAddressesAreIndependent()
        {
            AdminAuth.LoginThrottle throttle = new AdminAuth.LoginThrottle();
            DateTime start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            IPAddress first = IPAddress.Parse("203.0.113.7");
            IPAddress second = IPAddress.Parse("198.51.100.9");

            for (int i = 0; i < 4; i++)
            {
                Assert.False(throttle.RecordFailure(first, start.AddSeconds(i)));
            }

            for (int i = 0; i < 3; i++)
            {
                Assert.False(throttle.RecordFailure(second, start.AddSeconds(i)));
            }

            Assert.True(throttle.RecordFailure(first, start.AddSeconds(10)));
            Assert.False(throttle.RecordFailure(second, start.AddSeconds(10)));
        }

        [Fact]
        public void Throttle_ResetClears()
        {
            AdminAuth.LoginThrottle throttle = new AdminAuth.LoginThrottle();
            DateTime start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            IPAddress address = IPAddress.Parse("203.0.113.7");

            for (int i = 0; i < 4; i++)
            {
                Assert.False(throttle.RecordFailure(address, start.AddSeconds(i)));
            }

            throttle.Reset(address);

            Assert.False(throttle.RecordFailure(address, start.AddSeconds(10)));
        }

        [Fact]
        public void Throttle_NoResetAcrossReconnect()
        {
            AdminAuth.LoginThrottle throttle = new AdminAuth.LoginThrottle();
            DateTime start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            IPAddress address = IPAddress.Parse("203.0.113.7");

            for (int i = 0; i < 4; i++)
            {
                Assert.False(throttle.RecordFailure(address, start.AddSeconds(i)));
            }

            // A reconnect from the same address must not clear the window; the
            // disconnect path no longer calls Reset.
            Assert.True(throttle.RecordFailure(address, start.AddSeconds(10)));
        }

        [Fact]
        public void IsAllowed_NonOperator_OnlyPlayerCommands()
        {
            DataStr.PlayerData player = new DataStr.PlayerData(1);

            Assert.True(AdminAuth.IsAllowed(player, "squad"));
            Assert.False(AdminAuth.IsAllowed(player, "pvp"));
        }

        [Fact]
        public void IsAllowed_Operator_Everything()
        {
            DataStr.PlayerData player = new DataStr.PlayerData(1)
            {
                m_IsOperator = true
            };

            Assert.True(AdminAuth.IsAllowed(player, "pvp"));
            Assert.True(AdminAuth.IsAllowed(player, "squad"));
        }
    }
}
