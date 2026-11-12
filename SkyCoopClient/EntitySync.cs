using LiteNetLib.Utils;
using MelonLoader;
using SkyCoop;
using SkyCoopServer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SkyCoopServer.DataStr;

namespace SkyCoopClient
{
    public interface IEntityAdapter
    {
        string Kind { get; }
        GameObject? Find(string guid);
        GameObject? Spawn(in EntityMessage msg);
        bool TryCapture(GameObject obj, out string prefab, out string stateJson, out EntityFlags flags);
        void Apply(GameObject obj, string stateJson);
        void SetInteractable(GameObject obj, bool interactable);
        void Remove(GameObject obj);
    }

    public static class EntitySync
    {
        private static readonly Dictionary<string, IEntityAdapter> s_Adapters =
            new Dictionary<string, IEntityAdapter>(StringComparer.Ordinal);

        public static bool s_ApplyingRemote = false;

        public static void Register(IEntityAdapter adapter)
        {
            if (adapter == null || string.IsNullOrEmpty(adapter.Kind))
            {
                return;
            }

            s_Adapters[adapter.Kind] = adapter;
        }

        public static bool CanSend()
        {
            return ModMain.IsMultiplayer() &&
                   ModMain.Client != null &&
                   ModMain.Client.m_IsReady &&
                   !Minimalizer.s_LoadingFlag &&
                   Minimalizer.s_FramesDelayBeforeSendNewScene == 0 &&
                   ModMain.IsGameplayScene();
        }

        public static void SendState(string kind, string guid, GameObject obj)
        {
            if (obj == null || string.IsNullOrEmpty(kind) || string.IsNullOrEmpty(guid))
            {
                return;
            }

            if (!CanSend() || s_ApplyingRemote)
            {
                return;
            }

            if (!s_Adapters.TryGetValue(kind, out IEntityAdapter? adapter))
            {
                SkyCoop.Logger.Log(ConsoleColor.Yellow, $"[EntitySync] No adapter for {kind}");
                return;
            }

            try
            {
                if (!adapter.TryCapture(obj, out string prefab, out string stateJson, out EntityFlags flags))
                {
                    return;
                }

                EntityMessage message = new EntityMessage
                {
                    Kind = kind,
                    GUID = guid,
                    Prefab = prefab,
                    State = DataStr.CompressString(stateJson),
                    Position = obj.transform.position.ConvertToSystem(),
                    Rotation = obj.transform.rotation.ConvertToSystem(),
                    Flags = flags,
                };

                ClientSend.SendEntityState(message);
            }
            catch (Exception e)
            {
                SkyCoop.Logger.Log(ConsoleColor.Red, $"[EntitySync] SendState {kind} {guid} failed: {e.GetType().Name}: {e.Message}");
            }
        }

        public static void SendRemove(string kind, string guid, int reason)
        {
            if (!CanSend() || s_ApplyingRemote || string.IsNullOrEmpty(kind) || string.IsNullOrEmpty(guid))
            {
                return;
            }

            try
            {
                ClientSend.SendEntityRemove(kind, guid, reason);
            }
            catch (Exception e)
            {
                SkyCoop.Logger.Log(ConsoleColor.Red, $"[EntitySync] SendRemove {kind} {guid} failed: {e.GetType().Name}: {e.Message}");
            }
        }

        public static void HandleStatePacket(NetDataReader reader)
        {
            try
            {
                HandleState(reader.GetEntityMessage());
            }
            catch (Exception e)
            {
                SkyCoop.Logger.Log(ConsoleColor.Red, $"[EntitySync] HandleState failed: {e.GetType().Name}: {e.Message}");
            }
        }

        public static void HandleRemovedPacket(NetDataReader reader)
        {
            try
            {
                string kind = reader.GetString();
                string guid = reader.GetString();
                HandleRemoved(kind, guid);
            }
            catch (Exception e)
            {
                SkyCoop.Logger.Log(ConsoleColor.Red, $"[EntitySync] HandleRemoved failed: {e.GetType().Name}: {e.Message}");
            }
        }

        public static void HandleState(in EntityMessage message)
        {
            if (Minimalizer.s_LoadingFlag || !ModMain.IsGameplayScene())
            {
                return;
            }

            if (!s_Adapters.TryGetValue(message.Kind, out IEntityAdapter? adapter))
            {
                SkyCoop.Logger.Log(ConsoleColor.Yellow, $"[EntitySync] No adapter for {message.Kind}");
                return;
            }

            try
            {
                GameObject? obj = adapter.Find(message.GUID);
                if (obj == null)
                {
                    obj = adapter.Spawn(message);
                }

                if (obj == null)
                {
                    SkyCoop.Logger.Log(ConsoleColor.Red, $"[EntitySync] Could not spawn {message.Kind} {message.GUID}");
                    return;
                }

                if (!string.IsNullOrEmpty(message.State))
                {
                    string stateJson = DataStr.DecompressString(message.State);

                    s_ApplyingRemote = true;
                    try
                    {
                        adapter.Apply(obj, stateJson);
                        obj.transform.position = message.Position.ConvertToUnity();
                        obj.transform.rotation = message.Rotation.ConvertToUnity();
                        adapter.SetInteractable(obj, true);
                    }
                    finally
                    {
                        s_ApplyingRemote = false;
                    }
                }
                else if (message.IsUploader)
                {
                    MelonCoroutines.Start(SendStateNextFrame(message.Kind, message.GUID));
                }
                else
                {
                    adapter.SetInteractable(obj, false);
                }
            }
            catch (Exception e)
            {
                s_ApplyingRemote = false;
                SkyCoop.Logger.Log(ConsoleColor.Red, $"[EntitySync] Applying {message.Kind} {message.GUID} failed: {e.GetType().Name}: {e.Message}");
            }
        }

        public static void HandleRemoved(string kind, string guid)
        {
            if (string.IsNullOrEmpty(kind) || string.IsNullOrEmpty(guid))
            {
                return;
            }

            if (!s_Adapters.TryGetValue(kind, out IEntityAdapter? adapter))
            {
                return;
            }

            try
            {
                GameObject? obj = adapter.Find(guid);
                if (obj != null)
                {
                    adapter.Remove(obj);
                }
            }
            catch (Exception e)
            {
                SkyCoop.Logger.Log(ConsoleColor.Red, $"[EntitySync] Removing {kind} {guid} failed: {e.GetType().Name}: {e.Message}");
            }
        }

        private static IEnumerator SendStateNextFrame(string kind, string guid)
        {
            yield return null;

            GameObject? obj = null;
            IEntityAdapter? adapter = null;
            try
            {
                if (s_Adapters.TryGetValue(kind, out adapter))
                {
                    obj = adapter.Find(guid);
                }
            }
            catch (Exception e)
            {
                SkyCoop.Logger.Log(ConsoleColor.Red, $"[EntitySync] Delayed send lookup {kind} {guid} failed: {e.GetType().Name}: {e.Message}");
            }

            if (obj != null)
            {
                SendState(kind, guid, obj);
            }
        }
    }
}
