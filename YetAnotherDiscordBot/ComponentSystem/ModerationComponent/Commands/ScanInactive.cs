using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Attributes;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot.ComponentSystem.ModerationComponent.Commands
{
    public class ScanInactive : Command
    {
        public override string CommandName => "scaninactive";

        public override string Description => "This command will generate a text file to kick inactive members.";

        public override GuildPermission RequiredPermission => GuildPermission.Administrator;

        public override List<Type> RequiredComponents => new List<Type> { typeof(ModerationComponent) };

        public override bool UseLegacyExecute => false;

        List<ulong> kickIDs = new List<ulong>();

        static Dictionary<ulong, bool> isRunning = new Dictionary<ulong, bool>();

        [CommandMethod]
        public async void Execute(SocketSlashCommand command, long days)
        {
            if(OwnerShard == null)
            {
                throw new InvalidOperationException("OwnerShard is null!");
            }
            ModerationComponent component = OwnerShard.ComponentManager.GetComponent<ModerationComponent>(out bool success);
            if (!success)
            {
                throw new InvalidOperationException("Can't find ModerationComponent.");
            }
            if(isRunning.ContainsKey(OwnerShard.GuildID) && isRunning[OwnerShard.GuildID])
            {
                throw new InvalidOperationException("Purge already active in this server.");
            }
            SocketUser user = command.User;
            await command.RespondAsync("Purge started, good luck.", ephemeral: true);
            await foreach (var list in OwnerShard.TargetGuild.GetUsersAsync())
            {
                if(list != null)
                {
                    foreach(var item in list)
                    {
                        if(item.Hierarchy >= OwnerShard.TargetGuild.CurrentUser.Hierarchy)
                        {
                            continue;
                        }
                        kickIDs.Add(item.Id);
                    }
                }
            }
            List<SocketTextChannel> channels = OwnerShard.TargetGuild.TextChannels.ToList();
            string filepath = ConfigurationService.CacheFolder + $"purge_{DateTime.Now.ToString("dd-MM-yyyy")}-{DateTime.Now.ToString("hh\\-mm\\-ss")}.txt";
            SendDM(user, $"Total Guild Users: {kickIDs.Count}");
            foreach(var channel in channels)
            {
                SendDM(user, $"Begin channel scan: {channel.Name}");
                bool hasFinishedScan = false;
                ulong lastMessageId = 0;
                while (!hasFinishedScan)
                {
                    List<IMessage> messages = new List<IMessage>();
                    List<ulong> usersToRemove = new List<ulong>();
                    if(lastMessageId == 0)
                    {
                        messages = channel.GetMessagesAsync(250).FlattenAsync().Result.ToList();
                        Log.Debug("Messages loaded: " + messages.Count);
                        usersToRemove = messages.Select(x => x.Author.Id).ToList();
                        kickIDs.RemoveAll(x => usersToRemove.Contains(x));
                        if(!messages.Any())
                        {
                            Log.Warning("Empty message list.");
                            hasFinishedScan = true;
                            continue;
                        }
                        lastMessageId = messages.Last().Id;
                        double timeDiff = (messages.Last().CreatedAt.UtcDateTime - DateTimeOffset.Now.UtcDateTime).TotalDays;
                        if (timeDiff >= days)
                        {
                            Log.Debug("Finished Scan: Messages older than set date.");
                            hasFinishedScan = true;
                            continue;
                        }
                        continue;
                    }
                    messages = channel.GetMessagesAsync(lastMessageId, Direction.Before, 250).FlattenAsync().Result.ToList();
                    Log.Debug("Messages loaded: " + messages.Count);
                    usersToRemove = messages.Select(x => x.Author.Id).ToList();
                    kickIDs.RemoveAll(x => usersToRemove.Contains(x));
                    if (!messages.Any())
                    {
                        Log.Warning("Empty message list.");
                        hasFinishedScan = true;
                        continue;
                    }
                    lastMessageId = messages.Last().Id;
                    var lastMessage = messages.Last();
                    Log.Debug($"Last message created on: {lastMessage.CreatedAt}");
                    double diff = (messages.Last().CreatedAt.UtcDateTime - DateTimeOffset.Now.UtcDateTime).TotalDays;
                    if (diff >= days)
                    {
                        Log.Debug("Finished Scan: Messages older than set date.");
                        hasFinishedScan = true;
                        continue;
                    }
                }
                SendDM(user, $"Total Users to Kick: {kickIDs.Count}");
            }
            SendDM(user, $"Final Total to kick: {kickIDs.Count}");
            SendDM(user, $"You may access this file using: " + $"`purge_{DateTime.Now.ToString("dd - MM - yyyy")}-{DateTime.Now.ToString("hh\\-mm\\-ss")}.txt`");
            File.WriteAllText(filepath, string.Join("\n", kickIDs));
        }

        void SendDM(SocketUser user, string message)
        {
            try
            {
                user.SendMessageAsync(text: message);
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.ToString()}");
            }
        }
    }
}
