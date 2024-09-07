using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Attributes;

namespace YetAnotherDiscordBot.CommandSystem.Adapters
{
    [TypeAdapterAttribute(typeof(SocketUser))]
    public class UserAdapter : TypeAdapter<SocketUser>
    {
        public override ApplicationCommandOptionType Type => ApplicationCommandOptionType.User;

        public override SocketUser Adapt(SocketSlashCommandDataOption option, BotShard? shard)
        {
            IUser user = (IUser)option.Value;
            return Program.Client.GetUser(user.Id);
        }
    }
}
