using System.Collections.Generic;

namespace SkyCoopServer
{
    public static class InteractionLocks
    {
        public static int? HolderOf(IEnumerable<DataStr.PlayerData> players, string guid)
        {
            if (string.IsNullOrEmpty(guid) || players == null)
            {
                return null;
            }

            foreach (DataStr.PlayerData player in players)
            {
                if (player == null)
                {
                    continue;
                }

                if (player.m_InteractionGUID == guid || player.m_CarSeat == guid)
                {
                    return player.m_PlayerID;
                }
            }

            return null;
        }

        public static bool IsBlocked(IEnumerable<DataStr.PlayerData> players, string guid, int requesterId)
        {
            int? holder = HolderOf(players, guid);
            return holder != null && holder != requesterId;
        }
    }
}
