using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.ComponentSystem.RankSystemComponent.Commands
{
    public class ListRankRoles : Command
    {
        public override string CommandName => "listrankroles";

        public override string Description => "List the current rank roles.";

        public override GuildPermission RequiredPermission => GuildPermission.ManageRoles;

        public override List<Type> RequiredComponents => new List<Type> { typeof(RankSystemComponent) };

        public override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if (OwnerShard == null)
            {
                EmbedBuilder builder = new EmbedBuilder();
                Embed embed = builder.GetErrorEmbed("Error", $"Can't use this command without a guild.");
                command.RespondAsync(embed: embed, ephemeral: true);
                return;
            }
            RankSystemComponent component = OwnerShard.ComponentManager.GetComponent<RankSystemComponent>(out bool success);
            if (!success)
            {
                EmbedBuilder builder = new EmbedBuilder();
                Embed embed = builder.GetErrorEmbed("Error", $"Can't find the RankSystemComponent, cannot proceed.");
                command.RespondAsync(embed: embed, ephemeral: true);
                return;
            }
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithCurrentTimestamp();
            eb.WithTitle("Current Rank Roles");
            eb.WithColor(Color.Blue);
            string desc = "No rank roles in this server.";
            if (component.Configuration.RankRoleConfiguration.RoleData.Any())
            {
                desc = "```LEVEL, ID, NAME, ACTION\n";
                foreach (RankRoleData data in component.Configuration.RankRoleConfiguration.RoleData)
                {
                    string rolename = "[MISSING ROLE]";
                    if (OwnerShard.TargetGuild.GetRole(data.RoleId) != null)
                    {
                        rolename = OwnerShard.TargetGuild.GetRole(data.RoleId).Name;
                    }
                    desc += $"{data.RequiredLevel}, {data.RoleId}, {rolename}, {data.RoleAction}";
                    if (component.Configuration.RankRoleConfiguration.RoleData.Last() != data)
                    {
                        desc += "\n";
                    }
                }
                desc += "```";
            }
            eb.WithDescription(desc);
            command.RespondAsync(embed: eb.Build(), ephemeral: true);
        }
    }
}
