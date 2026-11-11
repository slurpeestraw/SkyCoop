using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using static SkyCoopServer.DataStr;

namespace SkyCoopServer
{
    public class FilesManager
    {
        // Main Data folder
        public static string s_DataDirectory = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/SkyModData";

        // Save Data folder
        public static string s_SaveDirectory = "";

        // Sub folders
        public static string s_GameModesDirectory = "GameModes";
        public static string s_LootTablesDirectory = "LootTables";
        public static string s_MapsDirectory = "Maps";
        public static string s_WeatherDirectory = "Weather";

        public static string s_ServerConfigLocation = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Server.json";

        public static void InitFolders(bool SkipDefaultSaveFolder = false)
        {
            if (!Directory.Exists(s_DataDirectory))
            {
                Directory.CreateDirectory(s_DataDirectory);
            }
            if (!Directory.Exists($"{s_DataDirectory}/{s_GameModesDirectory}"))
            {
                Directory.CreateDirectory($"{s_DataDirectory}/{s_GameModesDirectory}");
            }
            if (!Directory.Exists($"{s_DataDirectory}/{s_MapsDirectory}"))
            {
                Directory.CreateDirectory($"{s_DataDirectory}/{s_MapsDirectory}");
            }
            if (!Directory.Exists($"{s_DataDirectory}/{s_LootTablesDirectory}"))
            {
                Directory.CreateDirectory($"{s_DataDirectory}/{s_LootTablesDirectory}");
            }

            if (!SkipDefaultSaveFolder)
            {
                if (string.IsNullOrEmpty(s_SaveDirectory))
                {
                    s_SaveDirectory = $"{s_DataDirectory}/ServerSaves";
                }

                if (!Directory.Exists(s_SaveDirectory))
                {
                    Directory.CreateDirectory(s_SaveDirectory);
                }
            }
        }

        public static void SetSavesFolder(string Path)
        {
            s_SaveDirectory = $"{Path}/ServerSaves";
            Logger.Log($"[FilesManager] Saves folder set to {s_SaveDirectory}");
            if (!Directory.Exists(s_SaveDirectory))
            {
                Directory.CreateDirectory(s_SaveDirectory);
            }
        }

        public static List<DataStr.MinimalPlayersAndGameMode> GetGameModesList()
        {
            List <DataStr.MinimalPlayersAndGameMode> GameModes = new List<MinimalPlayersAndGameMode>();
            string _Path = $"{s_DataDirectory}/{s_GameModesDirectory}";

            if (Directory.Exists(_Path))
            {
                foreach (string FilePath in Directory.GetFiles(_Path))
                {
                    string FileName = Path.GetFileName(FilePath);
                    DataStr.GameRules Rule = GetRules(FileName);
                    DataStr.MinimalPlayersAndGameMode GameMode = new MinimalPlayersAndGameMode();

                    GameMode.GameModeName = Rule.m_HUDMode;
                    GameMode.MinimalPlayers = Rule.m_MinimalPlayersToPlay;

                    GameModes.Add(GameMode);
                }
            }
            return GameModes;
        }

        public static List<GameRules> GetGameRulesList()
        {
            List<GameRules> Rules = new List<GameRules>();
            string _Path = $"{s_DataDirectory}/{s_GameModesDirectory}";

            if (Directory.Exists(_Path))
            {
                foreach (string FilePath in Directory.GetFiles(_Path))
                {
                    string FileName = Path.GetFileName(FilePath);
                    Rules.Add(GetRules(FileName));
                }
            }
            return Rules;
        }

        public static GameRules GetRules(string GameMode)
        {
            string Path = $"{s_DataDirectory}/{s_GameModesDirectory}/{GameMode}";
            string JSON = "";

            Logger.Log($"[FilesManager] Loading file {Path}");
            if (File.Exists(Path))
            {
                try
                {
                    JSON = File.ReadAllText(Path);
                }
                catch (Exception e)
                {
                    Logger.Log($"[FilesManager] Failed to load {Path}: {e.Message}");
                    return new GameRules();
                }
            }
            else
            {
                Logger.Log($"[FilesManager] File {Path} not exist");
            }

            if (string.IsNullOrEmpty(JSON))
            {
                Logger.Log($"[FilesManager] File {Path} is empty");
                return new GameRules();
            }
            GameRulesJson JsonData = JsonSerializer.Deserialize<GameRulesJson>(JSON);
            return JsonData == null ? new GameRules() : JsonData.Load(GameMode);
        }


        public static List<string> GetMapsList()
        {
            string path = $"{s_DataDirectory}/{s_MapsDirectory}";

            if (Directory.Exists(path))
            {
                List<string> FileNames = new List<string>(Directory.GetFiles(path));
                for (int i = 0; i < FileNames.Count; i++)
                {
                    FileNames[i] = Path.GetFileName(FileNames[i]);
                }
                return FileNames;
            }
            return new List<string>();
        }

