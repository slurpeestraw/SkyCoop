using Xunit;

namespace SkyCoopServer.Tests
{
    public class PlayerSlotTests
    {
        [Fact]
        public void EnsureSlot_Grows_ToInclude_Id()
        {
            var manager = new PlayersDataManager(null!);
            manager.InitilizePlayers(2);

            bool result = manager.EnsureSlot(5);

            Assert.True(result);
            Assert.Equal(6, manager.m_Players.Count);
            Assert.Equal(5, manager.m_Players[5].m_PlayerID);
        }

        [Fact]
        public void EnsureSlot_Negative_ReturnsFalse()
        {
            var manager = new PlayersDataManager(null!);
            manager.InitilizePlayers(2);

            bool result = manager.EnsureSlot(-1);

            Assert.False(result);
            Assert.Equal(2, manager.m_Players.Count);
        }

        [Fact]
        public void EnsureSlot_Existing_DoesNotReplace()
        {
            var manager = new PlayersDataManager(null!);
            manager.InitilizePlayers(2);
            DataStr.PlayerData existing = manager.m_Players[1];

            bool result = manager.EnsureSlot(1);

            Assert.True(result);
            Assert.Same(existing, manager.m_Players[1]);
        }

        [Fact]
        public void SetPlayerName_UnknownIndex_ReturnsFalse_WithoutThrowing()
        {
            var manager = new PlayersDataManager(null!);
            manager.InitilizePlayers(2);

            bool result = manager.SetPlayerName(50, "Unknown");

            Assert.False(result);
        }

        [Fact]
        public void OnPlayerDisconnect_UnknownId_DoesNotThrow()
        {
            var manager = new PlayersDataManager(null!);
            manager.InitilizePlayers(2);

            System.Exception exception = Record.Exception(() => manager.OnPlayerDisconnect(50));

            Assert.Null(exception);
        }
    }
}
