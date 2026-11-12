using Il2Cpp;
using Il2CppTLD.PDID;
using SkyCoop;
using SkyCoopServer;
using System;
using System.Text.Json;
using UnityEngine;

namespace SkyCoopClient
{
    public class DebugMarkerInteract : MonoBehaviour
    {
        public DebugMarkerInteract(IntPtr ptr) : base(ptr) { }

        public string m_GUID = "";
        public string m_PendingAction = "bump";
    }

    public class DebugMarkerAdapter : IEntityAdapter
    {
        public const string c_Kind = "debug.marker";

        private sealed class MarkerState
        {
            public int n { get; set; }
        }

        public string Kind => c_Kind;

        public GameObject? Find(string guid)
        {
            return PdidTable.GetGameObject(guid);
        }

        public GameObject? Spawn(in EntityMessage msg)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "SkyCoopDebugMarker";
            obj.transform.position = msg.Position.ConvertToUnity();
            obj.transform.rotation = msg.Rotation.ConvertToUnity();

            ObjectGuid? objectGuid = obj.GetComponent<ObjectGuid>();
            if (objectGuid == null)
            {
                objectGuid = obj.AddComponent<ObjectGuid>();
            }
            PdidTable.RuntimeRegister(objectGuid, msg.GUID);

            DebugMarkerInteract marker = obj.AddComponent<DebugMarkerInteract>();
            marker.m_GUID = msg.GUID;

            return obj;
        }

        public bool TryCapture(GameObject obj, out string prefab, out string stateJson, out EntityFlags flags)
        {
            prefab = c_Kind;
            flags = EntityFlags.None;

            MarkerState state = new MarkerState { n = ReadCount(obj) };
            stateJson = JsonSerializer.Serialize(state);
            return true;
        }

        public void Apply(GameObject obj, string stateJson)
        {
            MarkerState? state = null;
            try
            {
                state = JsonSerializer.Deserialize<MarkerState>(stateJson);
            }
            catch (Exception e)
            {
                SkyCoop.Logger.Log(ConsoleColor.Red, $"[DebugMarkerAdapter] Bad state {stateJson}: {e.Message}");
            }

            int n = state != null ? Mathf.Max(0, state.n) : 0;
            obj.transform.localScale = Vector3.one * (1f + n * 0.1f);
        }

        public void SetInteractable(GameObject obj, bool interactable)
        {
            Collider? collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = interactable;
            }
        }

        public void Remove(GameObject obj)
        {
            UnityEngine.Object.Destroy(obj);
        }

        public static void Bump(string guid)
        {
            GameObject? obj = PdidTable.GetGameObject(guid);
            if (obj == null)
            {
                return;
            }

            int n = ReadCount(obj) + 1;
            obj.transform.localScale = Vector3.one * (1f + n * 0.1f);
            EntitySync.SendState(c_Kind, guid, obj);
        }

        private static int ReadCount(GameObject obj)
        {
            return Mathf.Max(0, Mathf.RoundToInt((obj.transform.localScale.x - 1f) / 0.1f));
        }
    }
}
