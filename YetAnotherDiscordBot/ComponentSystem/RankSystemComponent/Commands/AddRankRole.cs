using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.ComponentSystem.ModerationComponent;

namespace YetAnotherDiscordBot.ComponentSystem.RankSystemComponent.Commands
{
    public class AddRankRole : Command
    {
        public override string CommandName => "addrankrole";

        public override GuildPermission RequiredPermission => GuildPermission.ManageRoles;

        public override string Description => "Adds a rank role to the current guild.";

        public override bool UseLegacyExecute => true;

        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(RankSystemComponent)
        };

        public override void LegacyExecute(SocketSlashCommand command)
        {
            base.LegacyExecute(command);
            if(OwnerShard == null)
            {
                EmbedBuilder builder = new EmbedBuilder();
                Embed embed = builder.GetErrorEmbed("Error", $"Can't use this command without a guild.");
                command.RespondAsync(embed: embed, ephemeral: true);
                return;
            }
            SocketGuildUser user = OwnerShard.TargetGuild.GetUser(command.User.Id);
            SocketSlashCommandDataOption[] options = (SocketSlashCommandDataOption[])GetOptionsOrdered(command.Data.Options.ToList());
            double level = (double)options[0].Value;
            SocketRole targetRole = (SocketRole)options[1].Value;
            if(user.Hierarchy <= targetRole.Position)
            {
                EmbedBuilder builder = new EmbedBuilder();
                Embed embed = builder.GetErrorEmbed("Error", $"Requested role is higher or the same as your highest role.");
                command.RespondAsync(embed: embed, ephemeral: true);
                return;
            }
            if (OwnerShard.TargetGuild.GetUser(OwnerShard.Client.CurrentUser.Id).Hierarchy <= targetRole.Position)
            {
                EmbedBuilder builder = new EmbedBuilder();
                Embed embed = builder.GetErrorEmbed("Error", $"Requested role is higher or the same as the bots highest role.");
                command.RespondAsync(embed: embed, ephemeral: true);
                return;
            }
            string action = (string)options[2].Value;
            bool parsed = Enum.TryParse(typeof(RoleAction), action, out object? parsedObj);
            if (!parsed || parsedObj == null)
            {
                EmbedBuilder builder = new EmbedBuilder();
                Embed embed = builder.GetErrorEmbed("Error", $"Can't parse action '{action}' (try Add or Remove).");
                command.RespondAsync(embed: embed, ephemeral: true);
                return;
            }
            RoleAction roleAction = (RoleAction)parsedObj;
            RankSystemComponent component = OwnerShard.ComponentManager.GetComponent<RankSystemComponent>(out bool success);
            if (!success)
            {
                EmbedBuilder builder = new EmbedBuilder();
                Embed embed = builder.GetErrorEmbed("Error", $"Can't find the RankSystemComponent, cannot proceed.");
                command.RespondAsync(embed: embed, ephemeral: true);
                return;
            }
            bool actionCompleted = component.AddRankRole((uint)level, targetRole.Id, roleAction, out string error);
            if (!actionCompleted)
            {
                EmbedBuilder builder = new EmbedBuilder();
                Embed embed = builder.GetErrorEmbed("Error", error);
                command.RespondAsync(embed: embed, ephemeral: true);
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
                Name = "level",
                Description = "The level at which the role is unlocked at.",
                OptionType = ApplicationCommandOptionType.Number,
                Required = true,
            };
            Options.Add(cob);
            CommandOption cob1 = new CommandOption()
            {
                Name = "role",
                Description = "The target role",
                OptionType = ApplicationCommandOptionType.Role,
                Required = true,
            };
            Options.Add(cob1);
            CommandOption cob2 = new CommandOption()
            {
                Name = "action",
                Description = "What action to do with the role ('Add' or 'Remove')",
                OptionType = ApplicationCommandOptionType.String,
                Required = true,
            };
            Options.Add(cob2);
        }
    }
}
