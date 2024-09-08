using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.ComponentSystem.ModerationComponent.Commands
{
    public class PruneInactive : Command
    {
        public override string CommandName => "pruneinactive";

        public override string Description => "Removes users who have not interacted with anything in any discord channel in x amount of days.";

        public override GuildPermission RequiredPermission => GuildPermission.Administrator;

        public void Execute(SocketSlashCommand command, long days, bool countReactions = true)
        {

        }
    }
}
