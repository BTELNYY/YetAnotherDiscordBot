using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot.Configuration
{
    public class DiscordUserData : Configuration
    {
        public virtual string Filename { get; } = "config.json";

        [JsonIgnore]
        public Log Log
        {
            get
            {
                return DiscordUserDataService.BotShard.Log;
            }
        }

        private ulong _ownerId = 0;

        public ulong OwnerID
        {
            get
            {
                return _ownerId;
            }
            internal set
            {
                _ownerId = value;
            }
        }

        [JsonIgnore]
        private DiscordUserDataService? _discordUserDataService;

        [JsonIgnore]
        public DiscordUserDataService DiscordUserDataService
        {
            get
            {
                if(_discordUserDataService == null)
                {
                    throw new InvalidOperationException("The DiscordUserDataService is null.");
                }
                return _discordUserDataService;
            }
            internal set
            {
                _discordUserDataService = value;
            }
        }
        
        public DiscordUserData(ulong id, DiscordUserDataService service)
        {
            _ownerId = id;
            _discordUserDataService = service;
        }
    }
}
