using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.ComponentSystem.ChannelUtilsComponent.Commands
{
    public class DeleteMessagesContent : Command
    {
        public override string CommandName => "deletemessagecontent";

        public override string Description => "Deletes all messages containing a string";

        public override GuildPermission RequiredPermission => GuildPermission.ManageMessages;

        public override bool UseLegacyExecute => true;

        public override void LegacyExecute(SocketSlashCommand command)
        {
            base.LegacyExecute(command);
            if(OwnerShard == null)
            {
                throw new InvalidOperationException("OwnerShard is null!");
            }
        }

        public override void BuildOptions()
        {
            base.BuildOptions();
            Options.Clear();
            CommandOption option = new CommandOption
            {
                Required = true,
                Name = "stringcontent",
                OptionType = ApplicationCommandOptionType.String,
                Description = "Comma seperated list of substrings to remove."
            };
            Options.Add(option);
            CommandOption option1 = new CommandOption
            {
                Required = false,
                Name = "targetuser",
                OptionType = ApplicationCommandOptionType.User,
                Description = "The user whos messages to remove. (All if null)"
            };
            Options.Add(option1);
            CommandOption option2 = new CommandOption
            {
                Required = false,
                Name = "channel",
                OptionType = ApplicationCommandOptionType.Channel,
                Description = "Target channel",
            };
            Options.Add(option2);
        }
    }
}
