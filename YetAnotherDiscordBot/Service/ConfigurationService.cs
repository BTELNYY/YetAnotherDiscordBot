using Newtonsoft.Json;
using YetAnotherDiscordBot.Configuration;

namespace YetAnotherDiscordBot.Service
{
    public class ConfigurationService
    {
        public const string ConfigFolder = "./config/";

        //Global Config
        public const string GlobalConfigFolder = ConfigFolder + "global/";
        public const string GlobalConfigFile = ConfigFolder + "config.json";

        //Server Config
        public const string ServerConfigFolder = ConfigFolder + "servers/";

        public static void Start()
        {
            if (!Directory.Exists(ConfigFolder))
            {
                Directory.CreateDirectory(ConfigFolder);
            }
            if (!Directory.Exists(GlobalConfigFolder))
            {
                Directory.CreateDirectory(GlobalConfigFolder);
            }
            if (!Directory.Exists(ServerConfigFolder))
            {
                Directory.CreateDirectory(ServerConfigFolder);
            }
        }

        public static GlobalConfiguration GlobalConfiguration
        {
            get
            {
                if(_cachedGlobalConfiguration == null)
                {
                    return GetFileGlobalConfiguration();
                }
                else
                {
                    return _cachedGlobalConfiguration;
                }
            }
        }

        private static GlobalConfiguration? _cachedGlobalConfiguration;

        private static GlobalConfiguration GetFileGlobalConfiguration()
        {
            if (!File.Exists(GlobalConfigFile))
            {
                Log.GlobalError("Global configuration file does not exist!");
                _cachedGlobalConfiguration = CreateGlobalConfiguration();
                return _cachedGlobalConfiguration;
            }
            else
            {
                string data = File.ReadAllText(GlobalConfigFile);
                GlobalConfiguration? config = JsonConvert.DeserializeObject<GlobalConfiguration>(data);
                if(config == null)
                {
                    Log.GlobalError("Failed to get GlobalConfig from string!");
                    return CreateGlobalConfiguration();
                }
                else
                {
                    WriteGlobalConfiguration(config, true);
                    return config;
                }
            }
        }

        private static void WriteGlobalConfiguration(GlobalConfiguration config, bool overwrite = true)
        {
            if (File.Exists(GlobalConfigFile))
            {
                if (overwrite)
                {
                    File.Delete(GlobalConfigFile);
                }
                else
                {
                    return;
                }
            }
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(GlobalConfigFile, json);
        }

        private static GlobalConfiguration CreateGlobalConfiguration()
        {
            if(File.Exists(GlobalConfigFile))
            {
                return GetFileGlobalConfiguration();
            }
            string json = JsonConvert.SerializeObject(new GlobalConfiguration(), Formatting.Indented);
            Log.GlobalDebug(json);
            File.WriteAllText(GlobalConfigFile, json);
            return new GlobalConfiguration();
        }

        public static ServerConfiguration GetServerConfiguration(ulong serverId)
        {
            string folderPath = ServerConfigFolder + serverId.ToString();
            if (!Directory.Exists(folderPath)) 
            {
                Directory.CreateDirectory(folderPath);
                return WriteServerConfiguration(serverId);
            }
            string filePath = folderPath + "/config.json";
            if(!File.Exists(filePath))
            {
                return WriteServerConfiguration(serverId);
            }
            string data = File.ReadAllText(filePath);
            ServerConfiguration? config = JsonConvert.DeserializeObject<ServerConfiguration>(data);
            if(config == null)
            {
                Log.GlobalError("Failure to get server data! ServerID: " + serverId.ToString());
                return new ServerConfiguration();
            }
            else
            {
                WriteServerConfiguration(serverId, config, overwrite: true);
                return config;
            }
        }
        
        public static ServerConfiguration WriteServerConfiguration(ulong serverId)
        {
            string folderPath = ServerConfigFolder + serverId.ToString();
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string json = JsonConvert.SerializeObject(new ServerConfiguration(), Formatting.Indented);
            File.WriteAllText(folderPath + "/config.json", json);
            return new ServerConfiguration();
        }


        public static ServerConfiguration WriteServerConfiguration(ulong serverId, ServerConfiguration configuration, bool overwrite = false)
        {
            string folderPath = ServerConfigFolder + serverId.ToString();
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
            if(File.Exists(folderPath + "/config.json"))
            {
                if (overwrite)
                {
                    File.Delete(folderPath + "/config.json");
                    File.WriteAllText(folderPath + "/config.json", json);
                }
            }
            else
            {
                File.WriteAllText(folderPath + "/config.json", json);
            }
            return configuration;
        }

        public static T GetComponentConfiguration<T>(T model, ulong serverId, out bool success, bool writeFile = false, bool doRewrite = true) where T : ComponentServerConfiguration
        {
            string filePath = ServerConfigFolder + serverId.ToString() + "/" + model.Filename;
            if (!File.Exists(filePath))
            {
                if (writeFile)
                {
                    success = true;
                    return WriteComponentConfiguration(model, serverId);
                }
                else
                {
                    success = false;
                    T? result = (T?)Activator.CreateInstance(typeof(T));
                    if(result == null)
                    {
                        return (T)new ComponentServerConfiguration();
                    }
                    else
                    {
                        return result;
                    }
                }
            }
            else
            {
                string data = File.ReadAllText(filePath);
                T? json = JsonConvert.DeserializeObject<T>(data);
                if(json == null)
                {
                    success = false;
                    T? result = (T?)Activator.CreateInstance(typeof(T));
                    if (result == null)
                    {
                        if (doRewrite)
                        {
                            WriteComponentConfiguration((T)new ComponentServerConfiguration(), serverId, overwrite: true);
                        }
                        return (T)new ComponentServerConfiguration();
                    }
                    else
                    {
                        return result;
                    }
                }
                else
                {
                    success = true;
                    if (doRewrite)
                    {
                        WriteComponentConfiguration<T>(json, serverId, overwrite: true);
                    }
                    return json;
                }
            }
        }

        public static T WriteComponentConfiguration<T>(T model, ulong serverId, bool overwrite = false) where T : ComponentServerConfiguration
        {
            string filePath = ServerConfigFolder + serverId.ToString() + "/" + model.Filename;
            if (File.Exists(filePath))
            {
                if (overwrite)
                {
                    File.Delete(filePath);
                    string json = JsonConvert.SerializeObject(model, Formatting.Indented);
                    File.WriteAllText(filePath, json);
                    return GetComponentConfiguration(model, serverId, out bool result, doRewrite: false);
                }
                else
                {
                    return GetComponentConfiguration(model, serverId, out bool result);
                }
            }
            else
            {
                string json = JsonConvert.SerializeObject(model, Formatting.Indented);
                File.WriteAllText(filePath, json);
                return GetComponentConfiguration(model, serverId, out bool result);
            }
        }
    }
}