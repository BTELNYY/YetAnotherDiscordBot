using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.Attributes;

namespace YetAnotherDiscordBot.Commands
{
    [GlobalCommand]
    public class DestroyAppCommands : Command
    {
        public override string CommandName => "destroycommands";
        public override string Description => "Destroy ALL shard commands, then restart the shard.";
        public override GuildPermission RequiredPermission => GuildPermission.Administrator;

        public override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if(OwnerShard == null)
            {
                return;
            }
            command.RespondAsync("Destroying all commands.");
            var commands = OwnerShard.TargetGuild.GetApplicationCommandsAsync().Result.Where(x => x.ApplicationId == OwnerShard.Client.GetApplicationInfoAsync().Result.Id);
            foreach (var thing in commands)
            {
                Log.Info("Destroying command: " + thing.Name);
                thing.DeleteAsync().Wait();
            }
            command.Channel.SendMessageAsync("Commands Destroyed, restarting.");
            Environment.Exit(1);
            Program.RestartShard(OwnerShard.GuildID);
        }
    }
}
