namespace SkyCoopServer
{
    public static class ProtocolCheck
    {
        public static bool IsCompatible(int? clientVersion, out string reason)
        {
            if (clientVersion == null)
            {
                reason = $"Client too old: update SkyCoop (server protocol {Packet.c_ProtocolVersion}).";
                return false;
            }

            if (clientVersion < Packet.c_ProtocolVersion)
            {
                reason = $"Client too old: update SkyCoop (client {clientVersion}, server {Packet.c_ProtocolVersion}).";
                return false;
            }

            if (clientVersion > Packet.c_ProtocolVersion)
            {
                reason = $"Server too old: update the server (client {clientVersion}, server {Packet.c_ProtocolVersion}).";
                return false;
            }

            reason = "";
            return true;
        }
    }
}
