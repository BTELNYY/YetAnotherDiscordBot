using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot.Attributes
{

    [AttributeUsage(AttributeTargets.Parameter)]
    public class CommandParameter : Attribute
    {
        public CommandParameter(SlashCommandOptionBuilder builder)
        {
            Builder = builder;
        }

        public SlashCommandOptionBuilder Builder { get; set; } = new SlashCommandOptionBuilder();
    }
}
