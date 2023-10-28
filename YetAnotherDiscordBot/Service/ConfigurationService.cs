﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Configuration;

namespace YetAnotherDiscordBot.Service
{
    public class ConfigurationService
    {
        public const string ConfigFolder = "./config/";

        //Global Config
        public const string GlobalConfigFolder = ConfigFolder + "global/";
        public const string GlobalConfigFile = GlobalConfigFolder + "config.json";

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
                Log.Error("Global configuration file does not exist!");
                _cachedGlobalConfiguration = CreateGlobalConfiguration();
                return _cachedGlobalConfiguration;
            }
            else
            {
                string data = File.ReadAllText(GlobalConfigFile);
                GlobalConfiguration? config = JsonConvert.DeserializeObject<GlobalConfiguration>(data);
                if(config == null)
                {
                    Log.Error("Failed to get GlobalConfig from string!");
                    return CreateGlobalConfiguration();
                }
                else
                {
                    return config;
                }
            }
        }

        private static GlobalConfiguration CreateGlobalConfiguration()
        {
            if(File.Exists(GlobalConfigFile))
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
                Log.Error("Failure to get server data! ServerID: " + serverId.ToString());
                return new ServerConfiguration();
            }
            else
            {
                return config;
            }
        }
        
        private static ServerConfiguration WriteServerConfiguration(ulong serverId)
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

        public static T GetComponentConfiguration<T>(T model, ulong serverId, out bool success, bool writeFile = false) where T : ComponentServerConfiguration
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
                    return GetComponentConfiguration(model, serverId, out bool result);
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