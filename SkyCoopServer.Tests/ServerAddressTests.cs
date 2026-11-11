using SkyCoopServer;
using Xunit;

namespace SkyCoopServer.Tests
{
    public class ServerAddressTests
    {
        private const int c_DefaultPort = 37855;

        [Fact]
        public void HostOnly_UsesDefaults()
        {
            bool result = ServerAddress.TryParse("example.com", c_DefaultPort, out string host, out int port, out string key);

            Assert.True(result);
            Assert.Equal("example.com", host);
            Assert.Equal(c_DefaultPort, port);
            Assert.Equal(Packet.c_Key, key);
        }

        [Fact]
        public void HostPort()
        {
            bool result = ServerAddress.TryParse("example.com:1234", c_DefaultPort, out string host, out int port, out string key);

            Assert.True(result);
            Assert.Equal("example.com", host);
            Assert.Equal(1234, port);
            Assert.Equal(Packet.c_Key, key);
        }

        [Fact]
        public void PasswordHostPort()
        {
            bool result = ServerAddress.TryParse("secret@example.com:1234", c_DefaultPort, out string host, out int port, out string key);

            Assert.True(result);
            Assert.Equal("example.com", host);
            Assert.Equal(1234, port);
            Assert.Equal("secret", key);
        }

        [Fact]
        public void PasswordWithAt_SplitsOnLast()
        {
            bool result = ServerAddress.TryParse("a@b@host:1", c_DefaultPort, out string host, out int port, out string key);

            Assert.True(result);
            Assert.Equal("a@b", key);
            Assert.Equal("host", host);
            Assert.Equal(1, port);
        }

        [Fact]
        public void BracketedIpv6()
        {
            bool withPort = ServerAddress.TryParse("[::1]:37855", c_DefaultPort, out string host, out int port, out string key);
            Assert.True(withPort);
            Assert.Equal("::1", host);
            Assert.Equal(37855, port);
            Assert.Equal(Packet.c_Key, key);

            bool withoutPort = ServerAddress.TryParse("[::1]", c_DefaultPort, out host, out port, out key);
            Assert.True(withoutPort);
            Assert.Equal("::1", host);
            Assert.Equal(c_DefaultPort, port);
            Assert.Equal(Packet.c_Key, key);
        }

        [Fact]
        public void SurroundingWhitespace_IsTrimmed()
        {
            bool result = ServerAddress.TryParse(" host:1 ", c_DefaultPort, out string host, out int port, out string key);

            Assert.True(result);
            Assert.Equal("host", host);
            Assert.Equal(1, port);
            Assert.Equal(Packet.c_Key, key);
        }

        [Fact]
        public void BadPort_ReturnsFalse()
        {
            Assert.False(ServerAddress.TryParse("host:abc", c_DefaultPort, out _, out _, out _));
            Assert.False(ServerAddress.TryParse("host:70000", c_DefaultPort, out _, out _, out _));
        }

        [Fact]
        public void EmptyHost_ReturnsFalse()
        {
            Assert.False(ServerAddress.TryParse("", c_DefaultPort, out _, out _, out _));
            Assert.False(ServerAddress.TryParse(":37855", c_DefaultPort, out _, out _, out _));
            Assert.False(ServerAddress.TryParse("password@", c_DefaultPort, out _, out _, out _));
            Assert.False(ServerAddress.TryParse("[]", c_DefaultPort, out _, out _, out _));
        }
    }
}
