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
    [TypeAdapterAttribute(typeof(SocketGuildChannel))]
    public class GuildChannelAdapter : TypeAdapter<SocketGuildChannel>
    {
        public override ApplicationCommandOptionType Type => ApplicationCommandOptionType.Channel;

        public override SocketGuildChannel Adapt(SocketSlashCommandDataOption option, BotShard? shard)
        {
            if (shard == null)
            {
                throw new ArgumentNullException(nameof(shard));
            }
            return shard.TargetGuild.GetChannel(((IChannel)option.Value).Id);
        }
    }
}
