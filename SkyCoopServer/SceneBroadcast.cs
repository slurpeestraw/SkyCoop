using LiteNetLib;
using System;
using System.Collections.Generic;

namespace SkyCoopServer
{
    public static class SceneBroadcast
    {
        public static List<int> Recipients(IEnumerable<DataStr.PlayerData> players, string scene, int excludePlayerId = -1)
        {
            List<int> recipients = new List<int>();

            if (!SceneNames.IsGameplay(scene) || players == null)
            {
                return recipients;
            }

            foreach (DataStr.PlayerData player in players)
            {
                if (player == null)
                {
                    continue;
                }

                if (player.m_Scene == scene && player.m_PlayerID != excludePlayerId)
                {
                    recipients.Add(player.m_PlayerID);
                }
            }

            return recipients;
        }

        public static void ToScene(Server server, string scene, Action<NetPeer> send, int excludePlayerId = -1)
        {
            if (server == null || server.m_Instance == null || send == null)
            {
                return;
            }

            List<NetPeer> peers = new List<NetPeer>();
            server.m_Instance.GetConnectedPeers(peers);

            List<DataStr.PlayerData> players = new List<DataStr.PlayerData>();
            foreach (NetPeer peer in peers)
            {
                DataStr.PlayerData data = server.GetPlayerDataByNetPeer(peer);
                if (data != null)
                {
                    players.Add(data);
                }
            }

            foreach (int playerId in Recipients(players, scene, excludePlayerId))
            {
                NetPeer? peer = server.GetClient(playerId);
                if (peer != null)
                {
                    send(peer);
                }
            }
        }
    }
}
