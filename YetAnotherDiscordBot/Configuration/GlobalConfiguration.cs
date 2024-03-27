using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Service;

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

        public bool ShowLogsInConsole = true;

        public bool EnableLogging = true;

        public string LogPath  = $"./{ConfigurationService.ConfigFolder}/logs/";

        public List<LogLevel> StackframePrintLevels = new List<LogLevel>();

        public bool PrintStackFrames = true;
    }
}