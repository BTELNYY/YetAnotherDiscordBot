using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.ComponentSystem.ChannelUtilsComponent.Commands
{
    public class RemoveStickyMessage : Command
    {
        public override string CommandName => "removestickymessage";

        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(ChannelUtilsComponent),
        };

        public override string Description => "Adds a sticky message to a channel.";

        public override GuildPermission RequiredPermission => GuildPermission.ManageChannels;

        public override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if (OwnerShard == null)
            {
                throw new InvalidOperationException("OwnerShard is null!");
            }
            SocketSlashCommandDataOption[] options = GetOptionsOrdered(command.Data.Options).ToArray();
            SocketChannel currentChannel = (SocketChannel)command.Channel;
            if (command.Data.Options.Count > 0)
            {
                currentChannel = OwnerShard.TargetGuild.GetChannel(((IChannel)options[0].Value).Id);
            }
            EmbedBuilder eb = new EmbedBuilder();
            if (currentChannel is not SocketTextChannel socketTextChannel || socketTextChannel.GetChannelType() != ChannelType.Text)
            {
                eb.GetErrorEmbed("Error", "Invalid channel, only text channels can have sticky messages.");
                command.RespondAsync(embed: eb.Build(), ephemeral: true);
                return;
            }
            ChannelUtilsComponent component = OwnerShard.ComponentManager.GetComponent<ChannelUtilsComponent>(out bool success);
            if (!success)
            {
                throw new InvalidOperationException("Cannot find the ChannelUtilsComponent");
            }
            bool addedStickyChannel = component.RemoveStickyChannel(socketTextChannel.Id, out string errorMessage);
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
                Name = "channel",
                Description = "target channel",
                OptionType = ApplicationCommandOptionType.Channel,
                Required = false
            };
            Options.Add(cob);
        }
    }
}
