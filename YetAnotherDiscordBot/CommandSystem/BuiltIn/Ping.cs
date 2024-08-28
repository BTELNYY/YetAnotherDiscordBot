using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.Attributes;

namespace YetAnotherDiscordBot.CommandSystem.BuiltIn
{
    [GlobalCommand]
    public class Ping : Command
    {
        public override string CommandName => "ping";
        public override string Description => "Pong!";
        public override bool IsDefaultEnabled => true;
        public override GuildPermission RequiredPermission => GuildPermission.UseApplicationCommands;
        public override bool UseLegacyExecute => true;
        public override async void LegacyExecute(SocketSlashCommand command)
        {
            base.LegacyExecute(command);
            if (OwnerShard == null)
            {
                Log.GlobalError("ShardWhoRanMe is null but a command went off on it.");
                return;
            }
            await command.RespondAsync("Pong!" + " Current ping is " + OwnerShard.Client.Latency + "ms");
        }
    }
}