        public static MapData GetMapData(string MapName)
        {
            string Path = $"{s_DataDirectory}/{s_MapsDirectory}/{MapName}";
            string JSON = "";

            Logger.Log($"[FilesManager] Loading file {Path}");
            if (File.Exists(Path))
            {
                try
                {
                    JSON = File.ReadAllText(Path);
                }
                catch (Exception e)
                {
                    Logger.Log($"[FilesManager] Failed to load {Path}: {e.Message}");
                    return null;
                }
            }
            else
            {
                Logger.Log($"[FilesManager] File {Path} not exist");
            }

            if (string.IsNullOrEmpty(JSON))
            {
                Logger.Log($"[FilesManager] File {Path} is empty");
                return null;
            }
            return JsonSerializer.Deserialize<MapData>(JSON);
        }

        public static string GetSceneByMapName(string MapName)
        {
            string Path = $"{s_DataDirectory}/{s_MapsDirectory}/{MapName}";
            string JSON = "";

            Logger.Log($"[FilesManager] Loading file {Path}");
            if (File.Exists(Path))
            {
                try
                {
                    JSON = File.ReadAllText(Path);
                }
                catch (Exception e)
                {
                    Logger.Log($"[FilesManager] Failed to load {Path}: {e.Message}");
                    return "";
                }
            }
            else
            {
                Logger.Log($"[FilesManager] File {Path} not exist");
            }

            if (string.IsNullOrEmpty(JSON))
            {
                Logger.Log($"[FilesManager] File {Path} is empty");
                return "";
            }
            MapData JsonData = JsonSerializer.Deserialize<MapData>(JSON);
            return JsonData == null ? "" : JsonData.Scene;
        }

        public static Dictionary<string, PrefabTableJSON> GetAllLootTables()
        {
            Dictionary<string, PrefabTableJSON> Dict = new Dictionary<string, PrefabTableJSON>();
            string _Path = $"{s_DataDirectory}/{s_LootTablesDirectory}";
            if (Directory.Exists(_Path))
            {
                string[] AllFiles = Directory.GetFiles(_Path, "*", SearchOption.AllDirectories);

                foreach (string FilePath in AllFiles)
                {
                    PrefabTableJSON Table = GetLootTable(FilePath);
                    if (Table != null)
                    {
                        string Name = Path.GetFileNameWithoutExtension(FilePath);
                        if (!Dict.ContainsKey(Name))
                        {
                            Dict.Add(Name, Table);
                        }
                    }
                }
            }
            return Dict;
        }

        public static PrefabTableJSON GetLootTable(string Path)
        {
            string JSON = "";

            if (LootTableManager.c_DebugLogs)
            {
                Logger.Log($"[FilesManager] Loading loot table {Path}");
            }

            
            if (File.Exists(Path))
            {
                try
                {
                    JSON = File.ReadAllText(Path);
                }
                catch (Exception e)
                {
                    Logger.Log($"[FilesManager] Failed to load {Path}: {e.Message}");
                    return null;
                }
            }
            else
            {
                Logger.Log($"[FilesManager] File {Path} not exist");
                return null;
            }

            if (string.IsNullOrEmpty(JSON))
            {
                Logger.Log($"[FilesManager] File {Path} is empty");
                return null;
            }
            return JsonSerializer.Deserialize<PrefabTableJSON>(JSON);
        }

        public static string GetAirDrop(string DropPath)
        {
            string Path = $"{s_DataDirectory}/{DropPath}";
            string JSON = "";

            Logger.Log($"[FilesManager] Loading file {Path}");
            if (File.Exists(Path))
            {
                try
                {
                    return File.ReadAllText(Path);
                }
                catch (Exception e)
                {
                    Logger.Log($"[FilesManager] Failed to load {Path}: {e.Message}");
                    return null;
                }
            }
            else
            {
                Logger.Log($"[FilesManager] File {Path} not exist");
                return null;
            }

            if (string.IsNullOrEmpty(JSON))
            {
                Logger.Log($"[FilesManager] File {Path} is empty");
                return null;
            }
            return null;
        }

        public static WeatherManager.WeatherSettingsConfig GetWeatherConfig(string Profile = "Default")
        {
            if (string.IsNullOrEmpty(Profile))
            {
                Profile = "Default";
            }
            
            string Path = $"{s_DataDirectory}/{s_WeatherDirectory}/{Profile}";
            string JSON = "";

            Logger.Log($"[FilesManager] Loading file {Path}");
            if (File.Exists(Path))
            {
                try
                {
                    JSON = File.ReadAllText(Path);
                }
                catch (Exception e)
                {
                    Logger.Log($"[FilesManager] Failed to load {Path}: {e.Message}");
                    return null;
                }
            }
            else
            {
                Logger.Log($"[FilesManager] File {Path} not exist");
                return null;
            }

            if (string.IsNullOrEmpty(JSON))
            {
                Logger.Log($"[FilesManager] File {Path} is empty");
                return null;
            }
            return JsonSerializer.Deserialize<WeatherManager.WeatherSettingsConfig>(JSON);
        }

