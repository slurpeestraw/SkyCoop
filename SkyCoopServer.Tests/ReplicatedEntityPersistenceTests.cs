using System.Numerics;
using System.Text.Json;
using Xunit;

namespace SkyCoopServer.Tests
{
    public class ReplicatedEntityPersistenceTests
    {
        [Fact]
        public void SceneSaveData_Json_RoundTrips_Entities()
        {
            DataStr.SceneData.SaveData save = new DataStr.SceneData.SaveData
            {
                SceneName = "LakeRegion",
                Entities = new System.Collections.Generic.List<ReplicatedEntity.SaveData>
                {
                    new ReplicatedEntity
                    {
                        m_Kind = "corpse",
                        m_GUID = "guid-1",
                        m_Prefab = "Body",
                        m_State = "state",
                        m_Position = new Vector3(1, 2, 3),
                        m_Rotation = new Quaternion(4, 5, 6, 7),
                    }.Save(),
                },
            };

            string json = JsonSerializer.Serialize(save);
            DataStr.SceneData.SaveData? loaded =
                JsonSerializer.Deserialize<DataStr.SceneData.SaveData>(json);

            Assert.NotNull(loaded);
            Assert.NotNull(loaded!.Entities);
            Assert.Single(loaded.Entities!);
            Assert.Equal("guid-1", loaded.Entities![0].GUID);
            Assert.Equal("state", loaded.Entities[0].State);
            Assert.Equal(1, loaded.Entities[0].Position.x);
            Assert.Equal(7, loaded.Entities[0].Rotation.w);
        }

        [Fact]
        public void SceneSaveData_OldJsonWithoutEntities_Loads()
        {
            const string json = "{\"SceneName\":\"LakeRegion\"}";

            DataStr.SceneData.SaveData? loaded =
                JsonSerializer.Deserialize<DataStr.SceneData.SaveData>(json);

            Assert.NotNull(loaded);
            Assert.Null(loaded!.Entities);
        }
    }
}
