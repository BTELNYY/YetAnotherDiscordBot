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
    [TypeAdapterAttribute(typeof(SocketRole))]
    public class RoleAdapter : TypeAdapter<SocketRole>
    {
        public override ApplicationCommandOptionType Type => ApplicationCommandOptionType.Role;

        public override SocketRole Adapt(SocketSlashCommandDataOption option, BotShard shard)
        {
            return shard.TargetGuild.GetRole(((IRole)option.Value).Id);
        }
    }
}
