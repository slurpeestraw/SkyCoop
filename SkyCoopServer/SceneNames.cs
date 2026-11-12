using System;

namespace SkyCoopServer
{
    public static class SceneNames
    {
        public static bool IsGameplay(string? scene)
        {
            if (string.IsNullOrEmpty(scene))
            {
                return false;
            }

            if (scene == "Empty" || scene == "Boot")
            {
                return false;
            }

            return !scene.StartsWith("MainMenu", StringComparison.Ordinal);
        }
    }
}
