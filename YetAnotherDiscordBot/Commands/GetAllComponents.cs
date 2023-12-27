using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Attributes;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.Commands
{
    internal class GetAllComponents : Command
    {
        public override string CommandName => "getallcomponents";

        public override string Description => "Gets all available components for this shard.";

        public override GuildPermission RequiredPermission => GuildPermission.ManageGuild;

        public override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            string?[] components = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetCustomAttribute<BotComponent>() != null).Select(x => x.FullName).ToArray();
            if(components == null)
            {
                components = new string[1] { "Nothing to show." };
            }
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Color = Color.Blue;
            embedBuilder.WithTitle("Components");
            embedBuilder.WithCurrentTimestamp();
            embedBuilder.WithDescription($"```{string.Join(",\n", components)}```");
            command.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
        }
    }
}
