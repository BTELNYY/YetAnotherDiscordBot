using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.ComponentSystem.ModerationComponent;

namespace YetAnotherDiscordBot.ComponentSystem.ChannelUtilsComponent.Commands
{
    public class SetChannelSlowdown : Command
    {
        public override string CommandName => "setchannelslowdown";

        public override string Description => "Sets the channel slowdown to a specific value.";

        public override GuildPermission RequiredPermission => GuildPermission.ManageChannels;

        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(ModerationComponent.ModerationComponent),
            typeof(ChannelUtilsComponent),
        };

        public override void BuildOptions()
        {
            base.BuildOptions();
            CommandOptionsBase optionsBase = new CommandOptionsBase()
            {
                OptionType = Discord.ApplicationCommandOptionType.Integer,
                Name = "duration",
                Description = "The length of time for the slowmode to be, an integer between 0 and 21,600, represents the length in seconds.",
                Required = true
            };
            CommandOptionsBase optionsBase1 = new CommandOptionsBase()
            {
                OptionType = ApplicationCommandOptionType.String,
                Name = "reason",
                Description = "Audit logging reason",
                Required = true,
            };
            Options.Clear();
            Options.Add(optionsBase);
            Options.Add(optionsBase1);
        }

        public override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if(OwnerShard == null)
            {
                Log.Error("OwnerShard is null!");
                command.RespondAsync("An error occured: OwnerShard is null.", ephemeral: true);
                return;
            }
            ModerationComponent.ModerationComponent moderationComponent = OwnerShard.ComponentManager.GetComponent<ModerationComponent.ModerationComponent>(out bool success);
            if (!success)
            {
                Log.Error("Failed to get moderation component");
                command.RespondAsync("An error occured: Moderation Component can't be found.", ephemeral: true);
                return;
            }
            ChannelUtilsComponent channelUtilsComponent = OwnerShard.ComponentManager.GetComponent<ChannelUtilsComponent>(out bool channelUtilsComponentSuccess);
            if (!channelUtilsComponentSuccess)
            {
                Log.Error("Failed to get ChannelUtilsComponent");
                command.RespondAsync("An error occured: Channel Utils Component can't be found.", ephemeral: true);
                return;
            }
            if(command.Channel is not SocketTextChannel channel)
            {
                command.RespondAsync("This command can only be run in text channels.");
                return;
            }
            SocketSlashCommandDataOption[] options = GetOptionsOrdered(command.Data.Options.ToList());
            int length = (int)options[0].Value;
            string reason = (string)options[1].Value;
            if(length < 0 || length > 21600)
            {
                command.RespondAsync("Slowmode must be between 0 and 21600 seconds.");
                return;
            }
            RequestOptions discordOptions = new RequestOptions()
            {
                AuditLogReason = "Slowmode set by " + command.User.GlobalName,
                RetryMode = RetryMode.RetryRatelimit,
            };
            channel.ModifyAsync(props =>
            {
                props.SlowModeInterval = length;
            }, discordOptions);
            moderationComponent.SendMessageToAudit((SocketGuildUser)command.User, "Slowmode Changed: " + length.ToString(), reason);
            command.RespondAsync("Success.", ephemeral: true);
        }
    }
}
