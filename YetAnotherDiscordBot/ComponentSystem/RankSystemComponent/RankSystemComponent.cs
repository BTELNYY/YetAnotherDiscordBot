using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Attributes;
using YetAnotherDiscordBot.ComponentSystem.RankSystemComponent.Commands;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot.ComponentSystem.RankSystemComponent
{
    [BotComponent]
    public class RankSystemComponent : Component
    {
        private Dictionary<ulong, string> MessageCache = new Dictionary<ulong, string>();

        private Dictionary<uint, List<RankRoleData>> RoleDataCache = new Dictionary<uint, List<RankRoleData>>();

        private RankSystemComponentConfiguration? _configuration;

        public override List<Type> ImportedCommands => new List<Type>()
        {
            typeof(Rank),
            typeof(SetRank),
        };

        public RankSystemComponentConfiguration Configuration
        {
            get
            {
                if(_configuration == null)
                {
                    Log.Warning("Getter in Rank Component caught null. Trying to read from disk!");
                    _configuration = ConfigurationService.GetComponentConfiguration(new RankSystemComponentConfiguration(), OwnerShard.GuildID, out bool success, doRewrite: true);
                    if (!success)
                    {
                        Log.Error("Failure to read component from disk!");
                        _configuration = new RankSystemComponentConfiguration();
                        ConfigurationService.WriteComponentConfiguration(new RankSystemComponentConfiguration(), OwnerShard.GuildID, true);
                    }
                }
                return _configuration;
            }
        }

        public override void OnValidated()
        {
            base.OnValidated();
            _configuration = ConfigurationService.GetComponentConfiguration(new RankSystemComponentConfiguration(), OwnerShard.GuildID, out bool success, doRewrite: true);
            if (!success)
            {
                Log.Warning("Failure to get Component Config for rank component, restoring to default.");
                _configuration = new RankSystemComponentConfiguration();
                ConfigurationService.WriteComponentConfiguration(new RankSystemComponentConfiguration(), OwnerShard.GuildID, true);
            }
            int cachedRankRoles = 0;
            foreach(RankRoleData data in _configuration.RankRoleConfiguration.RoleData) 
            {
                if (RoleDataCache.ContainsKey(data.RequiredLevel))
                {
                    if (RoleDataCache[data.RequiredLevel].Contains(data))
                    {
                        Log.Warning($"Detected duplicate rank data! RoleID: {data.RoleId}, Required Level: {data.RequiredLevel}");
                        continue;
                    }
                    else
                    {
                        RoleDataCache[data.RequiredLevel].Add(data);
                        cachedRankRoles++;
                    }
                }
                else
                {
                    RoleDataCache.Add(data.RequiredLevel, new List<RankRoleData>() { data });
                    cachedRankRoles++;
                }
            }
            Log.Info($"Cached {cachedRankRoles} out of {_configuration.RankRoleConfiguration.RoleData.Count} rank roles.");
            OwnerShard.Client.MessageReceived += MessageRecieved;
            OwnerShard.Client.UserBanned += UserBanned;
            OwnerShard.Client.UserLeft += UserKicked;
        }

        private Task UserKicked(SocketGuild guild, SocketUser user)
        {
            List<RestAuditLogEntry> audits = guild.GetAuditLogsAsync(5).FlattenAsync().Result.ToList();
            if(audits.Any(x => x.Action == ActionType.Kick && x.Data is KickAuditLogData data && data.Target.Id == user.Id))
            {
                if (Configuration.DeleteDataOnKick)
                {
                    OwnerShard.DiscordUserDataService.DeleteData<DiscordUserRankData>(user.Id);
                }
            }
            return Task.CompletedTask;
        }

        private Task UserBanned(SocketUser user, SocketGuild guild)
        {
            if (!Configuration.DeleteDataOnBan)
            {
                return Task.CompletedTask;
            }
            OwnerShard.DiscordUserDataService.DeleteData<DiscordUserRankData>(user.Id);
            return Task.CompletedTask;
        }

        private Task MessageRecieved(SocketMessage msg)
        {
            if(msg.Channel is not SocketGuildChannel channel)
            {
                return Task.CompletedTask;
            }
            if(channel.Guild.Id != OwnerShard.GuildID)
            {
                return Task.CompletedTask;
            }
            if(msg.Author.IsBot)
            {
                return Task.CompletedTask;
            }
            if(!ValidateMessage((SocketGuildUser)msg.Author, msg))
            {
                return Task.CompletedTask;
            }
            DiscordUserRankData discordUserRankData = OwnerShard.DiscordUserDataService.GetData<DiscordUserRankData>(msg.Author.Id);
            if (discordUserRankData.XPLocked)
            {
                return Task.CompletedTask;
            }
            float rolled = Configuration.RankXPRandomizerConfiguration.Randomize();
            rolled *= Configuration.CurrentXPMultiplier;
            rolled *= discordUserRankData.PersonalXPMultiplier;
            float requiredXP = Configuration.RankAlgorithmConfiguratiom.FindXP(discordUserRankData.Level);
            if((rolled + discordUserRankData.XP) < requiredXP)
            {
                discordUserRankData.XP += rolled;
                OwnerShard.DiscordUserDataService.WriteData(discordUserRankData, msg.Author.Id, true);
                return Task.CompletedTask;
            }
            else
            {
                discordUserRankData.XP += rolled;
                while(discordUserRankData.XP > Configuration.RankAlgorithmConfiguratiom.FindXP(discordUserRankData.Level))
                {
                    float xp = Configuration.RankAlgorithmConfiguratiom.FindXP(discordUserRankData.Level);
                    discordUserRankData.XP -= xp;
                    discordUserRankData.Level++;
                }
                OwnerShard.DiscordUserDataService.WriteData(discordUserRankData, msg.Author.Id);
            }
            return Task.CompletedTask;
        }

        public DiscordUserRankData GetUserData(ulong id)
        {
            DiscordUserRankData discordUserRankData = OwnerShard.DiscordUserDataService.GetData<DiscordUserRankData>(id);
            return discordUserRankData;
        }

        public void SetUserData(ulong id, DiscordUserRankData data)
        {
            OwnerShard.DiscordUserDataService.WriteData<DiscordUserRankData>(data, id, true);
        }

        private bool ValidateMessage(SocketGuildUser user, SocketMessage msg)
        {
            if(user == null || msg == null)
            {
                Log.Error("User or Message is null in message validator!");
                return false;
            }
            if(msg.Content.Length <= Configuration.MinimumTextLength)
            {
                return false;
            }
            if(!msg.Content.Contains(' ') && Configuration.MustHaveSpaces)
            {
                return false;
            }
            if(MessageCache.ContainsKey(user.Id))
            {
                if (MessageCache[user.Id] == msg.Content)
                {
                    if (!Configuration.AllowDuplicateMessages)
                    {
                        return false;
                    }
                }
                else
                {
                    MessageCache[user.Id] = msg.Content;
                }
            }
            return true;
        }


        public void ValidateUser(DiscordUserRankData discordUserRankData)
        {
            SocketGuild guild = discordUserRankData.DiscordUserDataService.BotShard.TargetGuild;
            SocketGuildUser user = guild.GetUser(discordUserRankData.OwnerID);
            foreach (RankRoleData data in Configuration.RankRoleConfiguration.RoleData)
            {
                RoleAction action = data.RoleAction;
                SocketRole role = guild.GetRole(data.RoleId);
                if (discordUserRankData.Level >= data.RequiredLevel)
                {
                    //Proper level for role.
                }
                else
                {
                    if (action == RoleAction.Add)
                    {
                        action = RoleAction.Remove;
                    }
                    else
                    {
                        action = RoleAction.Add;
                    }
                }
                switch (action)
                {
                    case RoleAction.Add:
                        if (user.Roles.Where(x => x.Id == role.Id).Any())
                        {
                            continue;
                        }
                        user.AddRoleAsync(role);
                        break;
                    case RoleAction.Remove:
                        if (!user.Roles.Where(x => x.Id == role.Id).Any())
                        {
                            continue;
                        }
                        user.RemoveRoleAsync(role);
                        break;
                    default:
                        break;
                }
            }
        }

        public void ValidateUsers()
        {
            List<ulong> userIds = OwnerShard.TargetGuild.Users.Select(x => x.Id).ToList();
            Log.Info($"Validating all users! Total users to validate: {userIds.Count}");
            foreach (var id in userIds)
            {
                DiscordUserRankData data = GetUserData(id);
                ValidateUser(data);
            }
        }
    }
}
