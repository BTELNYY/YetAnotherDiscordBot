using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.Commands
{
    public class DestroyAppCommands : Command
    {
        public override string CommandName => "destroycommands";
        public override string Description => "Destroy ALL global application commands, then restart.";
        public override GuildPermission RequiredPermission => GuildPermission.Administrator;

        public override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if(ShardWhoRanMe == null)
            {
                return;
            }
            command.RespondAsync("Destroying all commands.");
            var commands = ShardWhoRanMe.Client.GetGlobalApplicationCommandsAsync();
            commands.Wait();
            foreach (var thing in commands.Result)
            {
                Log.Info("Destroying command: " + thing.Name);
                thing.DeleteAsync().Wait();
            }
            command.Channel.SendMessageAsync("Commands Destroyed, restarting.");
            Environment.Exit(1);
        }
    }
}
