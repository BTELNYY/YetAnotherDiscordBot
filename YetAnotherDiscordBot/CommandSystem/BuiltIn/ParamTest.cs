using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.Attributes;
using Discord;
using Discord.WebSocket;

namespace YetAnotherDiscordBot.CommandSystem.BuiltIn
{
    [GlobalCommand]
    public class ParamTest : Command
    {
        public override string CommandName => "paramtest";

        public override string Description => "Tests the new param system.";

        public override GuildPermission RequiredPermission => GuildPermission.Administrator;

        [CommandMethod]
        public void Execute(SocketSlashCommand command, string data, long data1, SocketGuildUser? user = null)
        {
            string userName = "noUser";
            if(user != null)
            {
                userName = user.GlobalName; 
            }
            command.RespondAsync($"{data}, {data1}, {userName}");
        }
    }
}
