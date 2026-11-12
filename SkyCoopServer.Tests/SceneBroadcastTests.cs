using System.Collections.Generic;
using Xunit;

namespace SkyCoopServer.Tests
{
    public class SceneBroadcastTests
    {
        private static DataStr.PlayerData Player(int id, string scene) =>
            new DataStr.PlayerData(id) { m_Scene = scene };

        [Fact]
        public void Recipients_FiltersSceneAndExcludesSender()
        {
            var players = new List<DataStr.PlayerData>
            {
                Player(0, "A"),
                Player(1, "A"),
                Player(2, "B"),
            };

            List<int> recipients = SceneBroadcast.Recipients(players, "A", 1);

            Assert.Equal(new List<int> { 0 }, recipients);
        }

        [Fact]
        public void Recipients_NonGameplayScene_IsEmpty()
        {
            var players = new List<DataStr.PlayerData> { Player(0, "Empty") };

            List<int> recipients = SceneBroadcast.Recipients(players, "Empty");

            Assert.Empty(recipients);
        }
    }
}
