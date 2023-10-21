using Discord;
using Discord.WebSocket;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using YetAnotherDiscordBot.Base;
using YetAnotherDiscordBot.Handlers;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot
{
    public class Program
    {
        public static DiscordSocketClient Client = new();

        public static Dictionary<ulong, BotShard> GuildToThread = new Dictionary<ulong, BotShard>();

        public static Dictionary<BotShard, Thread> BotToThread = new Dictionary<BotShard, Thread>();

        private Stopwatch stopwatch = new Stopwatch();

        public static async Task Main(string[] args)
        {
            await new Program().MainAsync(args);
        }

        public async Task MainAsync(string[] args)
        {
            Log.Info("Starting up!");
            stopwatch = Stopwatch.StartNew();
            DiscordSocketConfig config = new()
            {
                GatewayIntents = GatewayIntents.All,
                MessageCacheSize = 50,              
            };
            string token = "";
            token = File.ReadAllText("./token.txt");
            Client = new(config);
            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();
            await Client.SetStatusAsync(UserStatus.DoNotDisturb);
            await Client.SetCustomStatusAsync("In Dev!");
            ConfigurationService.Start();
            ShutdownService.Start();
            ShutdownService.OnShutdownSignal += OnShutdown;
            Client.Ready += OnReady;
            Client.Log += LogEvent;
            Client.ApplicationCommandCreated += ApplicationCommandCreated;
            Client.SlashCommandExecuted += CommandHandler.SlashCommandExecuted;
            await Task.Delay(-1);
        }

        private Task ApplicationCommandCreated(SocketApplicationCommand arg)
        {
            Log.Debug("Application command created!");
            return Task.CompletedTask;
        }

        private static Task LogEvent(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Error:
                    Log.Error(msg.Message + "\n" + msg.Exception);
                    break;
                case LogSeverity.Info:
                    Log.Info(msg.Message + "\n" + msg.Exception);
                    break;
                case LogSeverity.Warning:
                    Log.Warning(msg.Message + "\n" + msg.Exception);
                    break;
                case LogSeverity.Verbose:
                    Log.Verbose(msg.Message + "\n" + msg.Exception);
                    break;
                case LogSeverity.Critical:
                    Log.Critical(msg.Message + "\n" + msg.Exception);
                    break;
                case LogSeverity.Debug:
                    Log.Debug(msg.Message + "\n" + msg.Exception);
                    break;
                default:
                    Log.Warning("The bellow message failed to be caught by any switch, default warning used.");
                    Log.Warning(msg.Message + "\n" + msg.Exception);
                    break;
            }
            return Task.CompletedTask;
        }



        public void OnShutdown()
        {
            Log.Info("Logging out off Discord...");
            Client.LogoutAsync();
            Log.Info("Global Bot shutdown complete");
        }

        public Task OnReady()
        {
            CommandHandler.GlobalCommandInit();
            Log.Info("Pre-Server startup Took {ms}ms".Replace("{ms}", stopwatch.ElapsedMilliseconds.ToString()));
            Log.Info("Starting " + Client.Guilds.Count.ToString() + " threads. (One thread per guild)");
            foreach (SocketGuild guild in Client.Guilds)
            {
                BotShard bot = new(guild.Id);
                Thread thread = new(bot.StartBot);
                thread.Start();
                BotToThread.Add(bot, thread);
            }
            Log.Info("Full startup time was {ms}ms".Replace("{ms}", stopwatch.ElapsedMilliseconds.ToString()));
            return Task.CompletedTask;
        }
    }
}