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
    public class BanUser : Command
    {
        public override string CommandName => "banuser";
        public override string Description => "ban a user from this server. (this command does not delete recent messages)";
        public override GuildPermission RequiredPermission => GuildPermission.BanMembers;
        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(ModerationComponent),
        };

        public override async void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if (ShardWhoRanMe is null)
            {
                return;
            }
            ModerationComponent moderationComponent = ShardWhoRanMe.ComponentManager.GetComponent<ModerationComponent>(out bool success);
            if (!success)
            {
                await command.RespondAsync("An error occured. Contact btelnyy for more details.", ephemeral: true);
                Log.Warning("Failed to fetch Moderation Component!");
                return;
            }
            SocketSlashCommandDataOption[] options = GetOptionsOrdered(command.Data.Options.ToList());
            SocketGuildUser user = (SocketGuildUser)options[0].Value;
            SocketGuildUser author = ShardWhoRanMe.TargetGuild.GetUser(command.User.Id);
            string reason = (string)options[1].Value;
            bool senddm = (bool)options[2].Value;
            bool result = moderationComponent.PunishUser(user, author, ModerationComponent.Punishment.Ban, reason, sendDm: senddm);
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
            base.BuildOptions();
            CommandOptionsBase cob = new CommandOptionsBase()
            {
                Name = "user",
                Description = "User which to ban",
                OptionType = ApplicationCommandOptionType.User,
                Required = true,
            };
            CommandOptionsBase cob1 = new CommandOptionsBase()
            {
                Name = "reason",
                Description = "Reason for being banned",
                OptionType = ApplicationCommandOptionType.String,
                Required = true
            };
            CommandOptionsBase cob2 = new CommandOptionsBase()
            {
                Name = "senddm",
                Description = "Should the bot message the banned user?",
                OptionType = ApplicationCommandOptionType.Boolean,
                Required = true
            };
            Options.Clear();
            Options.Add(cob);
            Options.Add(cob1);
            Options.Add(cob2);
        }
    }
}
