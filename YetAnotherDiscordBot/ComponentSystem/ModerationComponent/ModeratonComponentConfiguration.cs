using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Configuration;

namespace YetAnotherDiscordBot.ComponentSystem.ModerationComponent
{
    public class ModeratonComponentConfiguration : ComponentConfiguration
    {
        public override string Filename => "ModerationConfig.json";

        public ulong ChannelID { get; set; } = 0;

        public ulong ServerID { get; set; } = 0;

        public string LockdownWarningVideoURL = "https://cdn.discordapp.com/attachments/887518399939350538/941157854734323712/SCP_SL_Light_Containment_Zone_Decontamination_30_Seconds.mp4";

        public uint LockdownDelay { get; set; } = 30;

        public Translations TranslationsData { get; set; } = new();

        public struct Translations
        {
            public string ReasonText = "Reason";

            public string AutherText = "Author";

            public string TargetText = "Target";

            public string ActionText = "Action";

            public string UntilText = "Until";

            public string ChannelText = "Channel";

            public string LockdownState = "Locked";

            public string DMText = "You were punished in {serverName}. \nAction: {action} \nReason: {reason} \nAuthor: {author}";

            public string LockdownWarning = "Danger, <#{channelId}> overall decontamination in T-minus {length} seconds. All checkpoint doors have been permanently opened, please evacuate immediately. \n{url}";

            public string LockdownStarted = "Channel is locked down and ready for decontamination, the removal of cringe has now begun.";

            public string LockdownEnded = "Channel lockdown lifted.";

            public ActionTranslations ActionTranslationsData = new();

            public struct ActionTranslations
            {
                public string BanText = "Ban";

                public string KickText = "Kick";

                public string TimeoutText = "Time Out";

                public string TempBan = "Temporary Ban";

                public string ChannelMute = "Channel Mute";

                public ActionTranslations() { }
            }

            public Translations() { }
        }
    }
}
