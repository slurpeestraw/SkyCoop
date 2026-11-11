using Xunit;

namespace SkyCoopServer.Tests
{
    public class CommandTextTests
    {
        [Fact]
        public void NormalizeClientCommand_Login_PreservesPasswordCase()
        {
            Assert.Equal("login MySecret", CommandText.NormalizeClientCommand("login MySecret"));
        }

        [Fact]
        public void NormalizeClientCommand_LoginTabAndLeadingWhitespace_PreservesPasswordCase()
        {
            Assert.Equal("login MySecret", CommandText.NormalizeClientCommand("  \tLOGIN\tMySecret"));
        }

        [Fact]
        public void NormalizeClientCommand_NonLogin_Lowercases()
        {
            Assert.Equal("pvp", CommandText.NormalizeClientCommand("PVP"));
        }

        [Fact]
        public void TryParseLogin_TabSeparator_ReturnsAttempt()
        {
            Assert.True(CommandText.TryParseLogin("login\tpw", out string attempt));
            Assert.Equal("pw", attempt);
        }

        [Fact]
        public void TryParseLogin_LeadingWhitespace_ReturnsAttempt()
        {
            Assert.True(CommandText.TryParseLogin("  \tlogin  pw", out string attempt));
            Assert.Equal("pw", attempt);
        }

        [Fact]
        public void TryParseLogin_ExactLogin_ReturnsEmptyAttempt()
        {
            Assert.True(CommandText.TryParseLogin("login", out string attempt));
            Assert.Equal("", attempt);
        }

        [Fact]
        public void TryParseLogin_NonLogin_ReturnsFalse()
        {
            Assert.False(CommandText.TryParseLogin("pvp", out string attempt));
            Assert.Equal("", attempt);
        }
    }
}
