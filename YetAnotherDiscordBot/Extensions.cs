using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot
{
    public static class Extensions
    {
        public static Embed GetErrorEmbed(this EmbedBuilder embed, string title, string details)
        {
            embed.Title = title;
            embed.Description = details;
            embed.Color = Color.Red;
            embed.WithCurrentTimestamp();
            return embed.Build();
        }

        public static SocketMessage? GetServerMessage(this MessageReference messageReference)
        {
            BotShard shard = Program.GetShard(messageReference.GuildId.Value);
            if(shard == null)
            {
                return null;
            }
            ITextChannel? channel = shard.TargetGuild.GetChannel(messageReference.ChannelId) as ITextChannel;
            if(channel == null)
            {
                return null;
            }
            SocketMessage? message = channel.GetMessageAsync(messageReference.MessageId.Value).Result as SocketMessage;
            if(message == null)
            {
                return null;
            }
            return message;
        }

        /// <summary>
        /// Checks if the type is a sublcass of the generic type <paramref name="generic"/>.
        /// </summary>
        /// <param name="toCheck"></param>
        /// <param name="generic"></param>
        /// <returns></returns>
        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                if(toCheck.BaseType == null)
                {
                    return false;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        public static Type? GetSuperClass(this Type type, Type super)
        {
            while (type != null && type != typeof(object))
            {
                var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                if (super == cur)
                {
                    return cur;
                }
                if (type.BaseType == null)
                {
                    return null;
                }
                type = type.BaseType;
            }
            return null;
        }
    }
}
