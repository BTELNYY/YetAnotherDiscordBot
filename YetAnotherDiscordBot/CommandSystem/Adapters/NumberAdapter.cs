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
    [TypeAdapterAttribute(typeof(double))]
    public class NumberAdapter : TypeAdapter<double>
    {
        public override ApplicationCommandOptionType Type => ApplicationCommandOptionType.Number;

        public override double Adapt(SocketSlashCommandDataOption option, BotShard shard)
        {
            return (double)option.Value;
        }
    }
}
