using System.Collections.Generic;
using Xunit;

namespace SkyCoopServer.Tests
{
    public class AnimalDespawnTests
    {
        private static DataStr.AnimalEntity Animal(
            string guid,
            string spawner,
            bool summoned = false,
            int aiState = 0,
            bool bleeding = false,
            float lastUpdated = 0)
        {
            return new DataStr.AnimalEntity
            {
                m_GUID = guid,
                m_SpawnerGUID = spawner,
                m_Summoned = summoned,
                m_AiState = aiState,
                m_IsBleeding = bleeding,
                m_LastUpdated = lastUpdated,
            };
        }

        [Fact]
        public void SelectPopulationDespawns_LimitsToCount()
        {
            var animals = new List<DataStr.AnimalEntity>
            {
                Animal("a", "S"),
                Animal("b", "S"),
                Animal("c", "S"),
            };

            List<string> result = DataStr.SceneData.SelectPopulationDespawns(animals, "S", 2, 100f);

            Assert.Equal(new List<string> { "a", "b" }, result);
        }

        [Fact]
        public void SelectPopulationDespawns_SkipsSummonedAndOtherSpawners()
        {
            var animals = new List<DataStr.AnimalEntity>
            {
                Animal("summoned", "S", summoned: true),
                Animal("other", "T"),
                Animal("eligible", "S"),
            };

            List<string> result = DataStr.SceneData.SelectPopulationDespawns(animals, "S", 5, 100f);

            Assert.Equal(new List<string> { "eligible" }, result);
        }

        [Fact]
        public void SelectPopulationDespawns_SkipsNonDespawnable()
        {
            var animals = new List<DataStr.AnimalEntity>
            {
                Animal("fresh", "S", aiState: -1, bleeding: true, lastUpdated: 100f),
            };

            List<string> result = DataStr.SceneData.SelectPopulationDespawns(animals, "S", 5, 100f);

            Assert.Empty(result);
        }
    }
}
