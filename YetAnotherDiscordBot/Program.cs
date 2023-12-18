using Discord;
using Discord.WebSocket;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Runtime.CompilerServices;
using YetAnotherDiscordBot.Configuration;
using YetAnotherDiscordBot.Handlers;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot
{
    public class Program
    {
        public static DiscordSocketClient Client = new();

        public static Dictionary<ulong, BotShard> GuildToThread = new Dictionary<ulong, BotShard>();

        public static Dictionary<BotShard, Thread> BotToThread = new Dictionary<BotShard, Thread>();

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
            Directory.CreateDirectory(InternalConfig.LogPath);
            Log.GlobalInfo("Starting up!");
            stopwatch = Stopwatch.StartNew();
            DiscordSocketConfig config = new()
            {
                GatewayIntents = GatewayIntents.All,
                MessageCacheSize = 50,              
            };
            string token = "";
            token = ConfigurationService.GlobalConfiguration.Token;
            Client = new(config);
            Log.GlobalInfo("Connecting to discord...");
            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();
            await Client.SetStatusAsync(UserStatus.DoNotDisturb);
            await Client.SetCustomStatusAsync("In Dev!");
            Log.GlobalInfo("Starting Services...");
            ConfigurationService.Start();
            ShutdownService.Start();
            ShutdownService.OnShutdownSignal += OnShutdown;
            Log.GlobalInfo("Register Events...");
            Client.Ready += OnReady;
            Client.Log += LogEvent;
            Client.GuildAvailable += GuildJoined;
            Client.ApplicationCommandCreated += ApplicationCommandCreated;
            Client.SlashCommandExecuted += CommandHandler.SlashCommandExecuted;
            await Task.Delay(-1);
        }

        private Task GuildJoined(SocketGuild arg)
        {
            Log.GlobalInfo("Added to new Guild at runtime. Starting new shard!");
            StartShard(arg.Id);
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
            }
            Log.GlobalInfo("Logging out off Discord...");
            Client.LogoutAsync();
            Log.GlobalInfo("Global Bot shutdown complete");
        }

        public Task OnReady()
        {
            CommandHandler.GlobalCommandInit();
            Log.GlobalInfo("Pre-Server startup Took {ms}ms".Replace("{ms}", stopwatch.ElapsedMilliseconds.ToString()));
            Log.GlobalInfo("Starting " + Client.Guilds.Count.ToString() + " threads. (One thread per guild)");
            foreach (SocketGuild guild in Client.Guilds)
            {
                StartShard(guild.Id);
            }
            Log.GlobalInfo("Full startup time was {ms}ms".Replace("{ms}", stopwatch.ElapsedMilliseconds.ToString()));
            return Task.CompletedTask;
        }

        public static BotShard StartShard(ulong guildId)
        {
            if (GuildToThread.ContainsKey(guildId))
            {
                return GuildToThread[guildId];
            }
            BotShard bot = new(guildId);
            Thread thread = new(bot.StartBot);
            thread.Start();
            BotToThread.Add(bot, thread);
            return bot;
        }
    }
}