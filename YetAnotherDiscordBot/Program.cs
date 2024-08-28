using Discord;
using Discord.WebSocket;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using YetAnotherDiscordBot.Configuration;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot
{
    public class Program
    {
        public static DiscordSocketClient Client = new();

        public static Dictionary<ulong, BotShard> GuildToThread = new Dictionary<ulong, BotShard>();

        public static Dictionary<BotShard, Thread> BotToThread = new Dictionary<BotShard, Thread>();

        public static List<Assembly> LoadedAssemblies = new List<Assembly>();

        public static GlobalConfiguration GlobalConfiguration
        {
            get
            {
                return ConfigurationService.GlobalConfiguration;
            }
        }

        public static BotShard GetShard(ulong id)
        {
            if (GuildToThread.ContainsKey(id))
            {
                return GuildToThread[id];
            }
            else
            {
                return StartShard(id);
            }
        }

        public static void RemoveShard(ulong id)
        {
            if(GuildToThread.ContainsKey(id))
            {
                BotShard shard = GuildToThread[id];
                BotToThread.Remove(shard);
                GuildToThread.Remove(id);
            }
        }

        public static bool HasShard(ulong id)
        {
            return GuildToThread.ContainsKey(id);
        }

        private Stopwatch stopwatch = new Stopwatch();

        public static async Task Main(string[] args)
        {
            await new Program().MainAsync(args);
        }

        public async Task MainAsync(string[] args)
        {
            Directory.CreateDirectory(ConfigurationService.GlobalConfiguration.LogPath);
            Log.GlobalInfo("Starting up!");
            stopwatch = Stopwatch.StartNew();
            DiscordSocketConfig config = new()
            {
                GatewayIntents = GatewayIntents.All,
                MessageCacheSize = 50,          
            };
            string token = "";
            token = ConfigurationService.GlobalConfiguration.Token;
            Log.GlobalInfo("Starting Services...");
            ConfigurationService.Start();
            ShutdownService.Start();
            CommandService.Start();
            ShutdownService.OnShutdownSignal += OnShutdown;
            Log.GlobalInfo("Connecting to discord...");
            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();
            await Client.SetStatusAsync(ConfigurationService.GlobalConfiguration.BotStatus);
            await Client.SetCustomStatusAsync(ConfigurationService.GlobalConfiguration.BotStatusMessage);
            Log.GlobalInfo("Register Events...");
            Client.Ready += OnReady;
            Client.Log += LogEvent;
            Client.ApplicationCommandCreated += ApplicationCommandCreated;
            Client.GuildUnavailable += GuildRemoved;
            Client.SlashCommandExecuted += CommandService.SlashCommandExecuted;
            //AppDomain.CurrentDomain.FirstChanceException += OnExceptionOccured;
            await Task.Delay(-1);
        }

        private void OnExceptionOccured(object? sender, FirstChanceExceptionEventArgs e)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithTitle("An Exception occured!");
            eb.WithColor(Color.Red);
            string senderData = "null";
            if (sender != null)
            {
                senderData = sender.GetType().Name;
            }
            eb.AddField("Object Type", $"```{senderData}```");
            eb.AddField("Exception", $"```{e.Exception.ToString()}```");
            eb.WithTimestamp(DateTime.Now);
            foreach (ulong devid in GlobalConfiguration.DeveloperIDs)
            {
                SocketUser dev = Client.GetUser(devid);
                try
                {
                    dev.SendMessageAsync(embed: eb.Build());
                }
                catch(Exception ex)
                {
                    Log.GlobalError("Failed to send exception to developer! Reason: \n" + ex.ToString());
                }
            }
        }

        private Task GuildRemoved(SocketGuild guild)
        {
            if (HasShard(guild.Id))
            {
                Log.GlobalWarning("Runtime removed from guild. Stopping shard. ID: " + guild.Id);
                GetShard(guild.Id).OnShutdown();
                RemoveShard(guild.Id);
            }
            return Task.CompletedTask;
        }

        private Task GuildJoined(SocketGuild guild)
        {
            Log.GlobalInfo("Added to new Guild at runtime. Starting new shard!");
            StartShard(guild.Id);
            return Task.CompletedTask;
        }

        private Task ApplicationCommandCreated(SocketApplicationCommand arg)
        {
            Log.GlobalDebug("Application command created!");
            return Task.CompletedTask;
        }

        private static Task LogEvent(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Error:
                    Log.GlobalError(msg.Message + "\n" + msg.Exception);
                    break;
                case LogSeverity.Info:
                    Log.GlobalInfo(msg.Message + "\n" + msg.Exception);
                    break;
                case LogSeverity.Warning:
                    Log.GlobalWarning(msg.Message + "\n" + msg.Exception);
                    break;
                case LogSeverity.Verbose:
                    Log.GlobalVerbose(msg.Message + "\n" + msg.Exception);
                    break;
                case LogSeverity.Critical:
                    Log.GlobalCritical(msg.Message + "\n" + msg.Exception);
                    break;
                case LogSeverity.Debug:
                    Log.GlobalDebug(msg.Message + "\n" + msg.Exception);
                    break;
                default:
                    Log.GlobalWarning("The bellow message failed to be caught by any switch, default warning used.");
                    Log.GlobalWarning(msg.Message + "\n" + msg.Exception);
                    break;
            }
            return Task.CompletedTask;
        }

        public static void OnShutdown()
        {
            Log.GlobalInfo("Shutdown Shards...");
            foreach (BotShard shard in BotToThread.Keys)
            {
                shard.OnShutdown();
                RemoveShard(shard.GuildID);
            }
            Log.GlobalInfo("Logging out off Discord...");
            Client.LogoutAsync();
            Log.GlobalInfo("Global Bot shutdown complete");
        }

        public Task OnReady()
        {
            CommandService.GlobalCommandInit();
            Log.GlobalInfo("Pre-Server startup Took {ms}ms".Replace("{ms}", stopwatch.ElapsedMilliseconds.ToString()));
            Log.GlobalInfo("Starting " + Client.Guilds.Count.ToString() + " threads. (One thread per guild)");
            foreach (SocketGuild guild in Client.Guilds)
            {
                StartShard(guild.Id);
            }
            Log.GlobalInfo("Full startup time was {ms}ms".Replace("{ms}", stopwatch.ElapsedMilliseconds.ToString()));
            Client.GuildAvailable += GuildJoined;
            return Task.CompletedTask;
        }

        public static BotShard StartShard(ulong guildId)
        {
            if (GuildToThread.ContainsKey(guildId))
            {
                return GuildToThread[guildId];
            }
            Log.GlobalInfo("Starting shard. ID: " + guildId);
            BotShard bot = new(guildId);
            Thread thread = new(bot.StartBot);
            thread.Start();
            BotToThread.Add(bot, thread);
            return bot;
        }

        public static void StopShard(ulong guildId)
        {
            if (HasShard(guildId))
            {
                GetShard(guildId).OnShutdown();
            }
        }

        public static void RestartShard(ulong guildId)
        {
            if (HasShard(guildId))
            {
                Log.GlobalInfo("Restarting Shard " + guildId.ToString());
                GetShard(guildId).OnShutdown();
                RemoveShard(guildId);
                StartShard(guildId);
                Log.GlobalInfo("Shard Restart Complete. ID: " + guildId.ToString());
            }
            else
            {
                Log.GlobalWarning("Tried to restart shard that doesn't exist. ID: " + guildId.ToString());
            }
        }
    }
}