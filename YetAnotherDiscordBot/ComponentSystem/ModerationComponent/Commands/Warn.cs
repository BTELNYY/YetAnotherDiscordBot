using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.ComponentSystem.ModerationComponent.Commands
{
    public class Warn : Command
    {
        public override string CommandName => "warnuser";

        public override string Description => "Warns a user.";

        public override void BuildOptions()
        {
            base.BuildOptions();
            Options.Clear();
            CommandOption optionsBase = new CommandOption()
            {
                Name = "user",
                Description = "User which to warn",
                OptionType = Discord.ApplicationCommandOptionType.User,
                Required = true,
            };
            CommandOption optionsBase1 = new CommandOption()
            {
                Name = "reason",
                Description = "Reason for warning",
                OptionType = Discord.ApplicationCommandOptionType.String,
                Required = true,
            };
            Options.Add(optionsBase);
            Options.Add(optionsBase1);
        }

        public override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
        }
    }
}
