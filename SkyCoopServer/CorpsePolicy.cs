namespace SkyCoopServer
{
    public sealed class CorpsePolicy : IEntityPolicy
    {
        public const string c_Kind = "corpse";

        public string Kind => c_Kind;

        public bool AcceptsUnlockedFinalReports => true;
    }
}
