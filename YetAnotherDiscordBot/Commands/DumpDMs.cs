using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Attributes;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot.Commands
{
    [GlobalCommand]
    public class DumpDMs : Command
    {
        public override string CommandName => "dumpdms";

        public override GuildPermission RequiredPermission => GuildPermission.Administrator;

        public override string Description => "Dumps DMs of users. Note that only people registered as bot developers may do this.";

        public async override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if(OwnerShard == null)
            {
                await command.RespondAsync("Sorry, an error has occured.", ephemeral: true);
                return;
            }
            if (!ConfigurationService.GlobalConfiguration.DeveloperIDs.Contains(command.User.Id))
            {
                await command.RespondAsync(embed: new EmbedBuilder().GetErrorEmbed("Error", "You are not authorized to run this command."), ephemeral: true);
                return;
            }
            SocketSlashCommandDataOption[] options = (SocketSlashCommandDataOption[])GetOptionsOrdered(command.Data.Options.ToList());
            string input = (string)options[0];
            if (!long.TryParse(input, out long userId))
            {
                await command.RespondAsync(embed: new EmbedBuilder().GetErrorEmbed("Error", "You must enter a valid discord ID."), ephemeral: true);
                return;
            }
            SocketUser? targetUser = OwnerShard.Client.GetUser((ulong)userId);
            if(targetUser == null)
            {
                await command.RespondAsync(embed: new EmbedBuilder().GetErrorEmbed("Error", "Can't find user by ID."), ephemeral: true);
                return;
            }
            IDMChannel? targetChannel = targetUser.CreateDMChannelAsync().Result;
            if(targetChannel == null)
            {
                await command.RespondAsync(embed: new EmbedBuilder().GetErrorEmbed("Error", "Can't open DMs with that user."), ephemeral: true);
                return;
            }
            IDMChannel? selfDmChannel = command.User.CreateDMChannelAsync().Result;
            if(selfDmChannel == null)
            {
                await command.RespondAsync(embed: new EmbedBuilder().GetErrorEmbed("Error", "Can't open DMs with you."), ephemeral: true);
                return;
            }
            await command.RespondAsync("A text file containing all messages will be sent in your DMs with this bot as soon as possible.", ephemeral: true);
            List<IMessage> messages = targetChannel.GetMessagesAsync().FlattenAsync().Result.ToList();
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string filename = $"{userId}-{date}.txt";
            string filepath = ConfigurationService.ConfigFolder + "cache/" + filename;
            string data = "";
            foreach(IMessage message in messages)
            {
                string msg = $"[{message.Author.Username} ({message.Author.Id}), {message.CreatedAt}]: {message.Content} \n";
                if (message.Embeds.Count != 0)
                {
                    foreach(Attachment attachment in message.Attachments)
                    {
                        msg += attachment.Url + " ";
                    }
                }
                data += msg;
            }
            Directory.CreateDirectory(ConfigurationService.ConfigFolder + "cache/");
            File.WriteAllText(filepath, data);
            await selfDmChannel.SendFileAsync(filepath);
            File.Delete(filepath);
        }

        public override void BuildOptions()
        {
            base.BuildOptions();
            CommandOption cob = new CommandOption()
            {
                Name = "user",
                Description = "User ID of the target user.",
                Required = true,
                OptionType = ApplicationCommandOptionType.String,
            };
            Options.Clear();
            Options.Add(cob);
        }
    }
}
