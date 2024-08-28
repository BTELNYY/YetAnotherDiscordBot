using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.ComponentSystem.PingPreventionComponent.Commands
{
    public class ListProtectedUsers : Command
    {
        public override string CommandName => "pingprotectedlist";

        public override string Description => "Lists ping protected users.";

        public override GuildPermission RequiredPermission => GuildPermission.ManageGuild;

        public override bool UseLegacyExecute => true;

        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(PingPrevention)
        };

        public override void LegacyExecute(SocketSlashCommand command)
        {
            base.LegacyExecute(command);
            if(OwnerShard == null)
            {
                DisplayError(command);
                return;
            }
            PingPrevention component = OwnerShard.ComponentManager.GetComponent<PingPrevention>(out bool success);
            if(!success)
            {
                DisplayError(command);
                Log.Error("Unable to get PingPrevention component.");
                return;
            }
            string compiledList = "";
            bool doSave = false;
            foreach(ulong userId in component.Configuration.PreventedIDs)
            {
                if(OwnerShard.TargetGuild.GetUser(userId) == null && component.Configuration.RemoveUsersNotInServer)
                {
                    component.Configuration.PreventedIDs.Remove(userId);
                    doSave = true;
                    continue;
                }
                compiledList += $"* {userId}, <@{userId}>\n";
            }
            if(doSave)
            {
                component.Configuration.Save();
            }
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle($"Ping Protected Users ({compiledList.Count()})");
            builder.WithDescription(compiledList);
            builder.WithCurrentTimestamp();
            builder.WithColor(Color.Blue);
            command.RespondAsync(embed: builder.Build(), ephemeral: true);
        }
    }
}
