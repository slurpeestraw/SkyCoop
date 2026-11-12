using Xunit;

namespace SkyCoopServer.Tests
{
    public class CorpsePolicyTests
    {
        private static ReplicatedEntity Corpse(string state = "s", int uploader = -1) =>
            new ReplicatedEntity
            {
                m_Kind = CorpsePolicy.c_Kind,
                m_GUID = "corpse-guid",
                m_State = state,
                m_Uploader = uploader,
            };

        public CorpsePolicyTests()
        {
            EntityRules.Register(new CorpsePolicy());
        }

        [Fact]
        public void Corpse_UnlockedFinalReport_IsAcceptedAndRemoved()
        {
            UpdateDecision decision = EntityRules.Evaluate(
                Corpse(), "LakeRegion", "LakeRegion", 2, null, EntityFlags.Final);

            Assert.Equal(UpdateDecision.AcceptAndRemove, decision);
        }

        [Fact]
        public void Corpse_UnlockedNonFinalReport_IsRejected()
        {
            UpdateDecision decision = EntityRules.Evaluate(
                Corpse(), "LakeRegion", "LakeRegion", 2, null, EntityFlags.None);

            Assert.Equal(UpdateDecision.Reject, decision);
        }

        [Fact]
        public void Corpse_PendingUpload_FromUploader_IsAccepted()
        {
            UpdateDecision decision = EntityRules.Evaluate(
                Corpse(state: "", uploader: 1), "LakeRegion", "LakeRegion", 1, null, EntityFlags.None);

            Assert.Equal(UpdateDecision.Accept, decision);
        }
    }
}
