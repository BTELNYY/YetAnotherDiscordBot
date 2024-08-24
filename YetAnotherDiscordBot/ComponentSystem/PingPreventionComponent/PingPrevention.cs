using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Attributes;
using YetAnotherDiscordBot.ComponentSystem.ModerationComponent;
using YetAnotherDiscordBot.ComponentSystem.PingPreventionComponent.Commands;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot.ComponentSystem.PingPreventionComponent
{
    [BotComponent]
    public class PingPrevention : Component
    {
        public override string Name => "PingPreventionComponent";

        public override string Description => "Prevents users from pinging specific people.";

        public override List<Type> ImportedCommands => new List<Type>()
        {
            typeof(AddProtectedUser),
            typeof(RemoveProtectedUser),
            typeof(ListProtectedUsers)
        };

        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(ModerationComponent.ModerationComponent)
        };

        public override Type ConfigurationClass => typeof(PingPreventionConfiguration);

        public override PingPreventionConfiguration Configuration
        {
            get
            {
                return (PingPreventionConfiguration)base.Configuration;
            }
        }

        private ModerationComponent.ModerationComponent? _moderationComponent;

        public ModerationComponent.ModerationComponent ModerationComponent 
        { 
            get
            {
                if(_moderationComponent == null)
                {
                    _moderationComponent = OwnerShard.ComponentManager.GetComponent<ModerationComponent.ModerationComponent>(out bool success);
                    if (!success)
                    {
                        Log.Error("Can't get moderation component!");
                        throw new InvalidOperationException("Component cannot operate with moderation component.");
                    }
                }
                return _moderationComponent;
            }
        }

        public override void OnValidated()
        {
            base.OnValidated();
            OwnerShard.Client.MessageReceived += CheckMessage;
            _moderationComponent = OwnerShard.ComponentManager.GetComponent<ModerationComponent.ModerationComponent>(out bool success);
            if (!success)
            {
                Log.Error("Can't get moderation component!");
            }
        }

        private Task CheckMessage(SocketMessage msg)
        {
            if(msg.Content == string.Empty)
            {
                Log.Warning("Message is empty!");
                return Task.CompletedTask;
            }
            if(msg.Author == OwnerShard.Client.CurrentUser)
            {
                return Task.CompletedTask;
            }
            List<ulong> mentionedids = msg.MentionedUsers.ToList().Select(x => x.Id).ToList();
            if(!Configuration.PreventedIDs.Any(x => mentionedids.Contains(x)))
            {
                return Task.CompletedTask;
            }
            List<ulong> illegalMentionedIds = new List<ulong>();
            foreach(SocketUser user in msg.MentionedUsers)
            {
                if(Configuration.PreventedIDs.Contains(user.Id))
                {
                    illegalMentionedIds.Add(user.Id);
                }
            }
            if (msg.Reference != null)
            {
                SocketMessage? reference = msg.Reference.GetServerMessage();
                if (reference != null && illegalMentionedIds.Contains(reference.Author.Id))
                {
                    illegalMentionedIds.Remove(reference.Author.Id);
                }
            }
            if(illegalMentionedIds.Count > 0)
            {
                SocketGuildUser sender = OwnerShard.TargetGuild.GetUser(msg.Author.Id);
                SocketGuildUser me = OwnerShard.TargetGuild.GetUser(OwnerShard.Client.CurrentUser.Id);
                SocketGuildChannel? channel = msg.Channel as SocketGuildChannel;
                if (channel == null)
                {
                    Log.Error("Channel is null!");
                }
                if (sender == null || me == null)
                {
                    Log.Error("Woah some very strange shit is happening, the sender and me are null!");
                    return Task.CompletedTask;
                }
                ModerationComponent.PunishUser(sender, me, Configuration.Action, "Automatic punishment for pinging protected user.", TimeSpan.FromSeconds(Configuration.ActionDuration), channel, sendDm: true);
            }
            return Task.CompletedTask;
        }
    }
}
