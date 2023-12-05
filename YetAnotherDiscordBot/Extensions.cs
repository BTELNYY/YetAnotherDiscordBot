using Discord;
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
    }
}
