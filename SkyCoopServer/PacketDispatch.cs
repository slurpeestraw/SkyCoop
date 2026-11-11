using System;

namespace SkyCoopServer
{
    public static class PacketDispatch
    {
        public static bool Invoke(int packetId, Action handler, Action<string> log)
        {
            try
            {
                handler();
                return true;
            }
            catch (Exception ex)
            {
                LogFailure(() => Describe(packetId), ex, log);
                return false;
            }
        }

        public static string Describe(int packetId)
        {
            return Enum.IsDefined(typeof(Packet.Type), packetId)
                ? ((Packet.Type)packetId).ToString()
                : $"Unknown({packetId})";
        }

        public static bool Guard(string context, Action action, Action<string> log)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                LogFailure(() => context, ex, log);
                return false;
            }
        }

        private static void LogFailure(Func<string> context, Exception ex, Action<string> log)
        {
            try
            {
                log?.Invoke($"[Dispatch] {context()} failed: {ex.GetType().Name}: {ex.Message}");
            }
            catch
            {
            }
        }
    }
}
