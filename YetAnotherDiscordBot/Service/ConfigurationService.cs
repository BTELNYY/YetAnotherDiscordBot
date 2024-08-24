using Newtonsoft.Json;
using YetAnotherDiscordBot.ComponentSystem.WackerySLAPI;
using YetAnotherDiscordBot.Configuration;

namespace YetAnotherDiscordBot.Service
{
    public class ConfigurationService
    {
        public const string ConfigFolder = "./config/";

        //Global Config
        public const string GlobalConfigFolder = ConfigFolder + "global/";
        public const string GlobalAssemblyFolder = GlobalConfigFolder + "assemblies/";
        public const string GlobalConfigFile = ConfigFolder + "config.json";

        //Server Config
        public const string ServerConfigFolder = ConfigFolder + "servers/";
        public const string CacheFolder = ConfigFolder + "cache/";

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
            if (!Directory.Exists(GlobalAssemblyFolder))
            {
                Directory.CreateDirectory(GlobalAssemblyFolder);
            }
            if (!Directory.Exists(ServerConfigFolder))
            {
                Directory.CreateDirectory(ServerConfigFolder);
            }
            if (!Directory.Exists(CacheFolder))
            {
                Directory.CreateDirectory(CacheFolder);
            }
        }

        public static GlobalConfiguration GlobalConfiguration
        {
            get
            {
                if(_cachedGlobalConfiguration == null)
                {
                    _cachedGlobalConfiguration = GetFileGlobalConfiguration();
                }
                return _cachedGlobalConfiguration;
            }
        }

        private static GlobalConfiguration? _cachedGlobalConfiguration;

        private static GlobalConfiguration GetFileGlobalConfiguration()
        {
            if (!File.Exists(GlobalConfigFile))
            {
                _cachedGlobalConfiguration = CreateGlobalConfiguration();
                return _cachedGlobalConfiguration;
            }
            else
            {
                string data = File.ReadAllText(GlobalConfigFile);
                GlobalConfiguration? config = JsonConvert.DeserializeObject<GlobalConfiguration>(data);
                if(config == null)
                {
                    return CreateGlobalConfiguration(true);
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

        private static GlobalConfiguration CreateGlobalConfiguration(bool recreate = false)
        {
            if(File.Exists(GlobalConfigFile) && !recreate)
            {
                return GetFileGlobalConfiguration();
            }
            string json = JsonConvert.SerializeObject(new GlobalConfiguration(), Formatting.Indented);
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

        public static T GetComponentConfiguration<T>(ulong serverId, out bool success, bool writeFile = false, bool doRewrite = true) where T : ComponentConfiguration
        {
            T? model = (T?)Activator.CreateInstance(typeof(T));
            if(model == null)
            {
                throw new InvalidOperationException("Failed to generate new instance of type " + typeof(T).FullName);
            }
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
                    result ??= (T)new ComponentConfiguration();
                    result.OwnerID = serverId;
                    return result;
                }
            }
            else
            {
                string data = File.ReadAllText(filePath);
                T? json = JsonConvert.DeserializeObject<T>(data);
                if (json == null)
                {
                    success = false;
                    T? result = (T?)Activator.CreateInstance(typeof(T));
                    if (result == null)
                    {
                        if (doRewrite)
                        {
                            WriteComponentConfiguration((T)new ComponentConfiguration(), serverId, overwrite: true);
                        }
                        result = (T)new ComponentConfiguration();
                    }
                    result.OwnerID = serverId;
                    return result;
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

        public static T GetComponentConfiguration<T>(T model, ulong serverId, out bool success, bool writeFile = false, bool doRewrite = true) where T : ComponentConfiguration
        {
            string filePath = ServerConfigFolder + serverId.ToString() + "/" + model.Filename;
            Type t = model.GetType();
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
                    T? result = (T?)Activator.CreateInstance(t);
                    result ??= (T)new ComponentConfiguration();
                    result.OwnerID = serverId;
                    return result;
                }
            }
            else
            {
                string data = File.ReadAllText(filePath);
                T? json = JsonConvert.DeserializeObject(data, t) as T;
                if (json == null)
                {
                    success = false;
                    T? result = (T?)Activator.CreateInstance(t);
                    if (result == null)
                    {
                        if (doRewrite)
                        {
                            WriteComponentConfiguration((T)new ComponentConfiguration(), serverId, overwrite: true);
                        }
                        result = (T)new ComponentConfiguration();
                    }
                    result.OwnerID = serverId;
                    return result;
                }
                else
                {
                    success = true;
                    if (doRewrite || json.OwnerID == 0)
                    {
                        json.OwnerID = serverId;
                        WriteComponentConfiguration<T>(json, serverId, overwrite: true);
                    }
                    json.OwnerID = serverId;
                    return json;
                }
            }
        }

        public static T WriteComponentConfiguration<T>(T model, ulong serverId, bool overwrite = false) where T : ComponentConfiguration
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
                model.OwnerID = serverId;
                string json = JsonConvert.SerializeObject(model, Formatting.Indented);
                File.WriteAllText(filePath, json);
                return GetComponentConfiguration(model, serverId, out bool result);
            }
        }

        /// <summary>
        /// Saves configuration according to the internal <see cref="ComponentConfiguration.OwnerID"/> property, note that the bool
        /// does not signify that the file has been written or not, just that validation has suceeded or not.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public static bool SaveComponentConfiguration<T>(T? model) where T : ComponentConfiguration
        {
            if(model == null)
            {
                Log.GlobalError("Passed null model into the save config function.");
                return false;
            }
            if(model.OwnerID == 0)
            {
                Log.GlobalError("Given an object with a invalid OwnerID! Object Type: " + model.GetType().Name);
                return false;
            }
            WriteComponentConfiguration(model, model.OwnerID, true);
            return true;
        }

        public static bool ResetComponentConfiguration<T>(T model) where T : ComponentConfiguration
        {
            if (model == null)
            {
                Log.GlobalError("Passed null model into the save config function.");
                return false;
            }
            if (model.OwnerID == 0)
            {
                Log.GlobalError("Given an object with a invalid OwnerID.");
                return false;
            }
            T? newInstance = (T?)Activator.CreateInstance(model.GetType());
            if (newInstance == null)
            {
                Log.GlobalError("Failed to create new instance of given type. Type: " + model.GetType().FullName);
                return false;
            }
            WriteComponentConfiguration(newInstance, model.OwnerID, true);
            return true;
        }

        public static bool DeleteComponentConfiguration<T>(T model) where T : ComponentConfiguration
        {
            if(model == null)
            {
                Log.GlobalError("Model is null in delete configuration method.");
                return false;
            }
            if(model.OwnerID == 0)
            {
                Log.GlobalError("OwnerID given in Deleted component configuration is invalid.");
                return false;
            }
            string filePath = ServerConfigFolder + model.OwnerID.ToString() + "/" + model.Filename;
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
    }
}