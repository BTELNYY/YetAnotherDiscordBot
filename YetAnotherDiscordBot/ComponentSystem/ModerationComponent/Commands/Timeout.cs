using Discord;
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
    public class TimeoutUser : Command
    {
        public override string CommandName => "timeoutuser";
        public override string Description => "time a user out for a set amount of seconds.";
        public override GuildPermission RequiredPermission => GuildPermission.MuteMembers;
        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(ModerationComponent),
        };
        public async override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            SocketSlashCommandDataOption[] options = (SocketSlashCommandDataOption[])GetOptionsOrdered(command.Data.Options.ToList());
            SocketGuildUser target = (SocketGuildUser)options[0].Value;
            if (OwnerShard is null)
            {
                return;
            }
            ModerationComponent moderationComponent = OwnerShard.ComponentManager.GetComponent<ModerationComponent>(out bool success);
            if (!success)
            {
                Log.Warning("Failed to fetch Moderation Component!");
                await command.RespondAsync("An error occured. Contact btelnyy for details.", ephemeral: true);
                return;
            }
            long length = (long)options[1].Value;
            string reason = (string)options[2].Value;
            SocketGuildUser author = OwnerShard.TargetGuild.GetUser(command.User.Id);
            if (length < 30)
            {
                //too short of length
                await command.RespondAsync("Duration too short.", ephemeral: true);
                return;
            }
            bool result = moderationComponent.PunishUser(target, author, ModerationComponent.Punishment.Timeout, reason, TimeSpan.FromSeconds(length));
            if (!result)
            {
                await command.RespondAsync("An error occured. Contact btelnyy for more information.", ephemeral: true);
            }
            else
            {
                await command.RespondAsync("Success", ephemeral: true);
            }
        }

        public override void BuildOptions()
        {
            CommandOption cob = new()
            {
                Name = "user",
                Description = "user to mute",
                OptionType = ApplicationCommandOptionType.User,
                Required = true
            };
            CommandOption cob2 = new()
            {
                Name = "length",
                Description = "amount of seconds to mute them for",
                OptionType = ApplicationCommandOptionType.Integer,
                Required = true
            };
            CommandOption cob3 = new()
            {
                Name = "reason",
                Description = "string reason",
                OptionType = ApplicationCommandOptionType.String,
                Required = true
            };
            Options.Clear();
            Options.Add(cob);
            Options.Add(cob2);
            Options.Add(cob3);
        }
    }
}
