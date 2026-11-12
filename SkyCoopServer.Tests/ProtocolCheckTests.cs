using Xunit;

namespace SkyCoopServer.Tests
{
    public class ProtocolCheckTests
    {
        [Fact]
        public void Matching_IsCompatible()
        {
            bool compatible = ProtocolCheck.IsCompatible(
                Packet.c_ProtocolVersion,
                out string reason);

            Assert.True(compatible);
            Assert.Equal("", reason);
        }

        [Fact]
        public void Missing_IsIncompatible_WithUpdateMessage()
        {
            bool compatible = ProtocolCheck.IsCompatible(null, out string reason);

            Assert.False(compatible);
            Assert.Equal($"Client too old: update SkyCoop (server protocol {Packet.c_ProtocolVersion}).", reason);
        }

        [Fact]
        public void Older_IsIncompatible()
        {
            bool compatible = ProtocolCheck.IsCompatible(0, out string reason);

            Assert.False(compatible);
            Assert.Contains($"client 0, server {Packet.c_ProtocolVersion}", reason);
        }

        [Fact]
        public void Newer_IsIncompatible()
        {
            bool compatible = ProtocolCheck.IsCompatible(Packet.c_ProtocolVersion + 1, out string reason);

            Assert.False(compatible);
            Assert.Contains("Server too old", reason);
        }
    }
}
