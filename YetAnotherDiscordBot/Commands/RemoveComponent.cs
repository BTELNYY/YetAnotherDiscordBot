﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.ComponentSystem;

namespace YetAnotherDiscordBot.Commands
{
    public class RemoveComponent : Command
    {
        public override string CommandName => "removecomponent";

        public override string Description => "Removes a component, restart for full effect.";

        public override GuildPermission RequiredPermission => GuildPermission.Administrator;

        public override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if(OwnerShard == null)
            {
                command.RespondAsync("An error occured: OwnerShard is null.", ephemeral: true);
                return;
            }
            SocketSlashCommandDataOption[] options = GetOptionsOrdered(command.Data.Options.ToList());
            string component = (string)options[0].Value;
            Component componentClass = ComponentManager.GetComponentByName(component, out bool success);
            if (!success)
            {
                command.RespondAsync("That is not a valid component.", ephemeral: true);
                return;
            }
            if(!OwnerShard.ServerConfiguration.AddedComponents.Contains(component))
            {
                command.RespondAsync("Sorry, That component isn't added on this shard.", ephemeral: true);
                return;
            }
            OwnerShard.ServerConfiguration.AddedComponents.Remove(component);
            OwnerShard.SaveServerConfiguration();
            command.RespondAsync("Success. Restart the shard to apply changes.");
        }

        public override void BuildOptions()
        {
            base.BuildOptions();
            CommandOptionsBase cob = new CommandOptionsBase()
            {
                Name = "component",
                Required = true,
                OptionType = ApplicationCommandOptionType.String,
                Description = "Fullname of the component to remove."
            };
            Options.Clear();
            Options.Add(cob);
        }
    }
}
