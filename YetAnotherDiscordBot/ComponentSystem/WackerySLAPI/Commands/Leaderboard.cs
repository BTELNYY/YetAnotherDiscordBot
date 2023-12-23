using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.ComponentSystem.WackerySLAPI.Commands
{
    public class Leaderboard : Command
    {
        public override string CommandName => "leaderboard";
        public override string Description => "get the play time leaderboard";
        public override GuildPermission RequiredPermission => base.RequiredPermission;
        public override bool IsDefaultEnabled => true;
        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(SLAPIComponent),
        };
        public override async void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if (OwnerShard == null)
            {
                await command.RespondAsync(embed: new EmbedBuilder().GetErrorEmbed("Error", "Cannot run this in DMs."));
                return;
            }
            SLAPIComponent slAPIComponent = OwnerShard.ComponentManager.GetComponent<SLAPIComponent>(out bool succes);
            if (!succes)
            {
                await command.RespondAsync(embed: new EmbedBuilder().GetErrorEmbed("Error", "Cannot get required component."));
                return;
            }
            try
            {
                PlayerStats[] players = slAPIComponent.GetPlaytimeLeaderboard();
                EmbedBuilder eb = new();
                eb.WithColor(Color.Blue);
                eb.WithTitle("Playtime Leaderboard");
                string description = "```(PLACE): (USERNAME), (PLAYTIME) \n";
                int counter = 1;
                foreach (var player in players)
                {
                    TimeSpan t = TimeSpan.FromSeconds(player.PlayTime);
                    string answer = string.Format("{0:D2}h {1:D2}m {2:D2}s",
                                    t.Hours + t.Days * 24,
                                    t.Minutes,
                                    t.Seconds);
                    description += $"{counter}: {player.LastNickname}, {answer} \n";
                    counter++;
                }
                description += "```";
                eb.WithCurrentTimestamp();
                eb.WithDescription(description);
                await command.RespondAsync(embed: eb.Build());
            }
            catch (Exception ex)
            {
                await command.Channel.SendMessageAsync("An error occured: \n " + ex.ToString());
                Log.Error("Error executing command. \n" + ex.ToString());
            }
        }
    }
}
