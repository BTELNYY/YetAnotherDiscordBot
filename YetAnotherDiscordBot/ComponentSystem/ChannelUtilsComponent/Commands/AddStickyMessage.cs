using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.ComponentSystem.ChannelUtilsComponent;

namespace YetAnotherDiscordBot.ComponentSystem.ChannelUtilsComponent.Commands
{
    public class AddStickyMessage : Command
    {
        public override string CommandName => "addstickymessage";

        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(ChannelUtilsComponent),
        };

        public override string Description => "Adds a sticky message to a channel.";

        public override GuildPermission RequiredPermission => GuildPermission.ManageChannels;

        public override bool UseLegacyExecute => true;

        public override void LegacyExecute(SocketSlashCommand command)
        {
            base.LegacyExecute(command);
            if (OwnerShard == null)
            {
                throw new InvalidOperationException("OwnerShard is null!");
            }
            SocketSlashCommandDataOption[] options = GetOptionsOrdered(command.Data.Options).ToArray();
            string text = (string)options[0].Value;
            SocketChannel currentChannel = (SocketChannel)command.Channel;
            if (command.Data.Options.Count > 1)
            {
                currentChannel = OwnerShard.TargetGuild.GetChannel(((IChannel)options[1].Value).Id);
            }
            EmbedBuilder eb = new EmbedBuilder();
            if (currentChannel is not SocketTextChannel socketTextChannel || socketTextChannel.GetChannelType() != ChannelType.Text)
            {
                eb.GetErrorEmbed("Error", "Invalid channel, only text channels can have sticky messages.");
                command.RespondAsync(embed: eb.Build(), ephemeral: true);
                return;
            }
            ChannelUtilsComponent component = OwnerShard.ComponentManager.GetComponent <ChannelUtilsComponent>(out bool success);
            if (!success)
            {
                throw new InvalidOperationException("Cannot find the ModerationComponent");
            }
            bool addedStickyChannel = component.AddStickyChannel(socketTextChannel.Id, text, out string errorMessage);
            if (!addedStickyChannel)
            {
                command.RespondAsync(embed: eb.GetErrorEmbed("Error", errorMessage), ephemeral: true);
                return;
            }
            command.RespondAsync("Success", ephemeral: true);
        }

        public override void BuildOptions()
        {
            base.BuildOptions();
            Options.Clear();
            CommandOption cob = new CommandOption()
            {
                Name = "message",
                Description = "Message to pin",
                OptionType = ApplicationCommandOptionType.String,
                Required = true
            };
            Options.Add(cob);
            CommandOption cob1 = new CommandOption()
            {
                Name = "channel",
                Description = "target channel",
                OptionType = ApplicationCommandOptionType.Channel,
                Required = false
            };
            Options.Add(cob1);
        }
    }
}
