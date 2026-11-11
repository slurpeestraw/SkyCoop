using Xunit;

namespace SkyCoopServer.Tests
{
    public class SmokeTests
    {
        [Fact]
        public void ServerAssemblyLoads()
        {
            Assert.Equal("SkyCoopServer", typeof(SkyCoopServer.Packet).Assembly.GetName().Name);
        }
    }
}
