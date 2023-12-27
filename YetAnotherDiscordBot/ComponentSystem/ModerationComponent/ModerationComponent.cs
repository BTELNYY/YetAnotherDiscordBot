using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Transactions;
using YetAnotherDiscordBot.Service;
using YetAnotherDiscordBot.Attributes;
using System.Runtime.CompilerServices;
using System.ComponentModel.Design;
using YetAnotherDiscordBot.ComponentSystem.ModerationComponent.Commands;

namespace YetAnotherDiscordBot.ComponentSystem.ModerationComponent
{
    [BotComponent]
    public class ModerationComponent : Component
    {
        public override string Name => nameof(ModerationComponent);

        public override string Description => "Component for moderation.";

        public override List<Type> ImportedCommands => new List<Type>()
        {
            typeof(BanUser),
            typeof(KickUser),
            typeof(LockdownChannel),
            typeof(TimeoutUser),
            typeof(ServerLockdown),
        };

        public ModeratonComponentConfiguration Configuration { get; private set; } = new ModeratonComponentConfiguration();

        private SocketGuild? _targetGuild;
        private bool _usingAlternativeGuild = false;
        private static Dictionary<ulong, List<Overwrite>> _channelLocks = new Dictionary<ulong, List<Overwrite>>();
        public bool UsingAlternativeGuild
        {
            get
            {
                return _usingAlternativeGuild;
            }
        }

        private SocketTextChannel? _targetChannel;

        public override bool Start()
        {
            Configuration = ConfigurationService.GetComponentConfiguration(Configuration, OwnerShard.GuildID, out bool success, true);
            if (!success)
            {
                Log.Error("Failure to start ModerationComponent, fetching config failed.");
                return success;
            }
            if(Configuration.ServerID != 0)
            {
                SocketGuild guild = OwnerShard.Client.GetGuild(Configuration.ServerID);
                if(guild == null)
                {
                    Log.Warning("Although an alternative GuildID is defined, it can't be found.");
                    guild = OwnerShard.TargetGuild;
                }
                else
                {
                    _targetGuild = guild;
                    _usingAlternativeGuild = true;
                }
            }
            else
            {
                _targetGuild = OwnerShard.TargetGuild;
            }
            if(Configuration.ChannelID != 0) 
            {
                if (_targetGuild != null)
                {
                    SocketTextChannel textChannel = _targetGuild.GetTextChannel(Configuration.ChannelID);
                    if(textChannel == null)
                    {
                        Log.Error("Text channel is defined but can't be found in the guild.");
                        return false;
                    }
                    else
                    {
                        _targetChannel = textChannel;
                    }
                }
            }
            else
            {
                Log.Warning("ChannelID in moderation config is 0.");
            }
            return base.Start();
        }

        public void SendMessageToAudit(SocketGuildUser author, SocketGuildUser user, string action, string reason, bool sendDm = false)
        {
            EmbedBuilder eb = new();
            eb.WithTitle($"{user.DisplayName} was punished.");
            eb.WithCurrentTimestamp();
            eb.WithColor(Color.Orange);
            eb.AddField(Configuration.TranslationsData.AutherText, author.DisplayName);
            eb.AddField(Configuration.TranslationsData.ReasonText, reason);
            eb.AddField(Configuration.TranslationsData.ActionText, action);
            if(_targetChannel is null)
            {
                return;
            }
            _targetChannel.SendMessageAsync(embed: eb.Build());
            if (sendDm)
            {
                string dm = Configuration.TranslationsData.DMText.Replace("{serverName}", user.Guild.Name).Replace("{author}", author.DisplayName).Replace("{reason}", reason).Replace("{action}", action);
                try
                {
                    user.SendMessageAsync(dm);
                }
                catch (Exception ex)
                {
                    Log.Debug("Failed to dm user! Error: \n" + ex.ToString());
                }
            }
        }

        public void SendMessageToAudit(SocketGuildUser author, string action, string reason)
        {
            EmbedBuilder eb = new();
            eb.WithTitle($"Moderation action taken");
            eb.WithCurrentTimestamp();
            eb.WithColor(Color.Orange);
            eb.AddField(Configuration.TranslationsData.AutherText, author.DisplayName);
            eb.AddField(Configuration.TranslationsData.ReasonText, reason);
            eb.AddField(Configuration.TranslationsData.ActionText, action);
            if (_targetChannel is null)
            {
                return;
            }
            _targetChannel.SendMessageAsync(embed: eb.Build());
        }

