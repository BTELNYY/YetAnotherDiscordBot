﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.ComponentSystem.PingPreventionComponent.Commands
{
    public class RemoveProtectedUser : Command
    {
        public override string CommandName => "removepingprotecteduser";

        public override string Description => "Removes a user to the list of users to prevent pinging of in this server.";

        public override GuildPermission RequiredPermission => GuildPermission.ManageGuild;

        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(PingPrevention)
        };

        public override bool UseLegacyExecute => true;

        public async override void LegacyExecute(SocketSlashCommand command)
        {
            base.LegacyExecute(command);
            if (OwnerShard == null)
            {
                DisplayError(command);
                return;
            }
            if (OptionsProcessed[0].Value is not SocketGuildUser target)
            {
                DisplayError(command);
                Log.Error($"Got an invalid object for a user.");
                return;
            }
            PingPrevention component = OwnerShard.ComponentManager.GetComponent<PingPrevention>(out bool success);
            if (!success)
            {
                DisplayError(command);
                Log.Error("Unable to find PingPrevention component!");
                return;
            }
            if (!component.Configuration.PreventedIDs.Contains(target.Id))
            {
                await command.RespondAsync("That user is not on the ping prevention list.", ephemeral: true);
                return;
            }
            component.Configuration.PreventedIDs.Add(target.Id);
            bool result = component.Configuration.Save();
            if (!result)
            {
                await command.RespondAsync("Failed to save configuration. Try again later.");
                return;
            }
            await command.RespondAsync("Success");
        }

        public override void BuildOptions()
        {
            base.BuildOptions();
            Options = new List<CommandOption>()
            {
                new CommandOption()
                {
                    Name = "user",
                    Description = "Which user to target",
                    Required = true,
                    OptionType = ApplicationCommandOptionType.User
                },
            };
        }
    }
}
