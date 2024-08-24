using Discord.Rest;
using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Attributes;
using YetAnotherDiscordBot.ComponentSystem.ChannelUtilsComponent.Commands;
using YetAnotherDiscordBot.ComponentSystem.ModerationComponent;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot.ComponentSystem.ChannelUtilsComponent
{
    [BotComponent]
    public class ChannelUtilsComponent : Component
    {
        public override string Name => "ChannelUtilsComponent";

        public override string Description => "Provides Utilities for managing channels";

        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(ModerationComponent.ModerationComponent)
        };

        public override List<Type> ImportedCommands => new List<Type>()
        {
            typeof(SetChannelSlowdown),
            typeof(GetAllMessages),
            typeof(AddStickyMessage),
            typeof(RemoveStickyMessage),
        };

        public override Type ConfigurationClass => typeof(ChannelUtilsComponentConfiguration);

        public override ChannelUtilsComponentConfiguration Configuration
        {
            get
            {
                return (ChannelUtilsComponentConfiguration)base.Configuration;
            }
        }

        public override void OnValidated()
        {
            base.OnValidated();
            List<ChannelUtilsComponentConfiguration.StickyMessageData> invalidData = new List<ChannelUtilsComponentConfiguration.StickyMessageData>();
            foreach (ChannelUtilsComponentConfiguration.StickyMessageData data in Configuration.StickiedMessages)
            {
                if (_stickedMessageCache.ContainsKey(data.ChannelID))
                {
                    Log.Warning($"Duplicate Sticky Message data. Channel: {data.ChannelID}, the duplicate will be destroyed.");
                    invalidData.Add(data);
                    continue;
                }
                SocketGuildChannel channel = OwnerShard.TargetGuild.GetChannel(data.ChannelID);
                if (channel == null || channel.GetChannelType() != ChannelType.Text)
                {
                    Log.Warning($"Invalid sticky message channel (Not found or Invalid type). Channel: {data.ChannelID}, the duplicate will be destroyed.");
                    invalidData.Add(data);
                    continue;
                }
                _stickedMessageCache.TryAdd(data.ChannelID, data);
                _stickyChannelRefreshQueue.Enqueue(data.ChannelID);
            }
            if (invalidData.Count > 0)
            {
                Log.Warning($"A total of {invalidData.Count} sticky message instances have been removed due to being invalid.");
                Configuration.StickiedMessages.RemoveAll(x => invalidData.Contains(x));
                Configuration.Save();
            }
            OwnerShard.Client.MessageReceived += StickedMessageChecker;
            OwnerShard.Client.MessageDeleted += OnMessageDeleted;
            Thread thread = new Thread(DoHandleStickedMessageChannels);
            thread.Start();
        }

        private Task OnMessageDeleted(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel)
        {
            if (CacheHasStickyChannel(channel.Id))
            {
                InvalidateStickyMessageCache(channel.Id);
            }
            return Task.CompletedTask;
        }

        public override void OnShutdown()
        {
            _terminating = true;
            Log.Info("Waiting for Sticky Message data to save...");
            lock (_stickyMessageThreadLock)
            {
                SaveStickyMessageData();
            }
            Log.Info("Done, sticky message data saved, finishing shutdown.");
            base.OnShutdown();
        }

        private ConcurrentQueue<ulong> _stickyChannelRefreshQueue = new ConcurrentQueue<ulong>();

        //Multi-threading....
        private ConcurrentDictionary<ulong, ChannelUtilsComponentConfiguration.StickyMessageData> _stickedMessageCache = new ConcurrentDictionary<ulong, ChannelUtilsComponentConfiguration.StickyMessageData>();

        public bool CacheHasStickyChannel(ulong channelId)
        {
            return _stickedMessageCache.ContainsKey(channelId);
        }

        public bool AddStickyChannel(ulong channelId, string message, out string failureReason)
        {
            SocketGuildChannel channel = OwnerShard.TargetGuild.GetChannel(channelId);
            if (channel == null)
            {
                failureReason = "Channel not found.";
                return false;
            }
            if (channel.GetChannelType() != ChannelType.Text || channel is not SocketTextChannel textChannel)
            {
                failureReason = "Only text channels can have sticky messages.";
                return false;
            }
            if (_stickedMessageCache.ContainsKey(channelId))
            {
                failureReason = "Channel already has a sticky message.";
                return false;
            }
            RestUserMessage messageSent = textChannel.SendMessageAsync(embed: GetStickyMessageEmbed(message)).Result;
            ChannelUtilsComponentConfiguration.StickyMessageData stickyMsgData = new ChannelUtilsComponentConfiguration.StickyMessageData();
            stickyMsgData.ChannelID = channelId;
            stickyMsgData.Message = message;
            stickyMsgData.LastMessageID = messageSent.Id;
            if (_stickedMessageCache.ContainsKey(channelId))
            {
                UpdateStickyMessageCache(channelId, stickyMsgData);
            }
            else
            {
                _stickedMessageCache.TryAdd(channelId, stickyMsgData);
            }
            failureReason = string.Empty;
            return true;
        }

        public bool RemoveStickyChannel(ulong channelId, out string failureReason)
        {
            SocketGuildChannel channel = OwnerShard.TargetGuild.GetChannel(channelId);
            if (channel == null)
            {
                failureReason = "Channel not found.";
                return false;
            }
            if (channel.GetChannelType() != ChannelType.Text || channel is not SocketTextChannel textChannel)
            {
                failureReason = "Only text channels can have sticky messages.";
                return false;
            }
            if (!_stickedMessageCache.ContainsKey(channelId))
            {
                failureReason = "Channel doesn't have a sticky message.";
                return false;
            }
            ChannelUtilsComponentConfiguration.StickyMessageData data = _stickedMessageCache[channelId];
            IMessage msg = textChannel.GetMessageAsync(data.ChannelID).Result;
            if (msg == null)
            {
                failureReason = "Can't find the message via internal ID, this can be ignored becuase the Config has been removed anyway.";
                return false;
            }
            msg.DeleteAsync();
            _stickedMessageCache.TryRemove(textChannel.Id, out ChannelUtilsComponentConfiguration.StickyMessageData outData);
            SaveStickyMessageData();
            failureReason = string.Empty;
            return true;
        }

        public void UpdateStickyMessageCache(ulong id, ChannelUtilsComponentConfiguration.StickyMessageData data, bool invalidateCache = true)
        {
            if (!_stickedMessageCache.ContainsKey(id))
            {
                return;
            }
            _stickedMessageCache[id] = data;
            if (invalidateCache)
            {
                InvalidateStickyMessageCache(id);
            }
        }

        public void SaveStickyMessageData()
        {
            Configuration.StickiedMessages = _stickedMessageCache.Values.ToList();
            Configuration.Save();
        }

        public void InvalidateStickyMessageCache(ulong channelId)
        {
            if (!_stickedMessageCache.TryGetValue(channelId, out ChannelUtilsComponentConfiguration.StickyMessageData value))
            {
                return;
            }
            if (_stickyChannelRefreshQueue.Contains(value.ChannelID))
            {
                return;
            }
            _stickyChannelRefreshQueue.Enqueue(value.ChannelID);
        }

        private Task StickedMessageChecker(SocketMessage msg)
        {
            if (msg.Channel.GetChannelType() != ChannelType.Text)
            {
                return Task.CompletedTask;
            }
            if (msg.Author.Id == OwnerShard.Client.CurrentUser.Id)
            {
                return Task.CompletedTask;
            }
            if (!_stickedMessageCache.ContainsKey(msg.Channel.Id))
            {
                return Task.CompletedTask;
            }
            if (_stickyChannelRefreshQueue.Contains(msg.Channel.Id))
            {
                return Task.CompletedTask;
            }
            _stickyChannelRefreshQueue.Enqueue(msg.Channel.Id);
            return Task.CompletedTask;
        }

        private bool _terminating = false;

        private object _stickyMessageThreadLock = new object();

        private void DoHandleStickedMessageChannels(object? state)
        {
            lock (_stickyMessageThreadLock)
            {
                uint refreshTime = Configuration.RefreshStickyMessagesEveryXSeconds;
                Stopwatch stopwatch = Stopwatch.StartNew();
                while (true)
                {
                    if (stopwatch.Elapsed.TotalSeconds < refreshTime && !_terminating)
                    {
                        continue;
                    }
                    for (int i = 0; i < _stickyChannelRefreshQueue.Count; i++)
                    {
                        bool success = _stickyChannelRefreshQueue.TryDequeue(out ulong id);
                        if (!success)
                        {
                            continue;
                        }
                        bool hasData = _stickedMessageCache.TryGetValue(id, out var data);
                        SocketGuildChannel channel = OwnerShard.TargetGuild.GetChannel(data.ChannelID);
                        if (channel == null || channel is not SocketTextChannel textChannel)
                        {
                            continue;
                        }
                        IMessage message = textChannel.GetMessageAsync(data.LastMessageID).Result;
                        if (message == null || message.Author.Id != OwnerShard.Client.CurrentUser.Id)
                        {
                            continue;
                        }
                        message.DeleteAsync();
                        RestUserMessage newMessage = textChannel.SendMessageAsync(embed: GetStickyMessageEmbed(data.Message)).Result;
                        data.LastMessageID = newMessage.Id;
                        UpdateStickyMessageCache(textChannel.Id, data, false);
                    }
                    SaveStickyMessageData();
                    if (_terminating)
                    {
                        break;
                    }
                    stopwatch.Restart();
                    refreshTime = Configuration.RefreshStickyMessagesEveryXSeconds;
                }
                stopwatch.Stop();
                stopwatch.Reset();
            }
        }

        public Embed GetStickyMessageEmbed(string text)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithCurrentTimestamp();
            builder.WithTitle("Sticky Message");
            builder.WithColor(Color.Purple);
            builder.WithDescription(text);
            return builder.Build();
        }
    }
}
