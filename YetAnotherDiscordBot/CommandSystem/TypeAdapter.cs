using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot.CommandSystem
{
    public class TypeAdapter<T> : IGenericTypeAdapter
    {
        public virtual ApplicationCommandOptionType Type
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual T Adapt(SocketSlashCommandDataOption option, BotShard? shard)
        {
            throw new NotImplementedException();
        }

        public object? AdaptGeneric(SocketSlashCommandDataOption option, BotShard? shard)
        {
            object? result = Adapt(option, shard);
            return result;
        }
    }
}
