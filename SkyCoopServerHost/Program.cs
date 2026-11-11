using System;
using System.Text;
using System.Threading;
using SkyCoopServer;

namespace SkyCoopServerHost
{
    internal static class Program
    {
        private static readonly ManualResetEventSlim s_Exit = new ManualResetEventSlim(false);
        private static Server? s_Server;

        private static int Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch
            {
            }

            Server.OnLogEvent += OnLog;
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                s_Exit.Set();
            };
            AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown();

            try
            {
                FilesManager.InitFolders();

                DataStr.ServerConfig config = FilesManager.LoadServerCFG() ?? new DataStr.ServerConfig();

                s_Server = new Server(config);
                s_Server.StartServer(config.m_Port, config.m_MaxPlayers);

                Logger.Log(ConsoleColor.Green, $"[Host] Server is running on port {config.m_Port}. Press Ctrl+C to stop.");

                while (!s_Exit.Wait(15))
                {
                    s_Server.Update();
                }

                return 0;
            }
            catch (Exception exception)
            {
                Logger.Log(ConsoleColor.Red, $"[Host] Fatal error: {exception}");
                return 1;
            }
            finally
            {
                Shutdown();
            }
        }

        private static void OnLog(Logger.LogData data)
        {
            try
            {
                Console.ForegroundColor = data.m_Color;
                Console.WriteLine(data.m_Message);
                Console.ResetColor();
            }
            catch
            {
                Console.WriteLine(data.m_Message);
            }
        }

        private static void Shutdown()
        {
            Server? server = Interlocked.Exchange(ref s_Server, null);

            if (server == null)
            {
                return;
            }

            try
            {
                if (server.m_IsReady)
                {
                    Logger.Log(ConsoleColor.Yellow, "[Host] Saving world and stopping server...");
                    server.SaveToFile();
                    server.DisconnectAllPlayers("Server shutdown", true);
                }
                else
                {
                    server.Dispose();
                }
            }
            catch (Exception exception)
            {
                Logger.Log(ConsoleColor.Red, $"[Host] Error during shutdown: {exception.Message}");
            }
        }
    }
}