        public bool PunishUser(SocketGuildUser user, SocketGuildUser author, Punishment punishment, string reason, TimeSpan? duration = null, SocketGuildChannel? channel = null, bool showMessage = true, bool sendDm = true)
        {
            if(user is null || author is null || reason is null)
            {
                Log.Warning("Null in punish user.");
                return false;
            }
            string punishmentText = GetTranslation(punishment);
            if (sendDm)
            {
                string dm = Configuration.TranslationsData.DMText.Replace("{serverName}", user.Guild.Name).Replace("{author}", author.DisplayName).Replace("{reason}", reason).Replace("{action}", punishmentText);
                try
                {
                    user.SendMessageAsync(dm);
                }
                catch (Exception ex)
                {
                    Log.Debug("Failed to dm user! Error: \n" + ex.ToString());
                }
            }
            EmbedBuilder eb = new();
            eb.WithTitle($"{user.Username} was punished.");
            eb.WithCurrentTimestamp();
            eb.WithColor(Color.Orange);
            eb.AddField(Configuration.TranslationsData.AutherText, $"<@{author.Id}> ({author.Username})");
            eb.AddField(Configuration.TranslationsData.TargetText, $"{user.Mention} ({user.Username})");
            eb.AddField(Configuration.TranslationsData.ReasonText, reason);
            eb.AddField(Configuration.TranslationsData.ActionText, GetTranslation(punishment));
            if (channel != null)
            {
                //Doesnt let me use channel.Mention... OOP try not to cuase brain damage
                eb.AddField(Configuration.TranslationsData.ChannelText, $"<#{channel.Id}> ({channel.Name})");
            }
            if(duration != null)
            {
                eb.AddField(Configuration.TranslationsData.UntilText, duration.Value.TotalSeconds + "s");
            }
            ITextChannel? textChannel = (ITextChannel?)channel;
            switch (punishment)
            {
                case Punishment.Kick:
                    user.KickAsync(reason);
                    if (textChannel != null && showMessage)
                    {
                        textChannel.SendMessageAsync(embed: eb.Build());
                    }
                    break;
                case Punishment.Ban:
                    user.BanAsync(0, reason);
                    if (textChannel != null && showMessage)
                    {
                        textChannel.SendMessageAsync(embed: eb.Build());
                    }
                    break;
                case Punishment.Timeout:
                    if(duration == null)
                    {
                        Log.Warning("Can't timeout with null duration.");
                        return false;
                    }
                    else
                    {
                        user.SetTimeOutAsync((TimeSpan)duration, new RequestOptions() { AuditLogReason = reason });
                        if(textChannel != null && showMessage)
                        {
                            textChannel.SendMessageAsync(embed: eb.Build());
                        }
                    }
                    break;
                case Punishment.Muzzle:
                    if (duration == null)
                    {
                        Log.Warning("Can't timeout with null duration.");
                        return false;
                    }
                    else
                    {
                        user.SetTimeOutAsync((TimeSpan)duration);
                        if (textChannel != null && showMessage)
                        {
                            textChannel.SendMessageAsync(embed: eb.Build());
                        }
                    }
                    break;
                case Punishment.ChannelMute:
                    if(textChannel == null)
                    {
                        Log.Error("TextChannel is null!");
                        return false;
                    }
                    var perms = new OverwritePermissions();
                    perms = perms.Modify(sendMessages: PermValue.Deny, sendMessagesInThreads: PermValue.Deny, createPrivateThreads: PermValue.Deny, createPublicThreads: PermValue.Deny, addReactions: PermValue.Deny);
                    var userperm = textChannel.GetPermissionOverwrite(user);
                    if (userperm is null)
                    {
                        textChannel.AddPermissionOverwriteAsync(user, perms);
                        if (showMessage)
                        {
                            textChannel.SendMessageAsync(embed: eb.Build());
                        }
                    }
                    else
                    {
                        textChannel.AddPermissionOverwriteAsync(user, OverwritePermissions.InheritAll);
                        textChannel.RemovePermissionOverwriteAsync(user);
                        if (showMessage)
                        {
                            textChannel.SendMessageAsync(embed: eb.Build());
                        }
                    }
                    break;
                default:
                    Log.Warning("Punishment wans't handled, probably needs to be handled by the command sending it.");
                    break;
            }
            if(_targetChannel != null && _targetChannel is ITextChannel staffChannel)
            {
                staffChannel.SendMessageAsync(embed: eb.Build());
            }
            return true;
        }

