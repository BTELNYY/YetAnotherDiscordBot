using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot.ComponentSystem.ChannelUtilsComponent.Commands
{
    
    public class GetAllMessages : Command
    {
        public override string CommandName => "getallmessages";

        public override string Description => "Get all the messages in current channel and send them to you.";

        public override GuildPermission RequiredPermission => GuildPermission.Administrator;

        public async override void Execute(SocketSlashCommand command)
        {
            base.Execute(command);
            if(OwnerShard is null)
            {
                await command.RespondAsync("An internal error has occured: OwnerShard is null!", ephemeral: true);
                return;
            }
            SocketSlashCommandDataOption[] commandDataOptions = GetOptionsOrdered(command.Data.Options.ToList());
            if(command.Channel is not SocketTextChannel channel)
            {
                await command.RespondAsync(embed: new EmbedBuilder().GetErrorEmbed("Error", "Command must be ran in a text channel."), ephemeral: true);
                return;
            }
            bool useJson = (bool)commandDataOptions[0].Value;
            await command.RespondAsync("This command will take a long time to finish. Please wait.", ephemeral: true);
            var messages = channel.GetMessagesAsync(int.MaxValue).FlattenAsync().Result;
            string filepath = ConfigurationService.CacheFolder + $"backup_{channel.Name}_{DateTime.Now.ToString("dd-MM-yyyy")}-{DateTime.Now.ToString("hh\\:mm\\:ss")}.txt";
            StreamWriter sw = new StreamWriter(filepath, append: true);
            ulong counter = 0;
            List<MessageData> messagesStruct = new List<MessageData>();
            foreach (var message in messages)
            {
                if(!useJson)
                {
                    string loggedstring = "";
                    string date = message.Timestamp.ToString("dd-MM-yyyy");
                    string time = message.Timestamp.ToString("hh\\:mm\\:ss");
                    loggedstring += $"[{date}, {time}] [{message.Author.Username} ({message.Author.Id})]: ";
                    loggedstring += $"{message.Content}";
                    foreach (var attachment in message.Attachments)
                    {
                        loggedstring += $" {attachment.Url}";
                    }
                    loggedstring += "\n";
                    counter++;
                    sw.Write(loggedstring);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(message.Content))
                    {
                        continue;
                    }
                    MessageData data = new MessageData();
                    data.MessageID = message.Id;
                    data.IsURL = message.Content.Contains("https://") || message.Content.Contains("http://");
                    foreach(var attachment in message.Attachments)
                    {
                        if (data.Attachments == null)
                        {
                            data.Attachments = new string[] { };
                        }
                        data.Attachments.Append(attachment.Url);
                    }
                    data.Message = message.Content;
                    data.UserID = message.Author.Id;
                    data.Username = message.Author.Username;
                    data.Nickname = OwnerShard.TargetGuild.GetUser(data.UserID).Nickname;
                    data.TimeStamp = message.Timestamp.ToUnixTimeSeconds();
                    data.GuildID = OwnerShard.GuildID;
                }
            }
            if (messagesStruct.Any())
            {
                string data = JsonConvert.SerializeObject(messagesStruct, Formatting.None);
                sw.Write(data);
            }
            sw.Close();
            try
            {
                SocketDMChannel? DMChannel = command.User.CreateDMChannelAsync().Result as SocketDMChannel;
                if (DMChannel == null)
                {
                    throw new InvalidOperationException("DM channel is null!");
                }
                await DMChannel.SendFileAsync(filepath);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                await command.Channel.SendMessageAsync($"{command.User.Mention}, Command error: \n ```{ex}```");
            }
        }

        public override void BuildOptions()
        {
            base.BuildOptions();
            Options.Clear();
            CommandOptionsBase optionsBase = new CommandOptionsBase()
            {
                Name = "usejson",
                OptionType = ApplicationCommandOptionType.Boolean,
                Description = "Should the output be a json array?",
                Required = true,
            };
        }

        public struct MessageData
        {
            public string Nickname;
            public string Username;
            public ulong UserID;
            public long TimeStamp;
            public ulong MessageID;
            public string Message;
            public string[] Attachments;
            public bool IsURL;
            public ulong GuildID;
        }
    }
}
