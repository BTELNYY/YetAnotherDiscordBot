using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Commands.SLStuff;
using YetAnotherDiscordBot.Service;
using YetAnotherDiscordBot.Attributes;

namespace YetAnotherDiscordBot.ComponentSystem.WackerySLAPI
{
    [BotComponent]
    public class SLAPIComponent : Component
    {
        public const string DevURL = @"http://localhost:8000/";
        public const string ProdURL = @"http://backend:8000/";

        public override string Name => nameof(SLAPIComponent);
        public override string Description => "Intergrates Wackery's SCP:SL API.";
        public bool APIDisabled = false;
        public string CurrentURL { get; private set; } = "?";

        public Thread UpdateThread
        {
            get
            {
                if(_updateThread == null)
                {
                    _updateThread = new Thread(UpdateThreadMethod);
                }
                return _updateThread;
            }
        }

        private bool _threadShouldSuicide = false;

        private Thread? _updateThread;
        public override List<Type> ImportedCommands => new List<Type>()
        {
            typeof(GetPlayerDetails),
            typeof(Leaderboard),
        };

        private SLAPIComponentConfiguration? _configuration;

        public SLAPIComponentConfiguration Configuration
        {
            get
            {
                if(_configuration == null)
                {
                    _configuration = new SLAPIComponentConfiguration();
                    _configuration = ConfigurationService.GetComponentConfiguration<SLAPIComponentConfiguration>(_configuration, OwnerShard.GuildID, out bool success);
                    if(!success)
                    {
                        ConfigurationService.WriteComponentConfiguration<SLAPIComponentConfiguration>(_configuration, OwnerShard.GuildID);
                    }
                }
                return _configuration;
            }
            set
            {
                if(value == null)
                {
                    ConfigurationService.WriteComponentConfiguration<SLAPIComponentConfiguration>(new SLAPIComponentConfiguration(), OwnerShard.GuildID, true);
                }
                else
                {
                    ConfigurationService.WriteComponentConfiguration<SLAPIComponentConfiguration>(value, OwnerShard.GuildID, true);
                }
            }
        } 

        private void UpdateThreadMethod()
        {
            while (true)
            {
                if (_threadShouldSuicide)
                {
                    return;
                }
                UpdateEmbed();
                Thread.Sleep(Configuration.UpdateEverySeconds * 1000);
            }
        }

        private async void UpdateEmbed()
        {
            ulong guildid = OwnerShard.GuildID;
            ulong channelid = Configuration.ChannelID;
            NWAllResponse response = GetServerStatus(Configuration.AuthToken);
            if (response.value.Count() == 0)
            {
                Log.Error("Failed to fetch servers: server count is 0");
                return;
            }
            List<Embed> embeds = new List<Embed>();
            foreach (ServerResponse s in response.value)
            {
                Log.Debug("Creating embeds");
                foreach (Server s1 in s.Servers)
                {
                    string name = "";
                    if (Configuration.IDToNamesOfServers.ContainsKey(s1.ID.ToString()))
                    {
                        name = Configuration.IDToNamesOfServers[s1.ID.ToString()];
                    }
                    else
                    {
                        name = "[Missing Server Name]";
                        if (Configuration.DisplayRawIDs)
                        {
                            name = s1.ID.ToString();
                        }
                    }
                    var embed = new EmbedBuilder
                    {
                        Title = name
                    };
                    List<string> playerNames = s1.GetPlayerNames().ToList();
                    embed.WithDescription("Players currently online:\n```\n" + string.Join("\n", playerNames) + "```")
                        .WithCurrentTimestamp()
                        .WithColor(Color.Green)
                        .AddField("Players online", playerNames.Count);
                    Log.Debug("Embed created");
                    embeds.Add(embed.Build());
                }
            }
            var channel = OwnerShard.TargetGuild.GetChannel(Configuration.ChannelID) as ITextChannel;
            if (channel == null)
            {
                Log.Fatal("Channel not found! " + Configuration.ChannelID);
                return;
            }
            Log.Debug("Channel obtained");
            var meses = await channel.GetMessagesAsync().FlattenAsync();
            Log.Debug("Messages obtained");
            if (meses == null)
            {
                Log.Warning("No messages in channel");
                await channel.SendMessageAsync(embeds: embeds.ToArray());
                return;
            }
            Log.Debug("Searching for messages from bot");
            var botMes = meses.Where((message => message.Author.Id == OwnerShard.Client.CurrentUser.Id));
            Log.Debug("Getting first bot message");
            if (!botMes.Any())
            {
                Log.Warning("No bot messages!");
                await channel.SendMessageAsync(embeds: embeds.ToArray());
                return;
            }
            var messagetoEdit = botMes.First();
            Log.Debug("Checking dat shit");
            if (messagetoEdit == null)
            {
                // create new message
                Log.Debug("Create new message");
                await channel.SendMessageAsync(embeds: embeds.ToArray());
            }
            else
            {
                // edit message
                Log.Debug("Edit message");
                var mestoEdituser = messagetoEdit as IUserMessage;
                if (mestoEdituser == null)
                {
                    Log.Fatal("not a IUserMessage");
                    return;
                }
                await mestoEdituser.ModifyAsync(properties => { properties.Embeds = embeds.ToArray(); });
            }
        }

        public override void OnValidated()
        {
            base.OnValidated();
            if (Environment.GetEnvironmentVariable("RUNNING_IN_DOCKER") == null)
            {
                CurrentURL = DevURL;
            }
            else
            {
                CurrentURL = ProdURL;
            }
            if(Configuration.EndpointURL != "default")
            {
                CurrentURL = Configuration.EndpointURL;
            }
            Log.Info("Testing auth with token in config! Result: " + TestAuth(Configuration.AuthToken));
            _threadShouldSuicide = false;
            UpdateThread.Start();
        }

