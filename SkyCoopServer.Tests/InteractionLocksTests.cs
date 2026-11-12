using System.Collections.Generic;
using Xunit;

namespace SkyCoopServer.Tests
{
    public class InteractionLocksTests
    {
        [Fact]
        public void HolderOf_MatchesInteractionOrCarSeat()
        {
            var players = new List<DataStr.PlayerData>
            {
                new DataStr.PlayerData(0) { m_InteractionGUID = "box" },
                new DataStr.PlayerData(1) { m_CarSeat = "car" },
            };

            Assert.Equal(0, InteractionLocks.HolderOf(players, "box"));
            Assert.Equal(1, InteractionLocks.HolderOf(players, "car"));
        }

        [Fact]
        public void HolderOf_EmptyGuid_IsNull()
        {
            var players = new List<DataStr.PlayerData>
            {
                new DataStr.PlayerData(0) { m_InteractionGUID = "" },
            };

            Assert.Null(InteractionLocks.HolderOf(players, ""));
        }

        [Fact]
        public void IsBlocked_FalseForOwnLock()
        {
            var players = new List<DataStr.PlayerData>
            {
                new DataStr.PlayerData(4) { m_InteractionGUID = "box" },
            };

            Assert.False(InteractionLocks.IsBlocked(players, "box", 4));
            Assert.True(InteractionLocks.IsBlocked(players, "box", 5));
        }
    }
}
