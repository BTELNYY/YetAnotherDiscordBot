using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.ComponentSystem;

namespace YetAnotherDiscordBot.Commands
{
    public class AddComponent : Command
    {
        public override string CommandName => "addcomponent";

        public override string Description => "Adds a component to the thread.";

        public override GuildPermission RequiredPermission => GuildPermission.Administrator;

        public override void BuildOptions()
        {
            base.BuildOptions();
            CommandOptionsBase commandOptionsBase = new CommandOptionsBase()
            {
                Name = "components",
                OptionType = ApplicationCommandOptionType.String,
                Required = true,
                Description = "The component full name (namespace and name, use /getallcomponents to see them)"
            };
            Options.Clear();
            Options.Add(commandOptionsBase);
        }

        public override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if(OwnerShard == null)
            {
                Log.Error("OwnerShard is null!");
                command.RespondAsync("An error occured: OwnerShard is null!", ephemeral: true);
                return;
            }
            SocketSlashCommandDataOption[] options = GetOptionsOrdered(command.Data.Options.ToList());
            string componentName = (string)options[0];
            Component component = ComponentManager.GetComponentByName(componentName, out bool succes);
            if (!succes)
            {
                command.RespondAsync("Can't find the component you mean!", ephemeral: true);
                return;
            }
            OwnerShard.ServerConfiguration.AddedComponents.Add(componentName);
            OwnerShard.SaveServerConfiguration();
            command.RespondAsync("Succes, restart the shard to apply changes.", ephemeral: true);
        }
    }
}
