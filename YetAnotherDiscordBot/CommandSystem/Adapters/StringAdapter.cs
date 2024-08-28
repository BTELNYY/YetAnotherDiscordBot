using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Attributes;

namespace YetAnotherDiscordBot.CommandSystem.Adapters
{
    [TypeAdapterAttribute(typeof(string))]
    public class StringAdapter : TypeAdapter<string>
    {
        public override ApplicationCommandOptionType Type => ApplicationCommandOptionType.String;

        public override string Adapt(SocketSlashCommandDataOption option, BotShard shard)
        {
            return (string)option.Value;
        }
    }
}