        public async void LockdownChannel(SocketTextChannel textchannel, SocketGuildUser author, bool useWarning = false, bool checkEveryonePerms = false, bool showLockdownMessage = true)
        {
            //This should almost never happen.
            //seethe
            if(textchannel.Guild.Id != OwnerShard.GuildID)
            {
                Log.Error("Tried to lockdown a channel across another guild.");
                return;
            }
            if(textchannel is SocketThreadChannel channel)
            {
                Log.Info("Can't run that on threads.");
                return;
            }
            OverwritePermissions? overwrites = textchannel.GetPermissionOverwrite(OwnerShard.TargetGuild.EveryoneRole);
            if (overwrites != null)
            {
                if ((overwrites.Value.ViewChannel == PermValue.Deny && overwrites.Value.SendMessages == PermValue.Deny) && checkEveryonePerms)
                {
                    Log.Debug("This channel can't be locked, @everyone can't type anyway.");
                    return;
                }
            }
            if (_channelLocks.ContainsKey(textchannel.Id))
            {
                foreach (var thing in _channelLocks[textchannel.Id])
                {
                    if (thing.TargetType == PermissionTarget.Role)
                    {
                        var r = OwnerShard.TargetGuild.GetRole(thing.TargetId);
                        await textchannel.AddPermissionOverwriteAsync(r, thing.Permissions);
                    }
                }
                _channelLocks.Remove(textchannel.Id);
                if (showLockdownMessage)
                {
                    await textchannel.SendMessageAsync(Configuration.TranslationsData.LockdownEnded);
                }
            }
            else
            {
                if (useWarning)
                {
                    string message = Configuration.TranslationsData.LockdownWarning.Replace("{channelId}", textchannel.Id.ToString()).Replace("{length}", Configuration.LockdownDelay.ToString()).Replace("{url}", Configuration.LockdownWarningVideoURL);
                    await textchannel.SendMessageAsync(message);
                    await Task.Run(() => DelayedLockdown(textchannel));
                }
                else
                {
                    var perms = new OverwritePermissions();
                    perms = perms.Modify(sendMessages: PermValue.Deny, sendMessagesInThreads: PermValue.Deny, createPrivateThreads: PermValue.Deny, createPublicThreads: PermValue.Deny, addReactions: PermValue.Deny);
                    var role = OwnerShard.TargetGuild.EveryoneRole;
                    _channelLocks.Add(textchannel.Id, textchannel.PermissionOverwrites.ToList());
                    foreach (var thing in textchannel.PermissionOverwrites)
                    {
                        if (thing.TargetType == PermissionTarget.Role)
                        {
                            var r = OwnerShard.TargetGuild.GetRole(thing.TargetId);
                            await textchannel.RemovePermissionOverwriteAsync(r);
                        }
                    }
                    await textchannel.AddPermissionOverwriteAsync(role, perms);
                    if (showLockdownMessage)
                    {
                        await textchannel.SendMessageAsync(Configuration.TranslationsData.LockdownStarted);
                    }
                }
            }
            EmbedBuilder eb = new();
            eb.WithTitle("Channel Lockdown");
            eb.WithColor(Color.Orange);
            eb.WithCurrentTimestamp();
            eb.AddField(Configuration.TranslationsData.AutherText, $"{author.Mention} ({author.Username})");
            eb.AddField(Configuration.TranslationsData.ChannelText, $"{textchannel.Mention} ({textchannel.Name})");
            bool channelState = _channelLocks.ContainsKey(textchannel.Id);
            //Race conditions my beloved....
            if (useWarning)
            {
                //Delay will ALWAYS lock the channel regardless of current state.
                channelState = true;
            }
            eb.AddField(Configuration.TranslationsData.ActionText, "Lockdown");
            eb.AddField(Configuration.TranslationsData.LockdownState, channelState.ToString());
            if(_targetChannel is null)
            {
                Log.Error("Target channel is null!");
                return;
            }
            await _targetChannel.SendMessageAsync(embed: eb.Build());
        }

        async Task DelayedLockdown(SocketTextChannel textchannel)
        {
            if (_channelLocks.ContainsKey(textchannel.Id))
            {
                return;
            }
            await Task.Delay(Configuration.LockdownDelay * 1000);
            var perms = new OverwritePermissions();
            perms = perms.Modify(sendMessages: PermValue.Deny, sendMessagesInThreads: PermValue.Deny, createPrivateThreads: PermValue.Deny, createPublicThreads: PermValue.Deny, addReactions: PermValue.Deny);
            var role = OwnerShard.TargetGuild.EveryoneRole;
            _channelLocks.Add(textchannel.Id, textchannel.PermissionOverwrites.ToList());
            foreach (var thing in textchannel.PermissionOverwrites)
            {
                if (thing.TargetType == PermissionTarget.Role)
                {
                    var r = OwnerShard.TargetGuild.GetRole(thing.TargetId);
                    await textchannel.RemovePermissionOverwriteAsync(r);
                }
            }
            await textchannel.AddPermissionOverwriteAsync(role, perms);
            await textchannel.SendMessageAsync(Configuration.TranslationsData.LockdownStarted);
        }

        public static string GetTranslation(Punishment p)
        {
            return p.ToString();
        }

        public enum Punishment
        {
            Kick,
            Ban,
            Timeout,
            ChannelMute,
            TempBan,
            Muzzle,
        }
    }
}
