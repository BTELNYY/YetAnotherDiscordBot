using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.ComponentSystem.WackerySLAPI;

namespace YetAnotherDiscordBot.Commands.SLStuff
{
    public class GetPlayerDetails : Command
    {
        public override string CommandName => "getplayerdetails";
        public override string Description => "Get details on a player";
        public override bool IsDefaultEnabled => true;
        public override GuildPermission RequiredPermission => GuildPermission.UseApplicationCommands;

        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(SLAPIComponent)
        };

        public override async void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            SocketSlashCommandDataOption[] options = GetOptionsOrdered(command.Data.Options.ToList());
            Embed[] embeds;
            if (ShardWhoRanMe == null)
            {
                await command.RespondAsync(embed: new EmbedBuilder().GetErrorEmbed("Error", "Cannot run this in DMs."));
                return;
            }
            SLAPIComponent slAPIComponent = ShardWhoRanMe.ComponentManager.GetComponent<SLAPIComponent>(out bool succes);
            if (!succes)
            {
                await command.RespondAsync(embed: new EmbedBuilder().GetErrorEmbed("Error", "Cannot get required component."));
                return;
            }
            PlayerStats stats = slAPIComponent.GetPlayerStats((string)options[0].Value);
            //lol what is this fucking check?
            if (stats.PlayTime == -1)
            {
                await command.RespondAsync(embed: new EmbedBuilder().GetErrorEmbed("Error", "Failed to get user."));
                return;
            }
            EmbedBuilder eb = new();
            eb.WithColor(Color.Blue);
            eb.WithTitle("Player details: " + stats.LastNickname);
            eb.AddField("Steam ID", stats.SteamID);
            eb.AddField("First Seen", stats.FirstSeen.ToString("dd-MM-yyyy") + " at " + stats.FirstSeen.ToString("hh\\:mm\\:ss"));
            eb.AddField("Last Seen", stats.LastSeen.ToString("dd-MM-yyyy") + " at " + stats.LastSeen.ToString("hh\\:mm\\:ss"));
            if (stats.Usernames.Count > 0)
            {
                string NameStr = "```";
                NameStr += string.Join("\n", stats.Usernames);
                NameStr += "```";
                eb.AddField($"{"Old Names"} ({stats.Usernames.Count})", NameStr);
            }
            if (stats.PFlags.Count > 0)
            {
                string FlagsStr = "```";
                FlagsStr += "(ID), (FLAG), ISSUER, (COMMENTS), (ISSUE TIME) \n";
                foreach (Flags f in stats.PFlags)
                {

                    FlagsStr += stats.PFlags.IndexOf(f) + ", ";
                    FlagsStr += f.Flag.ToString() + ", ";
                    FlagsStr += f.Issuer + ", ";
                    FlagsStr += f.Comment + ", ";
                    FlagsStr += f.IssueTime.ToString("dd-MM-yyyy") + " at " + f.IssueTime.ToString("hh\\:mm\\:ss") + "\n";
                }
                FlagsStr += "```";
                eb.AddField($"{"Flags"} ({stats.PFlags.Count})", FlagsStr);
            }
            TimeSpan t = TimeSpan.FromSeconds(stats.PlayTime);
            string answer = string.Format("{0:D2}h {1:D2}m {2:D2}s",
                            t.Hours + t.Days * 24,
                            t.Minutes,
                            t.Seconds);
            eb.AddField("Playtime", answer);
            if (stats.LoginAmount != 0)
            {
                eb.AddField("Logins", stats.LoginAmount.ToString());
            }
            if (stats.TimeOnline != 0)
            {
                TimeSpan t1 = TimeSpan.FromSeconds(stats.TimeOnline);
                string answer1 = string.Format("{0:D2}h {1:D2}m {2:D2}s",
                                t1.Hours + t1.Days * 24,
                                t1.Minutes,
                                t1.Seconds);
                eb.AddField("Time Online", answer1);
            }
            eb.WithCurrentTimestamp();
            Embed embed = eb.Build();
            embeds = new Embed[1] { embed };
            try
            {
                await command.RespondAsync("", embeds);
            }
            catch (Exception ex)
            {
                await command.RespondAsync("An error occured! \n ```" + ex.ToString() + "```");
            }
        }

        public override void BuildOptions()
        {
            CommandOptionsBase cob = new()
            {
                Name = "user",
                Description = "name of online user or ID64 of user.",
                OptionType = ApplicationCommandOptionType.String,
                Required = true
            };
            Options.Clear();
            Options.Add(cob);
        }
    }
}