using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.Commands
{
    public class ActiveComponents : Command
    {
        public override string CommandName => "activecomponents";

        public override GuildPermission RequiredPermission => GuildPermission.Administrator;

        public override string Description => "Lists all currently active components.";

        public override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if(OwnerShard == null)
            {
                command.RespondAsync("An error occured: OwnerShard is null.", ephemeral: true);
                return;
            }
            string?[] components = OwnerShard.ComponentManager.CurrentComponents.Where(x => x.HasLoaded).Select(x => x.GetType().FullName).ToArray();
            if(components == null)
            {
                command.RespondAsync("An error occured: List of components is null.", ephemeral: true);
                return;
            }
            string resultStr = string.Join(",\n", components);
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(Color.Blue);
            builder.WithTitle("Active Components");
            builder.WithCurrentTimestamp();
            if(resultStr == "")
            {
                resultStr = "No active components";
            }
            builder.WithDescription("```" + resultStr + "```");
            command.RespondAsync(embed: builder.Build(), ephemeral: true);
        }
    }
}
