using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
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

        public bool DeleteData<T>(ulong id) where T : DiscordUserData
        {
            object[] args = { id, this };
            T? clazz = (T?)Activator.CreateInstance(typeof(T), args);
            if (clazz == null)
            {
                throw new NullReferenceException("Activator Created null object.");
            }
            string fileName = GetUserFolder(id) + clazz.Filename;
            if (!File.Exists(fileName))
            {
                return false;
            }
            File.Delete(fileName);
            return true;
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

        public T GetData<T>(ulong id) where T : DiscordUserData
        {
            object[] args = { id, this };
            T? value = (T?)Activator.CreateInstance(typeof(T), args);
            if (value == null)
            {
                throw new NullReferenceException("Activator Created null object.");
            }
            string fileName = GetUserFolder(id) + value.Filename;
            if (!File.Exists(fileName))
            {
                return WriteData(value, id, true);
            }
            string data = File.ReadAllText(fileName);
            T? desil = JsonConvert.DeserializeObject<T>(data);
            if(desil == null)
            {
                return value;
            }
            else
            {
                desil.DiscordUserDataService = this;
                desil.OwnerID = id;
                return desil;
            }
        }

        public T WriteData<T>(T model, ulong id, bool overwrite = true) where T : DiscordUserData
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
                    return GetData<T>(id);
                }
            }
            File.WriteAllText(fileName, json);
            return model;
        }
    }
}
