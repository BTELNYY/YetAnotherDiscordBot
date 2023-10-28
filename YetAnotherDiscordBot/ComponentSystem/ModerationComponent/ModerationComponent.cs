using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot.ComponentSystem.ModerationComponent
{
    public class ModerationComponent : Component
    {
        public override string Name => nameof(ModerationComponent);

        public override string Description => "Component for moderation.";

        public ModeratonComponentConfiguration Configuration { get; private set; } = new ModeratonComponentConfiguration();

        private SocketGuild? _targetGuild;
        private bool _usingGuild = false;

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
                    _usingGuild = true;
                    _targetGuild = guild;
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

        public bool PunishUser(SocketGuildUser user, SocketGuildUser author, Punishment punishment, string reason, TimeSpan? duration = null, SocketGuildChannel? channel = null, bool showMessage = true)
        {
            if(_targetChannel is null)
            {
                return false;
            }
            string punishmentText = GetTranslation(punishment);
            string dm = Configuration.TranslationsData.DMText.Replace("{serverName}", user.Guild.Name).Replace("{author}", author.DisplayName).Replace("{reason}", reason).Replace("{action}", punishmentText);
            try
            {
                user.SendMessageAsync(dm);
            }
            catch(Exception ex)
            {
                Log.Debug("Failed to dm user! Error: \n" + ex.ToString());    
            }
            EmbedBuilder eb = new();
            eb.WithTitle($"{user.DisplayName} was punished.");
            eb.WithCurrentTimestamp();
            eb.WithColor(Color.Orange);
            eb.AddField(Configuration.TranslationsData.AutherText, author.DisplayName);
            eb.AddField(Configuration.TranslationsData.ReasonText, reason);
            eb.AddField(Configuration.TranslationsData.ActionText, GetTranslation(punishment));
            if (channel != null)
            {
                eb.AddField(Configuration.TranslationsData.ChannelText, $"<#{channel.Id}>");
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
                        break;
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
                        break;
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
                        break;
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
