using System.Numerics;
using LiteNetLib.Utils;
using Xunit;

namespace SkyCoopServer.Tests
{
    public class EntityMessageTests
    {
        [Fact]
        public void EntityMessage_RoundTrips()
        {
            EntityMessage original = new EntityMessage
            {
                Kind = "corpse",
                GUID = "guid-1",
                Prefab = "Body",
                State = "compressed-state",
                Position = new Vector3(1, 2, 3),
                Rotation = new Quaternion(4, 5, 6, 7),
                Flags = EntityFlags.Final,
                IsUploader = true,
                Reason = 3,
            };

            NetDataWriter writer = new NetDataWriter();
            writer.Put(original);
            EntityMessage loaded = new NetDataReader(writer).GetEntityMessage();

            Assert.Equal(original.Kind, loaded.Kind);
            Assert.Equal(original.GUID, loaded.GUID);
            Assert.Equal(original.Prefab, loaded.Prefab);
            Assert.Equal(original.State, loaded.State);
            Assert.Equal(original.Position, loaded.Position);
            Assert.Equal(original.Rotation, loaded.Rotation);
            Assert.Equal(EntityFlags.Final, loaded.Flags);
            Assert.True(loaded.IsUploader);
            Assert.Equal(3, loaded.Reason);
        }

        [Fact]
        public void EntityMessage_EmptyState_RoundTrips()
        {
            EntityMessage original = new EntityMessage
            {
                Kind = "corpse",
                GUID = "guid-2",
                Prefab = "Body",
                State = "",
                Flags = EntityFlags.None,
            };

            NetDataWriter writer = new NetDataWriter();
            writer.Put(original);
            EntityMessage loaded = new NetDataReader(writer).GetEntityMessage();

            Assert.Equal("", loaded.State);
            Assert.False(loaded.IsUploader);
            Assert.Equal(EntityFlags.None, loaded.Flags);
        }
    }
}
