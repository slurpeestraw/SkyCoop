namespace SkyCoopServer
{
    public static class CorpseLockRules
    {
        public const float c_GraceSeconds = 1.5f;

        public static bool ShouldRelease(
            bool panelOpenOnCorpse,
            bool removalSent,
            bool quartering,
            float secondsSinceGrant)
        {
            if (panelOpenOnCorpse || removalSent || quartering)
            {
                return false;
            }

            return secondsSinceGrant >= c_GraceSeconds;
        }
    }
}
