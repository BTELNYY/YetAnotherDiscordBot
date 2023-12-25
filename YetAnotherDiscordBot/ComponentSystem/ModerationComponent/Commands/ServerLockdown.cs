﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.ComponentSystem.ModerationComponent.Commands
{
    public class ServerLockdown : Command
    {
        public override string CommandName => "serverlockdown";

        public override string Description => "Locks all channels in the server.";

        public override GuildPermission RequiredPermission => GuildPermission.Administrator;

        public override List<Type> RequiredComponents => new List<Type>() 
        {
            typeof(ModerationComponent), 
        };

        public override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if(OwnerShard == null)
            {
                command.RespondAsync("An error occured: OwnerShard is null.", ephemeral: true);
                return;
            }
            ModerationComponent moderationComponent = OwnerShard.ComponentManager.GetComponent<ModerationComponent>(out bool success);
            if (!success)
            {
                command.RespondAsync("An error occured: ModerationComponent is not found.", ephemeral: true);
                return;
            }
            List<SocketTextChannel> channels = OwnerShard.TargetGuild.TextChannels.ToList();
            foreach(SocketTextChannel channel in channels)
            {
                moderationComponent.LockdownChannel(channel, (SocketGuildUser)command.User);
            }
            command.RespondAsync("Success.", ephemeral: true);
        }
    }
}
