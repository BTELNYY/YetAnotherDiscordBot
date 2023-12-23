using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.Commands
{
    public class RestartShard : Command
    {
        public override string CommandName => "restartshard";

        public override GuildPermission RequiredPermission => GuildPermission.Administrator;

        public override string Description => "Restarts the current shard and applies all settings which were modified.";

        public async override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if(OwnerShard == null)
            {
                await command.RespondAsync("An error occured: Shard is null.", ephemeral: true);
                return;
            }
            await command.RespondAsync("Restarting shard. Commands may break while this is happening.", ephemeral: true);
            Thread thread = new Thread(DoRestartTask);
            thread.Start(OwnerShard.GuildID);
        }

        private void DoRestartTask(object? obj)
        {
            if(obj is not ulong)
            {
                Log.Error("Failed to restart thread: not a valid ulong!");
            }
            else
            {
                ulong id = (ulong)obj;
                Program.RestartShard(id);
            }
            return;
        }
    }
}
