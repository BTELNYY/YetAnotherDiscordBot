using Discord;
using Discord.WebSocket;
using YetAnotherDiscordBot.Service;
using YetAnotherDiscordBot.Attributes;

namespace YetAnotherDiscordBot.ComponentSystem.DeletedMessageLogger
{
    [BotComponent]
    public class DeletedMessageLoggerComponent : Component
    {
        public override string Name => nameof(DeletedMessageLoggerComponent);

        public override string Description => "Component to intercept deleted messages and display them to a channel.";

        public DeletedMessageConfiguration Configuration { get; private set; } = new DeletedMessageConfiguration();

        private SocketTextChannel? _textChannel;

        private SocketGuild? _targetGuild;

        public SocketGuild TargetGuild
        {
            get
            {
                if(_targetGuild == null)
                {
                    return OwnerShard.TargetGuild;
                }
                else
                {
                    return _targetGuild;
                }
            }
        }

        private bool _isUsingAlternateGuild = false;

        public bool IsUsingAlternativeGuild
        {
            get
            {
                return _isUsingAlternateGuild;
            }
        }

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
            if(Configuration.GuildID != 0)
            {
                SocketGuild? guild = OwnerShard.Client.GetGuild(Configuration.GuildID);
                if(guild == null)
                {
                    Log.Warning("Alternative Guild is defined but cannot be found!");
                    _targetGuild = OwnerShard.TargetGuild;
                }
                else
                {
                    _targetGuild = guild;
                    _isUsingAlternateGuild = true;
                }
            }
            else
            {
                _targetGuild = OwnerShard.TargetGuild;
            }
            SocketTextChannel channel = _targetGuild.GetTextChannel(Configuration.ChannelID);
            if(channel == null)
            {
                Log.Warning($"[{nameof(DeletedMessageLoggerComponent)}] Invalid channel ID: {Configuration.ChannelID}");
            }
            else
            {
                _textChannel = channel;
            }
            OwnerShard.Client.MessageDeleted += MessageDeleted;
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
            SocketGuildChannel? socketGuildChannel = channel.Value as SocketGuildChannel;
            if(socketGuildChannel == null)
            {
                return Task.CompletedTask;
            }
            if(socketGuildChannel.Guild.Id != OwnerShard.GuildID)
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
            eb.WithAuthor(msg.Value.Author);
            if (msg.Value.Content != null || !string.IsNullOrEmpty(msg.Value.Content))
            {
                if (msg.Value.Content.Length > 0)
                {
                    eb.AddField("Content (text)", msg.Value.Content);
                }
            }
            if (msg.Value.Attachments.Count > 0)
            {
                List<Embed> embeds = new List<Embed>();
                List<Embed> msgEmbeds = new List<Embed>();
                string atturls = "";
                foreach (var att in msg.Value.Attachments)
                {
                    string attachmentparsed = att.Url.Replace("media", "cdn").Replace("net", "com");
                    if (att == msg.Value.Attachments.Last())
                    {
                        atturls += attachmentparsed;
                    }
                    else
                    {
                        atturls += attachmentparsed + "\n";
                    }
                    EmbedBuilder builder = new EmbedBuilder();
                    builder.WithUrl(attachmentparsed);
                    builder.WithImageUrl(attachmentparsed);
                    builder.AddField("URL", attachmentparsed);
                    msgEmbeds.Add(builder.Build());
                }
                eb.AddField("Attachments", atturls);
                embeds.Add(eb.Build());
                embeds = embeds.Concat(msgEmbeds).ToList();
                _textChannel.SendMessageAsync(embeds: embeds.ToArray());
            }
            else
            {
                _textChannel.SendMessageAsync(embed: eb.Build());
            }
            return Task.CompletedTask;
        }
    }
}
