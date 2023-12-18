using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot.Configuration
{
    public class GlobalConfiguration : Configuration
    {
        public string Token = "";

        public List<ulong> DeveloperIDs = new List<ulong>()
        {

        };

        public UserStatus BotStatus = UserStatus.Online;

        public string BotStatusMessage = "";
    }
}