using Il2Cpp;
using Il2CppTLD.PDID;
using SkyCoop;
using SkyCoopServer;
using System;
using UnityEngine;

namespace SkyCoopClient
{
    public class CorpseAdapter : IEntityAdapter
    {
        public static bool s_SpawningCorpse = false;

        public string Kind => CorpsePolicy.c_Kind;

        public GameObject? Find(string guid)
        {
            GameObject? obj = PdidTable.GetGameObject(guid);
            if (obj == null)
            {
                return null;
            }

            SkyCoop.Comps.BaseAiHook? hook = obj.GetComponent<SkyCoop.Comps.BaseAiHook>();
            if (hook != null)
            {
                if (!hook.IsLocalCorpse)
                {
                    return null;
                }

                UnityEngine.Object.Destroy(hook);
            }

            EnableCorpseHarvest(obj);
            return obj;
        }

        public GameObject? Spawn(in EntityMessage msg)
        {
            GameObject? existing = PdidTable.GetGameObject(msg.GUID);
            if (existing != null)
            {
                AnimalsManager.HandleRemove(existing, "(corpse replace)");
            }

            GameObject? prefab = AssetManager.GetAssetFromGame<GameObject>(msg.Prefab);
            if (prefab == null)
            {
                SkyCoop.Logger.Log(ConsoleColor.Red, $"[CorpseAdapter] Missing prefab {msg.Prefab}");
                return null;
            }

            s_SpawningCorpse = true;
            GameObject obj;
            try
            {
                obj = UnityEngine.Object.Instantiate(
                    prefab,
                    msg.Position.ConvertToUnity(),
                    msg.Rotation.ConvertToUnity());
            }
            finally
            {
                s_SpawningCorpse = false;
            }

            obj.name = msg.Prefab;

            ObjectGuid? objectGuid = obj.GetComponent<ObjectGuid>();
            if (objectGuid == null)
            {
                objectGuid = obj.AddComponent<ObjectGuid>();
            }
            PdidTable.RuntimeRegister(objectGuid, msg.GUID);

            BaseAi? ai = obj.GetComponent<BaseAi>();
            if (ai != null)
            {
                ai.m_CurrentHP = 0;
                ai.SetAiMode(AiMode.Dead);
                ai.enabled = false;
                ApplyCorpseRestore(ai);
            }

            EnableCorpseHarvest(obj);
            SkyCoop.Logger.Log(ConsoleColor.Cyan, $"[CorpseAdapter] Spawned {msg.Prefab} {msg.GUID}");
            return obj;
        }

        public bool TryCapture(GameObject obj, out string prefab, out string stateJson, out EntityFlags flags)
        {
            prefab = "";
            stateJson = "";
            flags = EntityFlags.None;

            BodyHarvest? bh = GetBodyHarvest(obj);
            if (bh == null)
            {
                return false;
            }

            prefab = SpawnRegion.GetPrefabNameFromInstanceName(obj.name);
            if (string.IsNullOrEmpty(prefab))
            {
                prefab = obj.name;
            }

            stateJson = bh.Serialize();
            if (bh.NoMoreResources() || bh.ConditionReachedZero())
            {
                flags = EntityFlags.Final;
            }

            return !string.IsNullOrEmpty(stateJson);
        }

        public void Apply(GameObject obj, string stateJson)
        {
            BodyHarvest? bh = GetBodyHarvest(obj);
            if (bh == null)
            {
                return;
            }

            bh.Deserialize(stateJson, false);
            EnableCorpseHarvest(obj);
        }

        public static void EnableCorpseHarvest(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            BaseAi? ai = obj.GetComponent<BaseAi>();
            if (ai != null && ai.m_BodyHarvest != null)
            {
                ai.m_BodyHarvest.enabled = true;
            }

            BodyHarvest[] bodyHarvests = obj.GetComponentsInChildren<BodyHarvest>(true);
            for (int i = 0; i < bodyHarvests.Length; i++)
            {
                if (bodyHarvests[i] != null)
                {
                    bodyHarvests[i].enabled = true;
                }
            }

            BodyHarvestInteraction[] interactions = obj.GetComponentsInChildren<BodyHarvestInteraction>(true);
            for (int i = 0; i < interactions.Length; i++)
            {
                if (interactions[i] != null)
                {
                    interactions[i].enabled = true;
                }
            }
        }

        public void SetInteractable(GameObject obj, bool interactable)
        {
            BodyHarvestInteraction[] interactions = obj.GetComponentsInChildren<BodyHarvestInteraction>(true);
            for (int i = 0; i < interactions.Length; i++)
            {
                if (interactions[i] != null)
                {
                    interactions[i].enabled = interactable;
                }
            }
        }

        public void Remove(GameObject obj)
        {
            CloseOpenPanel(obj);
            AnimalsManager.HandleRemove(obj, "(corpse removed by server)");
        }

        private static BodyHarvest? GetBodyHarvest(GameObject obj)
        {
            return obj.GetComponentInChildren<BodyHarvest>(true);
        }

        private static void ApplyCorpseRestore(BaseAi ai)
        {
            ai.m_ForceToCorpse = true;
            ai.ReparentBaseAi(null, true);

            MoveAgent agent = ai.GetMoveAgent();
            if (agent != null && agent.m_Agent != null)
            {
                agent.m_Agent.enabled = false;
                agent.m_Enabled = false;
            }
        }

        private static void CloseOpenPanel(GameObject obj)
        {
            BodyHarvest? bh = GetBodyHarvest(obj);
            if (bh == null)
            {
                return;
            }

            CorpseSync.ForceClosePanel(bh, true);
        }
    }
}
