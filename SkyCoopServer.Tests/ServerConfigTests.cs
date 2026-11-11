using System.Text;
using LiteNetLib.Utils;
using Xunit;

namespace SkyCoopServer.Tests
{
    public class ServerConfigTests
    {
        [Fact]
        public void Load_MapsPasswords()
        {
            DataStr.ServerConfigJSON json = new DataStr.ServerConfigJSON
            {
                ServerName = "Test",
                AdminPassword = "s3cret",
                JoinPassword = "joins3cret"
            };

            DataStr.ServerConfig config = json.Load();

            Assert.Equal("s3cret", config.m_AdminPassword);
            Assert.Equal("joins3cret", config.m_JoinPassword);
        }

        [Fact]
        public void Load_TrustLoopback_DefaultsTrue_WhenMissing()
        {
            DataStr.ServerConfigJSON json = new DataStr.ServerConfigJSON
            {
                ServerName = "Test"
            };

            DataStr.ServerConfig config = json.Load();

            Assert.True(config.m_TrustLoopback);
        }

        [Fact]
        public void Load_TrustLoopback_False_IsPreserved()
        {
            DataStr.ServerConfigJSON json = new DataStr.ServerConfigJSON
            {
                ServerName = "Test",
                TrustLoopback = false
            };

            DataStr.ServerConfig config = json.Load();

            Assert.False(config.m_TrustLoopback);
        }

        [Fact]
        public void GetConnectionKey_DefaultsToPublicKey()
        {
            DataStr.ServerConfig config = new DataStr.ServerConfig();

            Assert.Equal(Packet.c_Key, config.GetConnectionKey());
        }

        [Fact]
        public void GetConnectionKey_UsesJoinPassword()
        {
            DataStr.ServerConfig config = new DataStr.ServerConfig
            {
                m_JoinPassword = "joinpass"
            };

            Assert.Equal("joinpass", config.GetConnectionKey());
        }

        [Fact]
        public void ConfigPacket_DoesNotContainPasswords()
        {
            DataStr.ServerConfig config = new DataStr.ServerConfig
            {
                m_AdminPassword = "s3cret",
                m_JoinPassword = "s3cret"
            };
            NetDataWriter writer = new NetDataWriter();

            Packet.Put(writer, config);

            Assert.True(writer.Length > 0, "Config packet must contain data.");

            byte[] secret = Encoding.UTF8.GetBytes("s3cret");
            byte[] data = writer.Data;
            int length = writer.Length;
            bool contains = false;
            for (int i = 0; i + secret.Length <= length; i++)
            {
                bool match = true;
                for (int j = 0; j < secret.Length; j++)
                {
                    if (data[i + j] != secret[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    contains = true;
                    break;
                }
            }

            Assert.False(contains, "Config packet must not contain admin or join password bytes.");
        }
    }
}