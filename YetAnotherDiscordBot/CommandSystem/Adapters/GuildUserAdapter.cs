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
    [TypeAdapterAttribute(typeof(SocketGuildUser))]
    public class GuildUserAdapter : TypeAdapter<SocketGuildUser>
    {
        public override ApplicationCommandOptionType Type => ApplicationCommandOptionType.User;

        public override SocketGuildUser Adapt(SocketSlashCommandDataOption option, BotShard? shard)
        {
            if (shard == null)
            {
                throw new ArgumentNullException(nameof(shard));
            }
            IUser user = (IUser)option.Value;
            SocketGuildUser guild = shard.TargetGuild.GetUser(user.Id);
            return guild;
        }
    }
}
