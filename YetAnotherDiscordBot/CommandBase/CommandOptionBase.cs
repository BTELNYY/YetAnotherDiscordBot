using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot.CommandBase
{
    public class CommandOption
    {
        public string Name { get; set; } = "option";
        public ApplicationCommandOptionType OptionType { get; set; } = ApplicationCommandOptionType.String;
        public bool Required { get; set; } = false;
        public string Description { get; set; } = "Desc";

        public CommandOption(string name, ApplicationCommandOptionType optionType, bool required, string description)
        {
            Name = name;
            OptionType = optionType;
            Required = required;
            Description = description;
        }

        public CommandOption()
        {

        }
    }
}
