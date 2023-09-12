using Discord;
using Discord.WebSocket;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using YetAnotherDiscordBot.Base;

namespace YetAnotherDiscordBot
{
    public class Program
    {
        public static DiscordSocketClient Client = new();

        public static Dictionary<ulong, Bot> GuildToThread = new Dictionary<ulong, Bot>();

        public static Dictionary<Bot, Thread> BotToThread = new Dictionary<Bot, Thread>();

        public static async Task Main(string[] args)
        {
            await new Program().MainAsync(args);
        }

        public async Task MainAsync(string[] args)
        {
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
            Client.Ready += OnReady;
            Client.Log += LogEvent;
            await Task.Delay(-1);
        }

        private static Task LogEvent(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Error:
                    Log.WriteError(msg.Message + "\n" + msg.Exception);
                    break;
                case LogSeverity.Info:
                    Log.WriteInfo(msg.Message + "\n" + msg.Exception);
                    break;
                case LogSeverity.Warning:
                    Log.WriteWarning(msg.Message + "\n" + msg.Exception);
                    break;
                case LogSeverity.Verbose:
                    Log.WriteVerbose(msg.Message + "\n" + msg.Exception);
                    break;
                case LogSeverity.Critical:
                    Log.WriteCritical(msg.Message + "\n" + msg.Exception);
                    break;
                case LogSeverity.Debug:
                    Log.WriteDebug(msg.Message + "\n" + msg.Exception);
                    break;
                default:
                    Log.WriteWarning("The bellow message failed to be caught by any switch, default warning used.");
                    Log.WriteWarning(msg.Message + "\n" + msg.Exception);
                    break;
            }
            return Task.CompletedTask;
        }

        public Task OnReady()
        {
            Log.WriteInfo("Starting " + Client.Guilds.Count.ToString() + " threads. (One thread per guild)");
            foreach (SocketGuild guild in Client.Guilds)
            {
                Bot bot = new(guild.Id);
                Thread thread = new(bot.StartBot);
                thread.Start();
                BotToThread.Add(bot, thread);
            }
            return Task.CompletedTask;
        }
    }
}