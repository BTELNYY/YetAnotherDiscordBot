using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Service;
using Discord.WebSocket;
using System.Threading.Channels;

namespace YetAnotherDiscordBot.ComponentSystem.DeletedMessageLogger
{
    public class DeletedMessageLoggerComponent : Component
    {
        public override string Name => nameof(DeletedMessageLoggerComponent);

        public override string Description => "Component to intercept deleted messages and display them to a channel.";

        public DeletedMessageConfiguration Configuration { get; private set; } = new DeletedMessageConfiguration();

        private SocketTextChannel? _textChannel;

        public override void OnValidated()
        {
            base.OnValidated();
        }

        public override bool Start()
        {
            base.Start();
            Configuration = ConfigurationService.GetComponentConfiguration(new DeletedMessageConfiguration(), OwnerShard.GuildID, out bool success, true);
            if (!success)
            {
                Log.Warning($"Failed to get configuration for {nameof(DeletedMessageLoggerComponent)}");
                return success;
            }
            OwnerShard.Client.MessageDeleted += MessageDeleted;
            SocketTextChannel channel = OwnerShard.TargetGuild.GetTextChannel(Configuration.ChannelID);
            if(channel == null)
            {
                Log.Warning($"[{nameof(DeletedMessageLoggerComponent)}] Invalid channel ID: {Configuration.ChannelID}");
            }
            else
            {
                _textChannel = channel;
            }
            return true;
        }

        public override void OnShutdown()
        {
            base.OnShutdown();
            OwnerShard.Client.MessageDeleted -= MessageDeleted;
        }

        private Task MessageDeleted(Cacheable<IMessage, ulong> msg, Cacheable<IMessageChannel, ulong> channel)
        {
            if (msg.Value == null)
            {
                Log.Warning("Message is null.");
                return Task.CompletedTask;
            }
            if (msg.Value.Author.Id == OwnerShard.Client.CurrentUser.Id)
            {
                return Task.CompletedTask;
            }
            if(_textChannel == null)
            {
                Log.Error($"[{nameof(DeletedMessageLoggerComponent)}] Text channel is null.");
                return Task.CompletedTask;
            }
            EmbedBuilder eb = new();
            eb.WithTitle("Deleted Message");
            eb.AddField("Author", "<@" + msg.Value.Author.Id + ">");
            eb.AddField("Channel", "<#" + channel.Id + ">");
            if (msg.Value.Content != null || !string.IsNullOrEmpty(msg.Value.Content))
            {
                if (msg.Value.Content.Length > 0)
                {
                    eb.AddField("Content (text)", msg.Value.Content);
                }
            }
            if (msg.Value.Attachments.Count > 0)
            {
                string atturls = "";
                foreach (var att in msg.Value.Attachments)
                {
                    string attachmentparsed = att.Url.Replace("media", "cdn").Replace("net", "com");
                    Log.Debug(attachmentparsed);
                    Log.Debug(att.Url);
                    if (att == msg.Value.Attachments.Last())
                    {
                        atturls += attachmentparsed;
                    }
                    else
                    {
                        atturls += attachmentparsed + "\n";
                    }
                }
                eb.AddField("Attachments", atturls);
                Embed[] embeds = { eb.Build() };
                _textChannel.SendMessageAsync(embeds: embeds);
            }
            else
            {
                _textChannel.SendMessageAsync(embed: eb.Build());
            }
            return Task.CompletedTask;
        }
    }
}
