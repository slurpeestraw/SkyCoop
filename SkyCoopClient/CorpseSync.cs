using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.PDID;
using SkyCoop;
using SkyCoopServer;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyCoopClient
{
    public static class CorpseSync
    {
        public static bool s_CorpseDebug = false;
        public static bool s_Quartering = false;

        private static string s_QuarterGuid = "";
        private static HashSet<int>? s_GearIdsBefore;

        public static void DebugLog(string message)
        {
            if (s_CorpseDebug)
            {
                SkyCoop.Logger.Log(ConsoleColor.DarkCyan, $"[CorpseSync] {message}");
            }
        }

        public static bool TryGetGuid(BodyHarvest? bh, out string guid)
        {
            guid = "";
            if (bh == null)
            {
                return false;
            }

            ObjectGuid? objectGuid = bh.GetComponent<ObjectGuid>();
            if (objectGuid == null)
            {
                objectGuid = bh.GetComponentInParent<ObjectGuid>();
            }

            if (objectGuid == null)
            {
                return false;
            }

            guid = objectGuid.GetPDID();
            return !string.IsNullOrEmpty(guid);
        }

        public static bool IsTrackedWorldCorpse(BodyHarvest? bh)
        {
            if (bh == null || bh.IsGearItem())
            {
                return false;
            }

            return TryGetGuid(bh, out string guid) &&
                   SkyCoopClient.EntitySync.IsTracked(CorpsePolicy.c_Kind, guid);
        }

        public static void ClosePanelIfOpen()
        {
            if (!ModMain.IsMultiplayer())
            {
                return;
            }

            try
            {
                Panel_BodyHarvest panel = InterfaceManager.GetPanel<Panel_BodyHarvest>();
                if (panel == null || panel.m_BodyHarvest == null || !panel.IsEnabled())
                {
                    return;
                }

                if (!IsTrackedWorldCorpse(panel.m_BodyHarvest))
                {
                    return;
                }

                DebugLog($"Closing panel for {panel.m_BodyHarvest.name}");
                panel.Enable(false);
            }
            catch (Exception e)
            {
                SkyCoop.Logger.Log(ConsoleColor.Red, $"[CorpseSync] ClosePanelIfOpen failed: {e.GetType().Name}: {e.Message}");
            }
        }

        [HarmonyPatch(typeof(BodyHarvest), "InteractWithBodyHarvest")]
        private static class BodyHarvest_InteractWithBodyHarvest
        {
            private static bool Prefix(BodyHarvest __instance, bool playBookEndAnimation)
            {
                if (!ModMain.IsMultiplayer())
                {
                    return true;
                }

                try
                {
                    DebugLog($"InteractWithBodyHarvest {__instance?.name} tracked={IsTrackedWorldCorpse(__instance)} gear={__instance?.IsGearItem()}");

                    if (PlayersManager.s_GrantedReentry || __instance == null || __instance.IsGearItem())
                    {
                        return true;
                    }

                    if (!IsTrackedWorldCorpse(__instance))
                    {
                        return true;
                    }

                    return !PlayersManager.TryInteract(
                        __instance,
                        () => __instance.InteractWithBodyHarvest(playBookEndAnimation));
                }
                catch (Exception e)
                {
                    SkyCoop.Logger.Log(ConsoleColor.Red, $"[CorpseSync] Interact prefix failed: {e.GetType().Name}: {e.Message}");
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(Panel_BodyHarvest), "Enable", new System.Type[]
        {
            typeof(bool), typeof(BodyHarvest), typeof(bool), typeof(ComingFromScreenCategory)
        })]
        private static class Panel_BodyHarvest_Enable
        {
            private static void Postfix(Panel_BodyHarvest __instance, bool enable)
            {
                if (!ModMain.IsMultiplayer() || enable)
                {
                    return;
                }

                try
                {
                    BodyHarvest bh = __instance.m_BodyHarvest;
                    DebugLog($"Panel Enable(false) {bh?.name} tracked={IsTrackedWorldCorpse(bh)} quartering={s_Quartering}");
                    if (!IsTrackedWorldCorpse(bh) || !TryGetGuid(bh, out string guid))
                    {
                        return;
                    }

                    if (s_Quartering)
                    {
                        return;
                    }

                    if (!SkyCoopClient.EntitySync.s_ApplyingRemote)
                    {
                        SkyCoopClient.EntitySync.SendState(CorpsePolicy.c_Kind, guid, bh.gameObject);
                    }

                    if (SkyCoopClient.EntitySync.CanSend())
                    {
                        ClientSend.SendFinishInteract();
                    }
                }
                catch (Exception e)
                {
                    SkyCoop.Logger.Log(ConsoleColor.Red, $"[CorpseSync] Panel close failed: {e.GetType().Name}: {e.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(Panel_BodyHarvest), "HarvestSuccessful")]
        private static class Panel_BodyHarvest_HarvestSuccessful
        {
            private static void Postfix(Panel_BodyHarvest __instance)
            {
                if (!ModMain.IsMultiplayer() || SkyCoopClient.EntitySync.s_ApplyingRemote)
                {
                    return;
                }

                try
                {
                    BodyHarvest bh = __instance.m_BodyHarvest;
                    if (!IsTrackedWorldCorpse(bh) || !TryGetGuid(bh, out string guid))
                    {
                        return;
                    }

                    DebugLog($"HarvestSuccessful {guid}");
                    SkyCoopClient.EntitySync.SendState(CorpsePolicy.c_Kind, guid, bh.gameObject);
                }
                catch (Exception e)
                {
                    SkyCoop.Logger.Log(ConsoleColor.Red, $"[CorpseSync] HarvestSuccessful failed: {e.GetType().Name}: {e.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(BodyHarvest), "BodyHarvestDestroy")]
        private static class BodyHarvest_Destroy
        {
            private static bool Prefix(BodyHarvest __instance)
            {
                if (!ModMain.IsMultiplayer() || s_Quartering || SkyCoopClient.EntitySync.s_ApplyingRemote)
                {
                    return true;
                }

                try
                {
                    if (!IsTrackedWorldCorpse(__instance) || !TryGetGuid(__instance, out string guid))
                    {
                        return true;
                    }

                    if (__instance.NoMoreResources() || __instance.ConditionReachedZero())
                    {
                        DebugLog($"Depleted {guid}");
                        SkyCoopClient.EntitySync.SendState(CorpsePolicy.c_Kind, guid, __instance.gameObject);
                    }
                }
                catch (Exception e)
                {
                    SkyCoop.Logger.Log(ConsoleColor.Red, $"[CorpseSync] Depleted report failed: {e.GetType().Name}: {e.Message}");
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(BodyHarvest), "MaybeDestroyAfterDeserialize")]
        private static class BodyHarvest_MaybeDestroyAfterDeserialize
        {
            private static bool Prefix()
            {
                return !SkyCoopClient.EntitySync.s_ApplyingRemote;
            }
        }

        [HarmonyPatch(typeof(Panel_BodyHarvest), "QuarterSuccessful")]
        private static class Panel_BodyHarvest_QuarterSuccessful
        {
            private static void Prefix(Panel_BodyHarvest __instance)
            {
                if (!ModMain.IsMultiplayer())
                {
                    return;
                }

                try
                {
                    BodyHarvest bh = __instance.m_BodyHarvest;
                    if (!IsTrackedWorldCorpse(bh) || !TryGetGuid(bh, out string guid))
                    {
                        return;
                    }

                    s_Quartering = true;
                    s_QuarterGuid = guid;
                    s_GearIdsBefore = SnapshotGearIds();
                    DebugLog($"QuarterSuccessful start {guid}");
                }
                catch (Exception e)
                {
                    SkyCoop.Logger.Log(ConsoleColor.Red, $"[CorpseSync] Quarter prefix failed: {e.GetType().Name}: {e.Message}");
                }
            }

            private static void Postfix()
            {
                if (!ModMain.IsMultiplayer())
                {
                    return;
                }

                string guid = s_QuarterGuid;
                HashSet<int>? before = s_GearIdsBefore;

                try
                {
                    if (before != null)
                    {
                        List<GearItem> newGear = new List<GearItem>();
                        for (int i = 0; i < GearManager.m_Gear.Count; i++)
                        {
                            GearItem gear = GearManager.m_Gear[i];
                            if (gear != null && !before.Contains(gear.GetInstanceID()) && !gear.m_InPlayerInventory)
                            {
                                newGear.Add(gear);
                            }
                        }

                        for (int i = 0; i < newGear.Count; i++)
                        {
                            DebugLog($"Quarter gear {newGear[i].name}");
                            GearsSync.SendDropItem(newGear[i]);
                        }
                    }

                    if (!string.IsNullOrEmpty(guid))
                    {
                        SkyCoopClient.EntitySync.SendRemove(CorpsePolicy.c_Kind, guid, 0);
                    }
                }
                catch (Exception e)
                {
                    SkyCoop.Logger.Log(ConsoleColor.Red, $"[CorpseSync] Quarter postfix failed: {e.GetType().Name}: {e.Message}");
                }
                finally
                {
                    s_Quartering = false;
                    s_QuarterGuid = "";
                    s_GearIdsBefore = null;
                }
            }
        }

        [HarmonyPatch(typeof(BaseAi), "SpawnCarcassPickup")]
        private static class BaseAi_SpawnCarcassPickup
        {
            private static void Postfix(BaseAi __instance)
            {
                if (!ModMain.IsMultiplayer() || s_Quartering || SkyCoopClient.EntitySync.s_ApplyingRemote)
                {
                    return;
                }

                try
                {
                    BodyHarvest bh = __instance.m_BodyHarvest;
                    if (!IsTrackedWorldCorpse(bh) || !TryGetGuid(bh, out string guid))
                    {
                        return;
                    }

                    DebugLog($"Picked up {guid}");
                    SkyCoopClient.EntitySync.SendRemove(CorpsePolicy.c_Kind, guid, 1);
                }
                catch (Exception e)
                {
                    SkyCoop.Logger.Log(ConsoleColor.Red, $"[CorpseSync] Pickup removal failed: {e.GetType().Name}: {e.Message}");
                }
            }
        }

        private static HashSet<int> SnapshotGearIds()
        {
            HashSet<int> ids = new HashSet<int>();
            for (int i = 0; i < GearManager.m_Gear.Count; i++)
            {
                GearItem gear = GearManager.m_Gear[i];
                if (gear != null)
                {
                    ids.Add(gear.GetInstanceID());
                }
            }
            return ids;
        }
    }
}
