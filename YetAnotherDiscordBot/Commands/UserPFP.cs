using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.Commands
{
    public class UserPFP : Command
    {
        public override string CommandName => "userpfp";

        public override string Description => "Get a users profile picture.";

        public override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            SocketSlashCommandDataOption[] options = GetOptionsOrdered(command.Data.Options.ToList());
            SocketGuildUser user = (SocketGuildUser)options[0].Value;
            ulong id = user.Id;
            bool HasGuildPfp = false;
            string url = "";
            if (user.GetGuildAvatarUrl(size: 512) != null)
            {
                HasGuildPfp = true;
            }
            EmbedBuilder eb = new();
            eb.Title = user.Username + " (" + id + ")";
            eb.AddField("Has Guild Profile Picture?", HasGuildPfp.ToString());
            eb.ImageUrl = user.GetAvatarUrl(size: 128);
            url = user.GetAvatarUrl();
            string guildurl = user.GetGuildAvatarUrl(size: 128);
            eb.AddField("Global Profile Picture URL", url);
            if (HasGuildPfp && guildurl != null)
            {
                if (guildurl == string.Empty || guildurl == null)
                {
                    Log.Warning("Guild URL is NULL!");
                }
                else
                {
                    eb.AddField("Guild Profile Picture URL", guildurl);
                    eb.ImageUrl = guildurl;
                }
            }
            eb.WithCurrentTimestamp();
            Embed embed = eb.Build();
            command.RespondAsync(embed: embed);
        }

        public override void BuildOptions()
        {
            base.BuildOptions();
            CommandOptionsBase cob = new CommandOptionsBase()
            {
                Name = "user",
                Description = "User to get the PFP from.",
                Required = true,
                OptionType = ApplicationCommandOptionType.User
            };
            Options.Clear();
            Options.Add(cob);
        }
    }
}
