using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.ComponentSystem.RankSystemComponent.Commands
{
    public class Rank : Command
    {
        public override string CommandName => "rank";

        public override string Description => "Gets your rank within the current server.";

        public override bool UseLegacyExecute => true;

        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(RankSystemComponent),
        };

        public override void LegacyExecute(SocketSlashCommand command)
        {
            base.LegacyExecute(command);
            if(OwnerShard == null)
            {
                command.RespondAsync("An error has occured: Shard reference is null!");
                return;
            }
            RankSystemComponent rankSystemComponent = OwnerShard.ComponentManager.GetComponent<RankSystemComponent>(out bool success);
            if (!success)
            {
                command.RespondAsync("Failed to get RankSystemComponent!");
                return;
            }
            SocketSlashCommandDataOption[] optionsOrdered = (SocketSlashCommandDataOption[])GetOptionsOrdered(command.Data.Options.ToList());
            SocketGuildUser target = (SocketGuildUser)command.User;
            if(command.Data.Options.Count > 0) 
            {
                target = (SocketGuildUser)optionsOrdered[0].Value;
            }
            DiscordUserRankData discordUserRankData = OwnerShard.DiscordUserDataService.GetData<DiscordUserRankData>(target.Id);
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle("Rank data for " + target.GlobalName);
            embed.WithCurrentTimestamp();
            embed.WithColor(Color.DarkGreen);
            embed.AddField("Level", discordUserRankData.Level);
            embed.AddField("XP", $"{discordUserRankData.XP}/{rankSystemComponent.Configuration.RankAlgorithmConfiguratiom.FindXP(discordUserRankData.Level)}");
            embed.AddField("XP Locked?", discordUserRankData.XPLocked);
            embed.AddField("Personal XP Multiplier", $"{discordUserRankData.PersonalXPMultiplier} or {discordUserRankData.PersonalXPMultiplier * 100}%");
            command.RespondAsync(embed: embed.Build());
        }

        public override void BuildOptions()
        {
            base.BuildOptions();
            CommandOption cob = new CommandOption
            {
                Name = "user",
                Description = "User whos rank you wish to see",
                OptionType = ApplicationCommandOptionType.User,
                Required = false
            };
            Options.Clear();
            Options.Add(cob);
        }
    }
}
