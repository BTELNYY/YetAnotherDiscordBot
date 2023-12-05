﻿using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Transactions;
using YetAnotherDiscordBot.Commands;
using YetAnotherDiscordBot.Commands.Moderation;
using YetAnotherDiscordBot.Service;
using YetAnotherDiscordBot.Attributes;

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
            typeof(TimeoutUser)
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

        public bool PunishUser(SocketGuildUser user, SocketGuildUser author, Punishment punishment, string reason, TimeSpan? duration = null, SocketGuildChannel? channel = null, bool showMessage = true, bool sendDm = true)
        {
            if(_targetChannel is null || user is null || author is null || reason is null)
            {
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
                        user.SetTimeOutAsync((TimeSpan)duration);
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

        public void LockdownChannel(SocketTextChannel textchannel, SocketGuildUser author, bool useWarning = false)
        {
            //This should almost never happen.
            if(textchannel.Guild.Id != OwnerShard.GuildID)
            {
                Log.Error("Tried to lockdown a channel across another guild.");
                return;
            }
            if (useWarning && !_channelLocks.ContainsKey(textchannel.Id))
            {
                string message = Configuration.TranslationsData.LockdownWarning.Replace("{channelId}", textchannel.Id.ToString()).Replace("{length}", Configuration.LockdownDelay.ToString()).Replace("{url}", Configuration.LockdownWarningVideoURL);
                textchannel.SendMessageAsync(message);
                Task.Run(() => DelayedLockdown(textchannel));
            }
            if (!useWarning && !_channelLocks.ContainsKey(textchannel.Id))
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
                        textchannel.RemovePermissionOverwriteAsync(r);
                    }
                }
                textchannel.AddPermissionOverwriteAsync(role, perms);
                textchannel.SendMessageAsync(Configuration.TranslationsData.LockdownStarted);
            }
            if (_channelLocks.ContainsKey(textchannel.Id))
            {
                textchannel.SendMessageAsync(Configuration.TranslationsData.LockdownEnded);
                foreach (var thing in _channelLocks[textchannel.Id])
                {
                    if (thing.TargetType == PermissionTarget.Role)
                    {
                        var r = OwnerShard.TargetGuild.GetRole(thing.TargetId);
                        textchannel.AddPermissionOverwriteAsync(r, thing.Permissions);
                    }
                }
                _channelLocks.Remove(textchannel.Id);
                textchannel.SendMessageAsync(Configuration.TranslationsData.LockdownEnded);
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
            _targetChannel.SendMessageAsync(embed: eb.Build());
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
