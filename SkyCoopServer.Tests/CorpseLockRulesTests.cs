using SkyCoopServer;
using Xunit;

namespace SkyCoopServer.Tests
{
    public class CorpseLockRulesTests
    {
        [Fact]
        public void ShouldRelease_PanelOpen_False()
        {
            Assert.False(CorpseLockRules.ShouldRelease(true, false, false, 10f));
        }

        [Fact]
        public void ShouldRelease_RemovalSent_False()
        {
            Assert.False(CorpseLockRules.ShouldRelease(false, true, false, 10f));
        }

        [Fact]
        public void ShouldRelease_Quartering_False()
        {
            Assert.False(CorpseLockRules.ShouldRelease(false, false, true, 10f));
        }

        [Fact]
        public void ShouldRelease_WithinGrace_False()
        {
            Assert.False(CorpseLockRules.ShouldRelease(
                false, false, false, CorpseLockRules.c_GraceSeconds - 0.1f));
        }

        [Fact]
        public void ShouldRelease_IdleAfterGrace_True()
        {
            Assert.True(CorpseLockRules.ShouldRelease(
                false, false, false, CorpseLockRules.c_GraceSeconds));
        }
    }
}
