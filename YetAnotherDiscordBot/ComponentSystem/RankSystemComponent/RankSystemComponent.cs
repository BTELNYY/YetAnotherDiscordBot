using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private Dictionary<uint, List<RankRoleData>> _roleDataCache = new Dictionary<uint, List<RankRoleData>>();
        public override List<Type> ImportedCommands => new List<Type>()
        {
            typeof(Rank),
            typeof(SetRank),
            typeof(AddRankRole),
            typeof(RemoveRankRole),
            typeof(ListRankRoles),
        };

        public override Type ConfigurationClass => typeof(RankSystemComponentConfiguration);

        public override RankSystemComponentConfiguration Configuration
        {
            get
            {
                return (RankSystemComponentConfiguration)base.Configuration;
            }
        }

        public override void OnValidated()
        {
            base.OnValidated();
            CacheRankRoles();
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
                ValidateUser(GetUserData(msg.Author.Id));
            }
            return Task.CompletedTask;
        }


        private bool ValidateMessage(SocketGuildUser user, SocketMessage msg)
        {
            if (user == null || msg == null)
            {
                Log.Error("User or Message is null in message validator!");
                return false;
            }
            if (msg.Content.Length <= Configuration.MinimumTextLength)
            {
                return false;
            }
            if (!msg.Content.Contains(' ') && Configuration.MustHaveSpaces)
            {
                return false;
            }
            if (MessageCache.ContainsKey(user.Id))
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

        private void CacheRankRoles()
        {
            int cachedRankRoles = 0;
            foreach (RankRoleData data in Configuration.RankRoleConfiguration.RoleData)
            {
                if (_roleDataCache.ContainsKey(data.RequiredLevel))
                {
                    if (_roleDataCache[data.RequiredLevel].Contains(data))
                    {
                        Log.Warning($"Detected duplicate rank data! RoleID: {data.RoleId}, Required Level: {data.RequiredLevel}");
                        continue;
                    }
                    else
                    {
                        _roleDataCache[data.RequiredLevel].Add(data);
                        cachedRankRoles++;
                    }
                }
                else
                {
                    _roleDataCache.Add(data.RequiredLevel, new List<RankRoleData>() { data });
                    cachedRankRoles++;
                }
            }
            Log.Info($"Cached {cachedRankRoles} out of {Configuration.RankRoleConfiguration.RoleData.Count} rank roles.");
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
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<ulong> userIds = OwnerShard.TargetGuild.Users.Select(x => x.Id).ToList();
            Log.Info($"Validating all users! Total users to validate: {userIds.Count}");
            int totalIds = userIds.Count;
            int validated = 0;
            foreach (var id in userIds)
            {
                try
                {
                    DiscordUserRankData data = GetUserData(id);
                    ValidateUser(data);
                    validated++;
                }
                catch (Exception ex)
                {
                    Log.Error($"Validation failed! User: {id}, Error: \n {ex}");
                }
            }
            stopwatch.Stop();
            Log.Info($"Validation completed, {validated} out of {totalIds} succeeded. Took: {stopwatch.ElapsedMilliseconds}ms");
        }

        public bool AddRankRole(uint level, ulong roleId, RoleAction roleAction, out string failureReason)
        {
            SocketRole role = OwnerShard.TargetGuild.GetRole(roleId);
            if (role == null)
            {
                failureReason = $"Role with ID {roleId} not found.";
                return false;
            }
            if (_roleDataCache.ContainsKey(level) && _roleDataCache[level].Where(x => x.RoleId == roleId).Any())
            {
                failureReason = $"Role '{role.Name}' is already registered to level {level}.";
                return false;
            }
            RankRoleData data = new RankRoleData();
            data.RoleAction = roleAction;
            data.RoleId = roleId;
            data.RequiredLevel = level;
            if (Configuration.RankRoleConfiguration.RoleData.Contains(data))
            {
                failureReason = $"Role with ID {data.RoleId}, at level {data.RequiredLevel} with action {data.RoleAction} is already registered.";
                return false;
            }
            Configuration.RankRoleConfiguration.RoleData.Add(data);
            Configuration.Save();
            CacheRankRoles();
            failureReason = string.Empty;
            return true;
        }

        public bool RemoveRankRole(uint level, ulong roleId, RoleAction roleAction, out string failureReason)
        {
            SocketRole role = OwnerShard.TargetGuild.GetRole(roleId);
            if (role == null)
            {
                failureReason = $"Role with ID {roleId} not found.";
                return false;
            }
            if (_roleDataCache.ContainsKey(level) && !_roleDataCache[level].Where(x => x.RoleId == roleId).Any())
            {
                failureReason = $"Role '{role.Name}' is not registered to level {level}.";
                return false;
            }
            bool hasRemoved = false;
            RankRoleData target = new RankRoleData();
            foreach(RankRoleData data in Configuration.RankRoleConfiguration.RoleData)
            {
                if(data.RoleAction != roleAction || data.RequiredLevel != level || data.RoleId != roleId)
                {
                    continue;
                }
                hasRemoved = true;
                target = data;
            }
            if (!hasRemoved)
            {
                failureReason = $"Role '{role.Name}' wasn't found in the list of registrated rank roles.";
                return false;
            }
            Configuration.RankRoleConfiguration.RoleData.Remove(target);
            Configuration.Save();
            CacheRankRoles();
            failureReason = string.Empty;
            return true;
        }
    }
}