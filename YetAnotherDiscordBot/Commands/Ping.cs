using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Base;
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
            await command.RespondAsync("Pong!" + " Current ping is " + BotWhoRanMe.Client.Latency + "ms");
        }
    }
}