        public override void OnShutdown()
        {
            base.OnShutdown();
            _threadShouldSuicide = true;
        }

        public string TestAuth(string token)
        {
            try
            {
                string url = CurrentURL;
                string html = string.Empty;
                string requrl = "test/";
                Log.Debug("Trying to auth with Token: " + token);
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                var response = client.GetStringAsync(url + requrl);
                response.Wait();
                string result = response.Result;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return "Error";
            }
        }

        public PlayerStats GetPlayerStats(string? input)
        {
            if (input is null)
            {
                return new PlayerStats();
            }
            bool IsID = false;
            bool IsNorthwood = false;
            bool HasAtSymbol = false;
            HasAtSymbol = input.Contains('@');
            if (HasAtSymbol && ulong.TryParse(input.Split('@')[0], out ulong id))
            {
                IsID = true;
            }
            if (input.Length == 17 && ulong.TryParse(input, out id))
            {
                IsID = true;
            }
            if (HasAtSymbol && input.Split('@')[1] == "northwood")
            {
                IsNorthwood = true;
            }
            string url = CurrentURL;
            string html = string.Empty;
            string requrl = "";

            if (IsID)
            {
                requrl = "query/id/" + input;
            }
            if (!IsID || IsNorthwood)
            {
                requrl = "query/last_nick/" + input;
            }

            var client = new HttpClient();
            Log.Debug(url + requrl);
            try
            {
                var response = client.GetStringAsync(url + requrl);
                response.Wait();
                string result = response.Result;
                Log.Debug(result);
                return JsonConvert.DeserializeObject<PlayerStats>(result);
            }
            catch (Exception ex)
            {
                Log.Error("Error when fetching player details! \n" + ex.ToString());
                PlayerStats p = new PlayerStats();
                //this is a janky way to handle this, but it works
                p.PlayTime = -1;
                return p;
            }
        }

        public NWAllResponse GetServerStatus(string token)
        {
            try
            {
                string url = CurrentURL;
                string html = string.Empty;
                string requrl = "nw/all";

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                Log.Debug("GET: " + url + requrl);
                var response = client.GetStringAsync(url + requrl);
                response.Wait();
                string result = response.Result;
                Log.Debug(result);
                List<ServerResponse>? list = JsonConvert.DeserializeObject<List<ServerResponse>>(result);
                if (list == null)
                {
                    Log.Error("Failed to parse List of responses.");
                    return new NWAllResponse();
                }
                NWAllResponse resp = new NWAllResponse();
                resp.value = list.ToArray();
                return resp;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return new NWAllResponse();
            }
        }

        public PlayerStats[] GetPlaytimeLeaderboard()
        {
            try
            {
                string url = CurrentURL;
                string html = string.Empty;
                string requrl = "/query/leaderboard/";
                var client = new HttpClient();
                Log.Debug("GET: " + url + requrl);
                var response = client.GetStringAsync(url + requrl);
                response.Wait();
                string result = response.Result;
                Log.Debug(result);
                PlayerStats[]? list = JsonConvert.DeserializeObject<PlayerStats[]>(result);
                if (list == null)
                {
                    Log.Warning("List is null!");
                    return new PlayerStats[0];
                }
                else
                {
                    return list;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return new PlayerStats[0];
            }
        }
    }

    public struct PlayerStats
    {
        [JsonProperty("id")]
        public string SteamID;
        [JsonProperty("first_seen")]
        public DateTime FirstSeen;
        [JsonProperty("last_seen")]
        public DateTime LastSeen;
        [JsonProperty("play_time")]
        public long PlayTime;
        [JsonProperty("last_nickname")]
        public string LastNickname;
        [JsonProperty("nicknames")]
        public List<string> Usernames;
        [JsonProperty("flags")]
        public List<Flags> PFlags;
        [JsonProperty("time_online")]
        public long TimeOnline;
        [JsonProperty("login_amt")]
        public uint LoginAmount;

        public PlayerStats(Player p)
        {
            SteamID = p.ID;
            LastNickname = p.Nickname;
            FirstSeen = DateTime.UtcNow;
            LastSeen = DateTime.UtcNow;
            PlayTime = 0L;
            PFlags = new List<Flags>();
            Usernames = new List<string>();
            TimeOnline = 0L;
            LoginAmount = 0;
        }
        public void ResetOnlineTime()
        {
            TimeOnline = 0;
        }
    }


    public struct Flags
    {
        public PlayerFlags Flag;
        public string Issuer;
        public DateTime IssueTime;
        public string Comment;
    }

    public enum PlayerFlags
    {
        KOS,
        MASSKOS,
        RACISM,
        CHEATING,
        CAMPING,
        MICSPAM,
        TEAMING,
        SEXUALCOMMENTS,
        REPORTABUSE,
        BITCH,
        NONE,
        SEXISM,
        HOMOPHOBIA,
        TRANSPHOBIA,
        HATESPEECH
    }

    public struct NWAllResponse
    {
        public ServerResponse[] value;
    }

    public struct ServerResponse
    {
        public bool Success;
        public int Cooldown;
        public Server[] Servers;
    }

    public struct Server
    {
        public int ID;
        public int Port;
        public bool Online;
        public Player[] PlayersList;

        public string[] GetPlayerNames()
        {
            List<string> names = new List<string>();
            foreach (Player player in PlayersList)
            {
                names.Add(player.Nickname);
            }
            return names.ToArray();
        }
    }
    public struct Player
    {
        public string ID;
        public string Nickname;
        public override string? ToString()
        {
            if (ID == null || Nickname == null)
            {
                return null;
            }
            return "Nickname: " + Nickname + "; ID: " + ID;
        }
    }
}
