using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Configuration;

namespace YetAnotherDiscordBot.ComponentSystem.ChannelUtilsComponent
{
    public class ChannelUtilsComponentConfiguration : ComponentConfiguration
    {
        public override string Filename => "ChannelUtilsConfig.json";

        public List<StickyMessageData> StickiedMessages { get; set; } = new List<StickyMessageData>();

        public uint RefreshStickyMessagesEveryXSeconds { get; set; } = 30;

        public struct StickyMessageData
        {
            public string Message = string.Empty;

            public ulong ChannelID = 0;

            public ulong LastMessageID = 0;

            public StickyMessageData() { }
        }
    }
}
