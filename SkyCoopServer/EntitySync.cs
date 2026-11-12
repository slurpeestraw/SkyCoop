using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SkyCoopServer
{
    public static class DebugEntityAlias
    {
        public const string Kind = "debug.marker";

        public static string ToGuid(string alias)
        {
            alias = string.IsNullOrWhiteSpace(alias) ? "a" : alias.Trim().ToLowerInvariant();
            return "debug." + alias;
        }
    }

    public static class EntitySync
    {
        private static readonly TimeSpan c_PendingUploadTimeout = TimeSpan.FromSeconds(10);

        public static bool Create(
            Server server,
            string scene,
            string kind,
            string guid,
            string prefab,
            Vector3 position,
            Quaternion rotation,
            int preferredUploader)
        {
            if (server == null || !SceneNames.IsGameplay(scene))
            {
                return false;
            }

            DataStr.SceneData? sceneData = server.m_ScenesData.GetSceneData(scene);
            if (sceneData == null)
            {
                Logger.Log(ConsoleColor.Yellow, $"[EntitySync] Cannot create {kind} {guid}: scene {scene} is not loaded");
                return false;
            }

            if (sceneData.m_Entities.ContainsKey(guid))
            {
                Logger.Log(ConsoleColor.Yellow, $"[EntitySync] Entity {kind} {guid} already exists in {scene}");
                return false;
            }

            ReplicatedEntity entity = new ReplicatedEntity
            {
                m_Kind = kind ?? "",
                m_GUID = guid ?? "",
                m_Prefab = prefab ?? "",
                m_State = "",
                m_Position = position,
                m_Rotation = rotation,
                m_PendingSinceUtc = DateTime.UtcNow,
            };

            List<int> playersInScene = PlayersInScene(server, scene);
            if (playersInScene.Contains(preferredUploader))
            {
                entity.m_Uploader = preferredUploader;
            }
            else
            {
                entity.m_Uploader = preferredUploader;
                entity.m_Uploader = EntityRules.PickUploader(
                    entity, playersInScene, DateTime.UtcNow, c_PendingUploadTimeout) ?? -1;
            }

            sceneData.m_Entities[guid] = entity;
            Logger.Log($"[EntitySync] Created {kind} {guid} in {scene}, uploader {entity.m_Uploader}");

            int uploader = entity.m_Uploader;
            SceneBroadcast.ToScene(server, scene, peer =>
                ServerSend.SendEntityState(peer, ToMessage(entity, peer.Id == uploader)));
            return true;
        }

        public static void HandleClientState(NetPeer client, NetDataReader reader, Server server)
        {
            EntityMessage message = reader.GetEntityMessage();
            DataStr.PlayerData? player = server.GetPlayerDataByNetPeer(client);
            if (player == null)
            {
                return;
            }

            string scene = player.m_Scene;
            DataStr.SceneData? sceneData = server.m_ScenesData.GetSceneData(scene);
            if (sceneData == null || !sceneData.m_Entities.TryGetValue(message.GUID, out ReplicatedEntity? entity))
            {
                Logger.Log(ConsoleColor.DarkGray, $"[EntitySync] Rejected state for unknown {message.Kind} {message.GUID} from {client.Id}");
                return;
            }

            List<DataStr.PlayerData> connected = ConnectedPlayers(server);
            int? lockHolder = InteractionLocks.HolderOf(connected, message.GUID);

            UpdateDecision decision = EntityRules.Evaluate(
                entity,
                player.m_Scene,
                scene,
                client.Id,
                lockHolder,
                message.Flags);

            if (decision == UpdateDecision.Reject)
            {
                Logger.Log(ConsoleColor.DarkGray, $"[EntitySync] Rejected state for {entity.m_Kind} {entity.m_GUID} from {client.Id}");
                return;
            }

            entity.m_State = message.State ?? "";
            entity.m_Prefab = message.Prefab ?? "";
            entity.m_Position = message.Position;
            entity.m_Rotation = message.Rotation;

            EntityMessage accepted = ToMessage(entity, false, message.Flags, message.Reason);
            SceneBroadcast.ToScene(server, scene, peer =>
                ServerSend.SendEntityState(peer, accepted), client.Id);

            if (decision == UpdateDecision.AcceptAndRemove)
            {
                sceneData.m_Entities.Remove(entity.m_GUID);
                string kind = entity.m_Kind;
                string guid = entity.m_GUID;
                SceneBroadcast.ToScene(server, scene, peer =>
                    ServerSend.SendEntityRemoved(peer, kind, guid));
            }
        }

        public static void HandleClientRemove(NetPeer client, NetDataReader reader, Server server)
        {
            string kind = reader.GetString();
            string guid = reader.GetString();
            int reason = reader.GetInt();

            DataStr.PlayerData? player = server.GetPlayerDataByNetPeer(client);
            if (player == null)
            {
                return;
            }

            List<DataStr.PlayerData> connected = ConnectedPlayers(server);
            int? lockHolder = InteractionLocks.HolderOf(connected, guid);
            if (lockHolder != client.Id)
            {
                Logger.Log(ConsoleColor.DarkGray, $"[EntitySync] Rejected remove of {kind} {guid} from {client.Id} (holder {lockHolder?.ToString() ?? "none"}, reason {reason})");
                return;
            }

            DataStr.SceneData? sceneData = server.m_ScenesData.GetSceneData(player.m_Scene);
            if (sceneData == null || !sceneData.m_Entities.TryGetValue(guid, out ReplicatedEntity? entity))
            {
                return;
            }

            sceneData.m_Entities.Remove(guid);
            server.m_PlayersData.SetPlayerInteractionGUID(player, "");

            Logger.Log($"[EntitySync] Removed {entity.m_Kind} {entity.m_GUID} by {client.Id}, reason {reason}");
            string entityKind = entity.m_Kind;
            string entityGuid = entity.m_GUID;
            SceneBroadcast.ToScene(server, player.m_Scene, peer =>
                ServerSend.SendEntityRemoved(peer, entityKind, entityGuid));
        }

        public static void SendAll(Server server, string scene, NetPeer client)
        {
            DataStr.SceneData? sceneData = server.m_ScenesData.GetSceneData(scene);
            if (sceneData == null || client == null)
            {
                return;
            }

            foreach (ReplicatedEntity entity in sceneData.m_Entities.Values.ToList())
            {
                if (entity == null)
                {
                    continue;
                }

                if (entity.IsPending)
                {
                    int? newUploader = EntityRules.PickUploader(
                        entity, PlayersInScene(server, scene), DateTime.UtcNow, c_PendingUploadTimeout);

                    if (newUploader != null)
                    {
                        entity.m_Uploader = newUploader.Value;
                        entity.m_PendingSinceUtc = DateTime.UtcNow;
                    }
                }

                ServerSend.SendEntityState(client, ToMessage(entity, entity.m_Uploader == client.Id));
            }
        }

        public static void TickPendingUploads(Server server)
        {
            foreach (KeyValuePair<string, DataStr.SceneData> scene in server.m_ScenesData.m_LoadedScenes.ToList())
            {
                if (!SceneNames.IsGameplay(scene.Key) || scene.Value == null)
                {
                    continue;
                }

                List<int> playersInScene = PlayersInScene(server, scene.Key);

                foreach (ReplicatedEntity entity in scene.Value.m_Entities.Values.ToList())
                {
                    if (entity == null || !entity.IsPending)
                    {
                        continue;
                    }

                    int? newUploader = EntityRules.PickUploader(
                        entity, playersInScene, DateTime.UtcNow, c_PendingUploadTimeout);

                    if (newUploader == null || newUploader == entity.m_Uploader)
                    {
                        continue;
                    }

                    entity.m_Uploader = newUploader.Value;
                    entity.m_PendingSinceUtc = DateTime.UtcNow;

                    NetPeer? peer = server.GetClient(entity.m_Uploader);
                    if (peer != null)
                    {
                        Logger.Log($"[EntitySync] {entity.m_Kind} {entity.m_GUID} uploader reassigned to {entity.m_Uploader}");
                        ServerSend.SendEntityState(peer, ToMessage(entity, true));
                    }
                }
            }
        }

        private static EntityMessage ToMessage(
            ReplicatedEntity entity,
            bool isUploader,
            EntityFlags flags = EntityFlags.None,
            int reason = 0)
        {
            return new EntityMessage
            {
                Kind = entity.m_Kind,
                GUID = entity.m_GUID,
                Prefab = entity.m_Prefab,
                State = entity.m_State,
                Position = entity.m_Position,
                Rotation = entity.m_Rotation,
                Flags = flags,
                IsUploader = isUploader,
                Reason = reason,
            };
        }

        private static List<int> PlayersInScene(Server server, string scene)
        {
            List<int> ids = new List<int>();
            foreach (DataStr.PlayerData player in server.m_PlayersData.GetPlayersOnScene(scene))
            {
                if (player != null)
                {
                    ids.Add(player.m_PlayerID);
                }
            }
            return ids;
        }

        private static List<DataStr.PlayerData> ConnectedPlayers(Server server)
        {
            List<DataStr.PlayerData> players = new List<DataStr.PlayerData>();
            List<NetPeer> peers = new List<NetPeer>();
            server.m_Instance.GetConnectedPeers(peers);

            foreach (NetPeer peer in peers)
            {
                DataStr.PlayerData? data = server.GetPlayerDataByNetPeer(peer);
                if (data != null)
                {
                    players.Add(data);
                }
            }

            return players;
        }
    }
}
