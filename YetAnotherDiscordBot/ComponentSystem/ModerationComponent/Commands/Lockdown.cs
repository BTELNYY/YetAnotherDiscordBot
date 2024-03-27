using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.ComponentSystem.ModerationComponent;

namespace YetAnotherDiscordBot.ComponentSystem.ModerationComponent.Commands
{
    public class LockdownChannel : Command
    {
        public override string CommandName => "lockdown";
        public override string Description => "Locks a channel down with an optional 30 second warning.";
        public override GuildPermission RequiredPermission => GuildPermission.Administrator;
        public override bool IsDefaultEnabled => true;
        public override List<Type> RequiredComponents => new List<Type>
        {
            typeof(ModerationComponent),
        };
        public async override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if (OwnerShard is null)
            {
                await command.RespondAsync("An error occured, contact btelnyy for details.", ephemeral: true);
                Log.Error("Shard who ran me is null");
                return;
            }
            ModerationComponent moderationComponent = OwnerShard.ComponentManager.GetComponent<ModerationComponent>(out bool success);
            if (!success)
            {
                await command.RespondAsync("An error occured, contact btelnyy for details.", ephemeral: true);
                Log.Error("Failed to get moderation component.");
                return;
            }
            SocketSlashCommandDataOption[] options = (SocketSlashCommandDataOption[])GetOptionsOrdered(command.Data.Options.ToList());
            bool showCountdown = false;
            if (command.Data.Options.Count > 0 && options[0].Value is not null)
            {
                if ((bool)options[0].Value == true)
                {
                    showCountdown = true;
                }
            }
            else
            {
                showCountdown = false;
            }
            SocketGuildUser author = OwnerShard.TargetGuild.GetUser(command.User.Id);
            var channel = command.Channel;
            if (channel is null)
            {
                Log.Error("Failed to get channel in lockdown command!");
                await command.RespondAsync("There was an error, see log.", ephemeral: true);
                return;
            }
            if (channel is not SocketTextChannel textChannel)
            {
                await command.RespondAsync("This command can only be used in text channels.", ephemeral: true);
                return;
            }
            await command.RespondAsync("Success", ephemeral: true);
            moderationComponent.LockdownChannel(textChannel, author, showCountdown);
        }

        public override void BuildOptions()
        {
            CommandOption cob = new()
            {
                Name = "showcountdown",
                Description = "sends the SCP:SL decontamination 30 second warning.",
                OptionType = ApplicationCommandOptionType.Boolean,
                Required = false
            };
            Options.Clear();
            Options.Add(cob);
        }
    }
}
