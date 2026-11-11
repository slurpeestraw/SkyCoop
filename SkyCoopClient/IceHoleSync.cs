using Il2Cpp;
using Il2CppTLD.PDID;
using SkyCoop;
using System;
using UnityEngine;

namespace SkyCoopClient
{
    public static class IceHoleSync
    {
        private static bool s_ApplyingRemote = false;

        public static string GetKey(IceFishingHole Hole)
        {
            if (Hole == null)
            {
                return string.Empty;
            }

            ObjectGuid GuidObj = Hole.GetComponent<ObjectGuid>();
            if (GuidObj)
            {
                string Guid = GuidObj.GetPDID();
                if (!string.IsNullOrEmpty(Guid))
                {
                    return Guid;
                }
            }

            Vector3 Position = Hole.transform.position;
            return $"IceHole_{Position.x:F2}_{Position.y:F2}_{Position.z:F2}";
        }

        public static void SendStatus(IceFishingHole Hole)
        {
            if (!ModMain.IsMultiplayer() || ModMain.Client == null || !ModMain.Client.m_IsReady || Hole == null)
            {
                return;
            }

            if (Minimalizer.s_LoadingFlag || Minimalizer.s_FramesDelayBeforeSendNewScene > 0)
            {
                return;
            }

            try
            {
                string SceneName = ModMain.GetCurrentSceneName();
                if (string.IsNullOrEmpty(SceneName) || !ModMain.IsGameplayScene(SceneName))
                {
                    return;
                }

                ClientSend.SendIceHoleStatus(
                    GetKey(Hole),
                    Hole.transform.position,
                    Hole.NormalizedFrozen,
                    (int)Hole.m_State,
                    Hole.m_MinutesSinceLastClearing,
                    Hole.PauseFreezing);
            }
            catch (Exception Exception)
            {
                SkyCoop.Logger.Log(ConsoleColor.Red, $"[IceHoleSync] Failed to send ice hole status: {Exception.Message}");
            }
        }

        public static void HandleIceHoleStatus(string Key, Vector3 Position, float NormalizedFrozen, int FishingState, float MinutesSinceLastClearing, bool PauseFreezing)
        {
            if (Minimalizer.s_LoadingFlag)
            {
                return;
            }

            try
            {
                IceFishingHole Hole = FindHole(Key, Position);
                if (Hole == null)
                {
                    return;
                }

                float Frozen = Mathf.Clamp01(NormalizedFrozen);

                s_ApplyingRemote = true;
                try
                {
                    Hole.SetNormalizedFrozen(Frozen);
                    Hole.m_FrozenPercent = Mathf.RoundToInt(Frozen * 100f);
                    Hole.m_MinutesSinceLastClearing = MinutesSinceLastClearing;
                    Hole.PauseFreezing = PauseFreezing;
                    Hole.m_State = (FishingState)FishingState;
                }
                finally
                {
                    s_ApplyingRemote = false;
                }
            }
            catch (Exception Exception)
            {
                s_ApplyingRemote = false;
                SkyCoop.Logger.Log(ConsoleColor.Red, $"[IceHoleSync] Failed to apply ice hole status: {Exception.Message}");
            }
        }

        private static IceFishingHole FindHole(string Key, Vector3 Position)
        {
            if (!string.IsNullOrEmpty(Key))
            {
                GameObject Obj = PdidTable.GetGameObject(Key);
                if (Obj)
                {
                    IceFishingHole Hole = Obj.GetComponent<IceFishingHole>();
                    if (Hole)
                    {
                        return Hole;
                    }
                }
            }

            if (IceFishingHole.s_IceFishingHoles != null)
            {
                for (int i = 0; i < IceFishingHole.s_IceFishingHoles.Count; i++)
                {
                    IceFishingHole Hole = IceFishingHole.s_IceFishingHoles[i];
                    if (Hole && Vector3.Distance(Hole.transform.position, Position) < 1f)
                    {
                        return Hole;
                    }
                }
            }

            return null;
        }

        private static void MaybeSend(IceFishingHole Hole)
        {
            if (!s_ApplyingRemote)
            {
                SendStatus(Hole);
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(IceFishingHole), "ClearHole")]
        private static class IceFishingHole_ClearHole
        {
            private static void Postfix(IceFishingHole __instance)
            {
                if (!ModMain.IsMultiplayer()) { return; }

                MaybeSend(__instance);
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(IceFishingHole), "SetNormalizedFrozen")]
        private static class IceFishingHole_SetNormalizedFrozen
        {
            private static void Postfix(IceFishingHole __instance)
            {
                if (!ModMain.IsMultiplayer()) { return; }

                MaybeSend(__instance);
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(IceFishingHole), "StartFishing")]
        private static class IceFishingHole_StartFishing
        {
            private static void Postfix(IceFishingHole __instance)
            {
                if (!ModMain.IsMultiplayer()) { return; }

                MaybeSend(__instance);
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(IceFishingHole), "CancelFishing")]
        private static class IceFishingHole_CancelFishing
        {
            private static void Postfix(IceFishingHole __instance)
            {
                if (!ModMain.IsMultiplayer()) { return; }

                MaybeSend(__instance);
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(IceFishingHole), "OnActiveFishingComplete")]
        private static class IceFishingHole_OnActiveFishingComplete
        {
            private static void Postfix(IceFishingHole __instance)
            {
                if (!ModMain.IsMultiplayer()) { return; }

                MaybeSend(__instance);
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(Panel_IceFishingHoleClear), "OnBreakIceComplete")]
        private static class Panel_IceFishingHoleClear_OnBreakIceComplete
        {
            private static void Postfix(Panel_IceFishingHoleClear __instance, bool playerCancel)
            {
                if (playerCancel || !ModMain.IsMultiplayer() || __instance == null || __instance.m_IsBuildingHole || __instance.m_IceFishingHole == null)
                {
                    return;
                }

                MaybeSend(__instance.m_IceFishingHole);
            }
        }
    }
}
