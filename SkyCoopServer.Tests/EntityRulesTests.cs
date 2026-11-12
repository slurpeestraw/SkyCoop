using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace SkyCoopServer.Tests
{
    public class EntityRulesTests
    {
        private sealed class TestPolicy : IEntityPolicy
        {
            public string Kind { get; }

            public bool AcceptsUnlockedFinalReports { get; }

            public TestPolicy(string kind, bool acceptsUnlockedFinalReports)
            {
                Kind = kind;
                AcceptsUnlockedFinalReports = acceptsUnlockedFinalReports;
            }
        }

        private static ReplicatedEntity Entity(string kind = "test.kind", string state = "state") =>
            new ReplicatedEntity
            {
                m_Kind = kind,
                m_GUID = "guid",
                m_Prefab = "prefab",
                m_State = state,
                m_Uploader = 2,
            };

        [Fact]
        public void Evaluate_UnknownEntity_Rejects()
        {
            UpdateDecision decision = EntityRules.Evaluate(null, "A", "A", 1, null, EntityFlags.None);

            Assert.Equal(UpdateDecision.Reject, decision);
        }

        [Fact]
        public void Evaluate_OtherScene_Rejects()
        {
            UpdateDecision decision = EntityRules.Evaluate(Entity(), "A", "B", 1, null, EntityFlags.None);

            Assert.Equal(UpdateDecision.Reject, decision);
        }

        [Fact]
        public void Evaluate_PendingUploader_Accepts()
        {
            UpdateDecision decision = EntityRules.Evaluate(Entity(state: ""), "A", "A", 2, null, EntityFlags.None);

            Assert.Equal(UpdateDecision.Accept, decision);
        }

        [Fact]
        public void Evaluate_PendingNonUploader_Rejects()
        {
            UpdateDecision decision = EntityRules.Evaluate(Entity(state: ""), "A", "A", 3, null, EntityFlags.None);

            Assert.Equal(UpdateDecision.Reject, decision);
        }

        [Fact]
        public void Evaluate_LockHolder_Accepts()
        {
            UpdateDecision decision = EntityRules.Evaluate(Entity(), "A", "A", 3, 3, EntityFlags.None);

            Assert.Equal(UpdateDecision.Accept, decision);
        }

        [Fact]
        public void Evaluate_NonHolderWhileLocked_Rejects()
        {
            UpdateDecision decision = EntityRules.Evaluate(Entity(), "A", "A", 3, 1, EntityFlags.None);

            Assert.Equal(UpdateDecision.Reject, decision);
        }

        [Fact]
        public void Evaluate_UnlockedFinal_AcceptedOnlyWhenPolicyAllows()
        {
            EntityRules.Register(new TestPolicy("test.unlocked-final", true));

            UpdateDecision allowed = EntityRules.Evaluate(
                Entity(kind: "test.unlocked-final"), "A", "A", 3, null, EntityFlags.Final);
            UpdateDecision defaultKind = EntityRules.Evaluate(
                Entity(kind: "test.default-final"), "A", "A", 3, null, EntityFlags.Final);

            Assert.Equal(UpdateDecision.AcceptAndRemove, allowed);
            Assert.Equal(UpdateDecision.Reject, defaultKind);
        }

        [Fact]
        public void Evaluate_FinalFromHolder_IsAcceptAndRemove()
        {
            UpdateDecision decision = EntityRules.Evaluate(Entity(), "A", "A", 3, 3, EntityFlags.Final);

            Assert.Equal(UpdateDecision.AcceptAndRemove, decision);
        }

        [Fact]
        public void PickUploader_NotPending_Null()
        {
            int? uploader = EntityRules.PickUploader(
                Entity(), new List<int> { 1 }, DateTime.UtcNow, TimeSpan.FromSeconds(10));

            Assert.Null(uploader);
        }

        [Fact]
        public void PickUploader_UploaderPresentWithinTimeout_Null()
        {
            DateTime now = DateTime.UtcNow;
            ReplicatedEntity entity = Entity(state: "");
            entity.m_Uploader = 3;
            entity.m_PendingSinceUtc = now;

            int? uploader = EntityRules.PickUploader(
                entity, new List<int> { 3, 4 }, now, TimeSpan.FromSeconds(10));

            Assert.Null(uploader);
        }

        [Fact]
        public void PickUploader_UploaderGone_PicksLowestOther()
        {
            DateTime now = DateTime.UtcNow;
            ReplicatedEntity entity = Entity(state: "");
            entity.m_Uploader = 3;
            entity.m_PendingSinceUtc = now;

            int? uploader = EntityRules.PickUploader(
                entity, new List<int> { 5, 1 }, now, TimeSpan.FromSeconds(10));

            Assert.Equal(1, uploader);
        }

        [Fact]
        public void PickUploader_Timeout_Reassigns()
        {
            DateTime pending = DateTime.UtcNow;
            ReplicatedEntity entity = Entity(state: "");
            entity.m_Uploader = 3;
            entity.m_PendingSinceUtc = pending;

            int? uploader = EntityRules.PickUploader(
                entity, new List<int> { 3, 4 }, pending + TimeSpan.FromSeconds(11), TimeSpan.FromSeconds(10));

            Assert.Equal(4, uploader);
        }

        [Fact]
        public void PickUploader_EmptyScene_Null()
        {
            ReplicatedEntity entity = Entity(state: "");
            entity.m_Uploader = 3;

            int? uploader = EntityRules.PickUploader(
                entity, new List<int>(), DateTime.UtcNow, TimeSpan.FromSeconds(10));

            Assert.Null(uploader);
        }

        [Fact]
        public void SaveLoad_RoundTrips_AllPersistedFields_AndDropsRuntimeFields()
        {
            ReplicatedEntity entity = new ReplicatedEntity
            {
                m_Kind = "corpse",
                m_GUID = "abc",
                m_Prefab = "Body",
                m_State = "compressed-state",
                m_Position = new Vector3(1, 2, 3),
                m_Rotation = new Quaternion(4, 5, 6, 7),
                m_Uploader = 5,
            };

            ReplicatedEntity loaded = ReplicatedEntity.Load(entity.Save());

            Assert.Equal(entity.m_Kind, loaded.m_Kind);
            Assert.Equal(entity.m_GUID, loaded.m_GUID);
            Assert.Equal(entity.m_Prefab, loaded.m_Prefab);
            Assert.Equal(entity.m_State, loaded.m_State);
            Assert.Equal(entity.m_Position, loaded.m_Position);
            Assert.Equal(entity.m_Rotation, loaded.m_Rotation);
            Assert.Equal(-1, loaded.m_Uploader);
        }
    }
}
