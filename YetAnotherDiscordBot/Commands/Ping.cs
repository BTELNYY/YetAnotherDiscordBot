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
    public class Ping : Command
    {
        public override string CommandName => "ping";
        public override string Description => "Pong!";
        public override bool IsDefaultEnabled => true;
        public override GuildPermission RequiredPermission => GuildPermission.UseApplicationCommands;
        public override async void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if(ShardWhoRanMe == null)
            {
                Log.GlobalError("ShardWhoRanMe is null but a command went off on it.");
                return;
            }
            await command.RespondAsync("Pong!" + " Current ping is " + ShardWhoRanMe.Client.Latency + "ms");
        }
    }
}
