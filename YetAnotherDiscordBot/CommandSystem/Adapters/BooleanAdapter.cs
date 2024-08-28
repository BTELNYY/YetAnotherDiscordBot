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
    [TypeAdapterAttribute(typeof(bool))]
    public class BooleanAdapter : TypeAdapter<bool>
    {
        public override ApplicationCommandOptionType Type => ApplicationCommandOptionType.Boolean;

        public override bool Adapt(SocketSlashCommandDataOption option, BotShard shard)
        {
            return (bool)option.Value;
        }
    }
}
