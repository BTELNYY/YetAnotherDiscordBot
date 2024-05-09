using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.ComponentSystem;
using YetAnotherDiscordBot.ComponentSystem.ModerationComponent;

namespace YetAnotherDiscordBot.ComponentSystem.ModerationComponent.Commands
{
    public class KickUser : Command
    {
        public override string CommandName => "kickuser";
        public override string Description => "kick a user from this server.";
        public override GuildPermission RequiredPermission => GuildPermission.KickMembers;
        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(ModerationComponent),
        };
        public override async void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if (OwnerShard is null)
            {
                return;
            }
            ModerationComponent moderationComponent = OwnerShard.ComponentManager.GetComponent<ModerationComponent>(out bool success);
            if (!success)
            {
                await command.RespondAsync("An error occured. Contact btelnyy for more details.", ephemeral: true);
                Log.Warning("Failed to fetch Moderation Component!");
                return;
            }
            SocketSlashCommandDataOption[] options = (SocketSlashCommandDataOption[])GetOptionsOrdered(command.Data.Options.ToList());
            SocketGuildUser user = (SocketGuildUser)options[0].Value;
            string reason = (string)options[1].Value;
            SocketGuildUser author = OwnerShard.TargetGuild.GetUser(command.User.Id);
            bool senddm = (bool)options[2].Value;
            bool showPublicMessage = (bool)options[3].Value;
            bool result = moderationComponent.PunishUser(user, author, ModerationComponent.Punishment.Kick, reason, sendDm: senddm);
            if (!result)
            {
                await command.RespondAsync("An error occured. Contact btelnyy for more information.", ephemeral: true);
            }
            else
            {
                await command.RespondAsync("Success", ephemeral: true);
                if (showPublicMessage)
                {
                    EmbedBuilder builder = ModerationComponent.GetActionEmbed(user, author, reason, ModerationComponent.Punishment.Kick);
                    await command.Channel.SendMessageAsync(embed: builder.Build());
                }
            }
        }

        public override void BuildOptions()
        {
            base.BuildOptions();
            CommandOption cob = new CommandOption()
            {
                Name = "user",
                Description = "User which to kick",
                OptionType = ApplicationCommandOptionType.User,
                Required = true,
            };
            CommandOption cob1 = new CommandOption()
            {
                Name = "reason",
                Description = "Reason for being kicked",
                OptionType = ApplicationCommandOptionType.String,
                Required = true
            };
            CommandOption cob2 = new CommandOption()
            {
                Name = "senddm",
                Description = "Should the bot message the kicked user?",
                OptionType = ApplicationCommandOptionType.Boolean,
                Required = true
            };
            CommandOption cob3 = new CommandOption()
            {
                Name = "showpublicmsg",
                Description = "Should the bot display a public message?",
                OptionType = ApplicationCommandOptionType.Boolean,
                Required = true
            };
            Options.Clear();
            Options.Add(cob);
            Options.Add(cob1);
            Options.Add(cob2);
            Options.Add(cob3);
        }
    }
}
