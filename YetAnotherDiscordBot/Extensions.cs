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
    }
}
