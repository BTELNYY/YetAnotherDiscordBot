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
    public class SetRank : Command
    {
        public override string CommandName => "setrank";

        public override GuildPermission RequiredPermission => GuildPermission.ManageRoles;

        public override string Description => "Set the rank of a user.";

        public override bool UseLegacyExecute => true;

        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(RankSystemComponent),
        };

        public override void BuildOptions()
        {
            base.BuildOptions();
            CommandOption cob = new CommandOption()
            {
                Name = "target",
                Required = true,
                OptionType = ApplicationCommandOptionType.User,
                Description = "User which rank to set",
            };
            CommandOption cob1 = new CommandOption()
            {
                Name = "rank",
                Required = true,
                OptionType = ApplicationCommandOptionType.Integer,
                Description = "Integer of rank to set."
            };
            CommandOption cob2 = new CommandOption()
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

        public override void LegacyExecute(SocketSlashCommand command)
        {
            base.LegacyExecute(command);
            if(OwnerShard == null)
            {
                command.RespondAsync("An error occured: Shard is null.");
                return;
            }
            SocketSlashCommandDataOption[] options = (SocketSlashCommandDataOption[])GetOptionsOrdered(command.Data.Options.ToList());
            SocketGuildUser target = (SocketGuildUser)options[0].Value;
            long level = (long)options[1].Value;
            double xp = 0d;
            if (command.Data.Options.Count > 2)
            {
                xp = (double)options[2].Value;
            }
            DiscordUserRankData discordUserRankData = OwnerShard.DiscordUserDataService.GetData<DiscordUserRankData>(target.Id);
            RankSystemComponent component = (RankSystemComponent)OwnerShard.ComponentManager.GetComponent<RankSystemComponent>(out bool success);
            discordUserRankData.XP = (float)xp;
            discordUserRankData.Level = (uint)level;
            OwnerShard.DiscordUserDataService.WriteData(discordUserRankData, target.Id, true);
            command.RespondAsync("Success", ephemeral: true);
            component.ValidateUser(discordUserRankData);
        }
    }
}
