using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.ComponentSystem.RankSystemComponent.Commands
{
    public class Setrank : Command
    {
        public override string CommandName => "setrank";

        public override GuildPermission RequiredPermission => GuildPermission.ManageRoles;

        public override string Description => "Set the rank of a user.";

        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(RankSystemComponent),
        };

        public override void BuildOptions()
        {
            base.BuildOptions();
            CommandOptionsBase cob = new CommandOptionsBase()
            {
                Name = "target",
                Required = true,
                OptionType = ApplicationCommandOptionType.User,
                Description = "User which rank to set",
            };
            CommandOptionsBase cob1 = new CommandOptionsBase()
            {
                Name = "rank",
                Required = true,
                OptionType = ApplicationCommandOptionType.Integer,
                Description = "Integer of rank to set."
            };
            CommandOptionsBase cob2 = new CommandOptionsBase()
            {
                Name = "xp",
                Required = false,
                OptionType = ApplicationCommandOptionType.Number,
                Description = "The XP to set.",
            };
            Options.Clear();
            Options.Add(cob);
            Options.Add(cob1);
            Options.Add(cob2);
        }

        public override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if(OwnerShard == null)
            {
                command.RespondAsync("An error occured: Shard is null.");
                return;
            }
            SocketSlashCommandDataOption[] options = GetOptionsOrdered(command.Data.Options.ToList());
            SocketGuildUser target = (SocketGuildUser)options[0].Value;
            long level = (long)options[1].Value;
            double xp = 0d;
            if (command.Data.Options.Count > 2)
            {
                xp = (double)options[2].Value;
            }
            DiscordUserRankData discordUserRankData = OwnerShard.DiscordUserDataService.GetData<DiscordUserRankData>(target.Id);
            discordUserRankData.XP = (float)xp;
            discordUserRankData.Level = (uint)level;
            OwnerShard.DiscordUserDataService.WriteData(discordUserRankData, target.Id, true);
            command.RespondAsync("Success", ephemeral: true);
        }
    }
}
