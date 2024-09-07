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
    [TypeAdapterAttribute(typeof(long))]
    public class IntegerAdapter : TypeAdapter<long>
    {
        public override ApplicationCommandOptionType Type => ApplicationCommandOptionType.Integer;

        public override long Adapt(SocketSlashCommandDataOption option, BotShard? shard)
        {
            return (long)option.Value;
        }
    }
}