        public static DataStr.ScenesLootSpawns GetGearsSpawnsData(string Path)
        {

            var options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };
            string _Path = $"{s_DataDirectory}/{Path}";
            string JSON = "";

            Logger.Log($"[FilesManager] Loading file {_Path}");
            if (File.Exists(_Path))
            {
                try
                {
                    JSON = File.ReadAllText(_Path);
                }
                catch (Exception e)
                {
                    Logger.Log($"[FilesManager] Failed to load {_Path}: {e.Message}");
                    return null;
                }
            }
            else
            {
                Logger.Log($"[FilesManager] File {_Path} not exist");
                return null;
            }

            if (string.IsNullOrEmpty(JSON))
            {
                Logger.Log($"[FilesManager] File {_Path} is empty");
                return null;
            }
            return JsonSerializer.Deserialize<ScenesLootSpawns>(JSON, options);
        }

        public static DataStr.ScenesAnimalSpawns GetAnimalsSpawnsData(string Path)
        {

            var options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };
            string _Path = $"{s_DataDirectory}/{Path}";
            string JSON = "";

            Logger.Log($"[FilesManager] Loading file {_Path}");
            if (File.Exists(_Path))
            {
                try
                {
                    JSON = File.ReadAllText(_Path);
                }
                catch (Exception e)
                {
                    Logger.Log($"[FilesManager] Failed to load {_Path}: {e.Message}");
                    return null;
                }
            }
            else
            {
                Logger.Log($"[FilesManager] File {_Path} not exist");
                return null;
            }

