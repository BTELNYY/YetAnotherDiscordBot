using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Configuration;

namespace YetAnotherDiscordBot.ComponentSystem.DeletedMessageLogger
{
    public class DeletedMessageConfiguration : ComponentConfiguration
    {
        public override string Filename => "DeletedMessageConfig.json";

        public ulong ChannelID = 0;

        public ulong GuildID = 0;
    }
}
