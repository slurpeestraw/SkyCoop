using Il2Cpp;
using SkyCoop;
using SkyCoopServer;
using System;
using UnityEngine;

namespace SkyCoopClient
{
    public static class CorpseLockWatchdog
    {
        private static string s_Guid = "";
        private static BodyHarvest? s_BodyHarvest;
        private static float s_GrantedAt;
        private static bool s_RemovalSent;

        public static void OnGranted(BodyHarvest bh, string guid)
        {
            s_BodyHarvest = bh;
            s_Guid = guid ?? "";
            s_GrantedAt = Time.realtimeSinceStartup;
            s_RemovalSent = false;
        }

        public static void OnRemovalSent(string guid)
        {
            if (!string.IsNullOrEmpty(s_Guid) && s_Guid == guid)
            {
                s_RemovalSent = true;
            }
        }

        public static void Clear()
        {
            s_BodyHarvest = null;
            s_Guid = "";
            s_RemovalSent = false;
        }

        public static void Tick()
        {
            if (string.IsNullOrEmpty(s_Guid))
            {
                return;
            }

            try
            {
                float seconds = Time.realtimeSinceStartup - s_GrantedAt;
                bool panelOpen = false;

                BodyHarvest? bh = s_BodyHarvest;
                if (bh != null)
                {
                    Panel_BodyHarvest panel = InterfaceManager.GetPanel<Panel_BodyHarvest>();
                    panelOpen = panel != null && panel.IsEnabled() && panel.m_BodyHarvest == bh;
                }

                if (!CorpseLockRules.ShouldRelease(panelOpen, s_RemovalSent, CorpseSync.s_Quartering, seconds))
                {
                    return;
                }

                string guid = s_Guid;
                if (EntitySync.CanSend())
                {
                    ClientSend.SendFinishInteract();
                }

                SkyCoop.Logger.Log(ConsoleColor.Yellow, $"[CorpseLock] released idle lock {guid} after {seconds:F1}s");
                Clear();
            }
            catch (Exception e)
            {
                string guid = s_Guid;
                try
                {
                    if (EntitySync.CanSend())
                    {
                        ClientSend.SendFinishInteract();
                    }
                }
                catch (Exception sendError)
                {
                    SkyCoop.Logger.Log(ConsoleColor.Red, $"[CorpseLock] failed to release {guid} after error: {sendError.GetType().Name}: {sendError.Message}");
                }

                SkyCoop.Logger.Log(ConsoleColor.Red, $"[CorpseLock] release check failed for {guid}: {e.GetType().Name}: {e.Message}");
                Clear();
            }
        }
    }
}