            if (string.IsNullOrEmpty(JSON))
            {
                Logger.Log($"[FilesManager] File {_Path} is empty");
                return null;
            }
            return JsonSerializer.Deserialize<ScenesAnimalSpawns>(JSON, options);
        }

        public static void WriteAllTextAtomic(string path, string contents)
        {
            string TmpPath = path + ".tmp";
            try
            {
                using (FileStream Stream = new FileStream(TmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (StreamWriter Writer = new StreamWriter(Stream))
                {
                    Writer.Write(contents);
                    Writer.Flush();
                    Stream.Flush(true);
                }

                if (File.Exists(path))
                {
                    File.Replace(TmpPath, path, path + ".bak");
                }
                else
                {
                    File.Move(TmpPath, path);
                }
            }
            catch
            {
                try
                {
                    if (File.Exists(TmpPath))
                    {
                        File.Delete(TmpPath);
                    }
                }
                catch
                {
                }
                throw;
            }
        }

        public static bool TryReadJson<T>(string path, out T? value)
        {
            value = default;

            if (!File.Exists(path))
            {
                return false;
            }

            if (TryDeserializeFile(path, out value))
            {
                return true;
            }

            Logger.Log($"[FilesManager] {path} is corrupt, trying {path}.bak");
            try
            {
                string CorruptPath = path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                if (File.Exists(CorruptPath))
                {
                    CorruptPath += "-" + Guid.NewGuid().ToString("N");
                }
                File.Move(path, CorruptPath);
            }
            catch (Exception e)
            {
                Logger.Log($"[FilesManager] Failed to quarantine {path}: {e.Message}");
            }

            if (TryDeserializeFile(path + ".bak", out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryDeserializeFile<T>(string path, out T? value)
        {
            value = default;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                string JSON = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(JSON))
                {
                    Logger.Log($"[FilesManager] File {path} is empty");
                    return false;
                }

                value = JsonSerializer.Deserialize<T>(JSON);
                return value != null;
            }
            catch (Exception e)
            {
                Logger.Log($"[FilesManager] Failed to load {path}: {e.Message}");
                return false;
            }
        }

        public static void SaveSceneToFile(SceneData.SaveData SaveData, Server ServerInstance)
        {
            JsonSerializerOptions Options = new JsonSerializerOptions();
            Options.WriteIndented = true;
            string JSON = JsonSerializer.Serialize<SceneData.SaveData>(SaveData, Options);
            try
            {
                string SeedName = ServerInstance.m_Config.m_Seed.ToString();

                if (!Directory.Exists($"{s_SaveDirectory}/{SeedName}"))
                {
                    Directory.CreateDirectory($"{s_SaveDirectory}/{SeedName}");
                }
                if (!Directory.Exists($"{s_SaveDirectory}/{SeedName}/Scenes"))
                {
                    Directory.CreateDirectory($"{s_SaveDirectory}/{SeedName}/Scenes");
                }
                
                WriteAllTextAtomic($"{s_SaveDirectory}/{SeedName}/Scenes/{SaveData.SceneName}", JSON);
                return;
            }
            catch (Exception e)
            {
                Logger.Log($"[FilesManager] Failed to save scene {SaveData.SceneName}: {e.Message}");
            }
        }

        public static SceneData.SaveData LoadSceneFromFile(string SceneName, Server ServerInstance)
        {
            string Path = $"{s_SaveDirectory}/{ServerInstance.m_Config.m_Seed.ToString()}/Scenes/{SceneName}";

            Logger.Log($"[FilesManager] Loading file {Path}");
            if (TryReadJson<SceneData.SaveData>(Path, out SceneData.SaveData? SaveData))
            {
                return SaveData;
            }

            Logger.Log($"[FilesManager] Failed to load {Path}");
            return null;
        }

        public static void SaveServerToFile(Server.SaveData SaveData, Server ServerInstance)
        {
            JsonSerializerOptions Options = new JsonSerializerOptions();
            Options.WriteIndented = true;
            string JSON = JsonSerializer.Serialize<Server.SaveData>(SaveData, Options);
            try
            {
                string SeedName = ServerInstance.m_Config.m_Seed.ToString();

                if (!Directory.Exists($"{s_SaveDirectory}/{SeedName}"))
                {
                    Directory.CreateDirectory($"{s_SaveDirectory}/{SeedName}");
                }

                WriteAllTextAtomic($"{s_SaveDirectory}/{SeedName}/ServerData", JSON);
                return;
            }
            catch (Exception e)
            {
                Logger.Log($"[FilesManager] Failed to save server data: {e.Message}");
            }
        }

        public static Server.SaveData LoadServerFromFile(int Seed)
        {
            string Path = $"{s_SaveDirectory}/{Seed.ToString()}/ServerData";

            Logger.Log($"[FilesManager] Loading file {Path}");
            if (TryReadJson<Server.SaveData>(Path, out Server.SaveData? SaveData))
            {
                return SaveData;
            }

            Logger.Log($"[FilesManager] Failed to load {Path}");
            return null;
        }

        public class SaveDataAndSeed
        {
            public int Seed;
            public Server.SaveData Save;

            public SaveDataAndSeed(int seed, Server.SaveData save)
            {
                Seed = seed;
                Save = save;
            }
        }

        public static List<SaveDataAndSeed> GetServerSavesList()
        {
            List<SaveDataAndSeed> Saves = new List<SaveDataAndSeed>();
            if (Directory.Exists(s_SaveDirectory))
            {
                List<string> FilesPaths = new List<string>(Directory.GetDirectories(s_SaveDirectory));
                for (int i = 0; i < FilesPaths.Count; i++)
                {
                    string FileName = Path.GetFileName(FilesPaths[i]);
                    int Seed = int.Parse(FileName);
                    Server.SaveData ServerSaveData = LoadServerFromFile(Seed);
                    if(ServerSaveData != null)
                    {
                        Saves.Add(new SaveDataAndSeed(Seed, ServerSaveData));
                    }
                }
            }
            return Saves;
        }

        public static void DeleteSave(string SaveName)
        {
            Logger.Log($"[FilesManager] Delete save {$"{s_SaveDirectory}/{SaveName}"}");
            if (Directory.Exists($"{s_SaveDirectory}/{SaveName}"))
            {
                Directory.Delete($"{s_SaveDirectory}/{SaveName}", true);
            }
        }

        public static DataStr.ServerConfig LoadServerCFG()
        {
            string Path = s_ServerConfigLocation;

            Logger.Log($"[FilesManager] Loading file {Path}");
            if (!TryReadJson<DataStr.ServerConfigJSON>(Path, out DataStr.ServerConfigJSON? Proxy) || Proxy == null)
            {
                Logger.Log($"[FilesManager] Failed to load {Path}");
                return null;
            }

            return Proxy.Load();
        }

        public static List<string> GetDupeList(string Path)
        {

            string _Path = $"{s_DataDirectory}/{Path}";
            string JSON = "";

            Logger.Log($"[FilesManager] Loading file {_Path}");
            if (File.Exists(_Path))
            {
                try
                {
                    JSON = File.ReadAllText(_Path);
                }
                catch (Exception e)
                {
                    Logger.Log($"[FilesManager] Failed to load {_Path}: {e.Message}");
                    return null;
                }
            }
            else
            {
                Logger.Log($"[FilesManager] File {_Path} not exist");
                return null;
            }

            if (string.IsNullOrEmpty(JSON))
            {
                Logger.Log($"[FilesManager] File {_Path} is empty");
                return null;
            }
            return JsonSerializer.Deserialize<List<string>>(JSON);
        }
    }
}
