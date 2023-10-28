using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Configuration;

namespace YetAnotherDiscordBot.ComponentSystem.ModerationComponent
{
    public class ModeratonComponentConfiguration : ComponentServerConfiguration
    {
        public override string Filename => "ModeratonConfig.json";

        public ulong ChannelID = 0;

        public ulong ServerID = 0;

        public Translations TranslationsData = new();

        public struct Translations
        {
            public string ReasonText = "Reason";

            public string AutherText = "Author";

            public string ActionText = "Action";

            public string UntilText = "Until";

            public string ChannelText = "Channel";

            public string DMText = "You were punished in {serverName}. \nAction: {action} \nReason: {reason} \nAuthor: {author}";

            public ActionTranslations ActionTranslationsData = new();

            public struct ActionTranslations
            {
                public string BanText = "Ban";

                public string KickText = "Kick";

                public string TimeoutText = "Time Out";

                public string TempBan = "Temporary Ban";

                public string ChannelMute = "Channel Mute";

                public string Muzzle = "Muzzled";

                public ActionTranslations() { }
            }

            public Translations() { }
        }
    }
}
