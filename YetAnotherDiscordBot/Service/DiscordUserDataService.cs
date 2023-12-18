using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Configuration;

namespace YetAnotherDiscordBot.Service
{
    public class DiscordUserDataService
    {
        private ulong _guildId;

        public ulong GuildID
        {
            get
            {
                return _guildId;
            }
        }

        private BotShard _botShard;

        public BotShard BotShard
        {
            get
            {
                return _botShard;
            }
        }

        public string ConfigFolder
        {
            get
            {
                return ConfigurationService.ServerConfigFolder + _guildId.ToString() + "/userdata/";
            }
        }

        public string GetUserFolder(ulong id)
        {
            return ConfigFolder + id.ToString() + "/";
        }

        public DiscordUserDataService(BotShard botShard)
        {
            _botShard = botShard;
            _guildId = botShard.GuildID;
            Directory.CreateDirectory(ConfigFolder);
        }

        public bool DeleteAllData(ulong id)
        {
            if(Directory.Exists(GetUserFolder(id)))
            {
                Directory.Delete(GetUserFolder(id));
                return true;
            }
            return false;
        }

        public bool DeleteData<T>(T model, ulong id) where T : DiscordUserData
        {
            string fileName = GetUserFolder(id) + model.Filename;
            if (!File.Exists(fileName))
            {
                return false;
            }
            File.Delete(fileName);
            return true;
        }

        public T GetData<T>(T model, ulong id) where T : DiscordUserData
        {
            string fileName = GetUserFolder(id) + model.Filename;
            if (!File.Exists(fileName))
            {
                return CreateData(model, id);
            }
            string data = File.ReadAllText(fileName);
            DiscordUserData? desil = JsonConvert.DeserializeObject<DiscordUserData>(data);
            if(desil == null)
            {
                T? newData = (T?)Activator.CreateInstance(typeof(T));
                if(newData == null)
                {
                    return (T)new DiscordUserData();
                }
                else
                {
                    return newData;
                }
            }
            else
            {
                return (T)desil;
            }
        }

        public T WriteData<T>(T model, ulong id, bool overwrite = false) where T : DiscordUserData
        {
            string fileName = GetUserFolder(id) + model.Filename;
            Directory.CreateDirectory(GetUserFolder(id));
            string json = JsonConvert.SerializeObject(model, Formatting.Indented);
            if(File.Exists(fileName))
            {
                if (overwrite)
                {
                    File.Delete(fileName);
                }
                else
                {
                    return GetData(model, id);
                }
            }
            File.WriteAllText(fileName, json);
            return model;
        }

        private T CreateData<T>(T model, ulong id, bool overwrite = false) where T : DiscordUserData
        {
            string fileName = GetUserFolder(id) + model.Filename;
            Directory.CreateDirectory(GetUserFolder(id));
            if (File.Exists(fileName))
            {
                if (overwrite)
                {
                    File.Delete(fileName);
                    string json = JsonConvert.SerializeObject(model, Formatting.Indented);
                    File.WriteAllText(fileName, json);
                    return (T)new DiscordUserData();
                }
                else
                {
                    return GetData(model, id);
                }
            }
            else
            {
                string json = JsonConvert.SerializeObject(model, Formatting.Indented);
                File.WriteAllText(fileName, json);
                return (T)new DiscordUserData();
            }
        }
    }
}
