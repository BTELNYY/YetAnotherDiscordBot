using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.ComponentSystem;
using YetAnotherDiscordBot.Configuration;
using YetAnotherDiscordBot.Handlers;
using YetAnotherDiscordBot.Service;
using Discord;
using Discord.Net;
using Newtonsoft.Json;
using System.Diagnostics;

namespace YetAnotherDiscordBot
{
    public class BotShard
    {
        public Dictionary<string, Command> PerBotCommands = new Dictionary<string, Command>();

        private List<string> _alreadyPresentCommands = new List<string>();

        public bool IsShuttingDown { get; private set; } = false;

        public ulong GuildID { get; private set; } = 0;

        public DiscordSocketClient Client 
        {
            get
            {
                return Program.Client;
            }
        }

        public string GuildNickname
        {
            get
            {
                return TargetGuild.GetUser(Client.CurrentUser.Id).Nickname;
            }
            private set
            {
                TargetGuild.GetUser(Client.CurrentUser.Id).ModifyAsync(props =>
                {
                    props.Nickname = value;
                });
            }
        }

        private ServerConfiguration? _serverConfig;

        

        public ServerConfiguration ServerConfiguration
        {
            get
            {
                if(_serverConfig == null)
                {
                     ServerConfiguration config = ConfigurationService.GetServerConfiguration(GuildID);
                    _serverConfig = config;
                    return _serverConfig;
                }
                else
                {
                    return _serverConfig;
                }
            }
        }

        public void SaveServerConfiguration()
        {
            ConfigurationService.WriteServerConfiguration(GuildID, ServerConfiguration, true);
        }

        public SocketGuild TargetGuild
        {
            get
            {
                return Client.GetGuild(GuildID);
            }
        }


        public ComponentManager ComponentManager
        {
            get
            {
                if(_componentManager == null)
                {
                    Log.Error("Attempted to access null Component Manager.");
                    _componentManager = new ComponentManager(this);
                }
                return _componentManager;
            }
        }

        private ComponentManager? _componentManager;

        private Log? _log;

        public Log Log
        {
            get
            {
                _log ??= new Log(GuildID);
                return _log;
            }
        }

        private DiscordUserDataService? _discordUserDataService;

        public DiscordUserDataService DiscordUserDataService
        {
            get
            {
                if (_discordUserDataService == null)
                {
                    Log.Error("DiscordUserService is NULL!");
                    _discordUserDataService = new(this);
                }
                return _discordUserDataService;
            }
        }

        public BotShard(ulong guildId)
        {
            GuildID = guildId;
            if (Program.GuildToThread.ContainsKey(guildId))
            {
                Log.Error("GuildID is already handled! GuildID: " + guildId.ToString());
                return;
            }
            _alreadyPresentCommands = TargetGuild.GetApplicationCommandsAsync().Result.Where(x => x.ApplicationId == Client.GetApplicationInfoAsync().Result.Id).Select(x => x.Name).ToList();
            _serverConfig = ConfigurationService.GetServerConfiguration(GuildID);
            _componentManager = new ComponentManager(this);
            _discordUserDataService = new DiscordUserDataService(this);
            _componentManager.Start();
            Program.GuildToThread.Add(guildId, this);
        }

        public void OnSlashCommandExecuted(SocketSlashCommand command)
        {
            if (IsShuttingDown)
            {
                return;
            }
            if (!CommandHandler.Commands.ContainsKey(command.CommandName) && !PerBotCommands.ContainsKey(command.CommandName))
            {
                Log.Error("Command Not registered in Dict: " + command.CommandName);
                command.RespondAsync("Sorry, this command is not registered internally, contact the developer about this.");
                var result = Client.GetGlobalApplicationCommandAsync(command.CommandId);
                result.AsTask().Wait();
                result.Result.DeleteAsync().Wait();
                return;
            }
            try
            {
                if (!CommandHandler.Commands.ContainsKey(command.CommandName))
                {
                    PerBotCommands[command.CommandName].Execute(command);
                }
                else
                {
                    CommandHandler.Commands[command.CommandName].Execute(command);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Executing Command " + command.CommandName + " threw an exception: \n" + ex.ToString());
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle("Error");
                builder.WithCurrentTimestamp();
                builder.Description = "Please report this to the developer. \n```" + ex.ToString() + "```";
                builder.Color = Color.Red;
                command.RespondAsync(embed: builder.Build(), ephemeral: true);
                return;
            }
            return;
        }

        public async Task<bool> BuildShardCommand(Command command)
        {
            SlashCommandBuilder scb = new();
            scb.WithName(command.CommandName);
            scb.WithDescription(command.Description);
            scb.WithContextTypes(command.ContentTypes);
            command.BuildOptions();
            foreach (CommandOption cop in command.Options)
            {
                scb.AddOption(cop.Name, cop.OptionType, cop.Description, cop.Required);
            }
            
            scb.DefaultMemberPermissions = command.RequiredPermission;
            scb.IsDefaultPermission = command.IsDefaultEnabled;
            command.BuildAliases();
            PerBotCommands.Add(command.CommandName, command);
            Log.Info("Registering Aliases for: " + command.CommandName + "; Alias: " + string.Join(", ", command.Aliases.ToArray()));
            foreach (string alias in command.Aliases)
            {
                PerBotCommands.Add(alias, command);
            }
            try
            {
                if (_alreadyPresentCommands.Contains(command.CommandName))
                {
                    Log.Info("Skipping command " + command.CommandName + " because its already added to guild.");
                    return true;
                }
                Log.Info("Building Command: " + command.CommandName);
                SocketApplicationCommand result = await TargetGuild.CreateApplicationCommandAsync(scb.Build());
                Log.Debug($"{result.Id}, {result.Name}, {result.Type}");
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Log.Error(json);
                return false;
            }
            catch (Exception exception)
            {
                Log.Error("Failed to build command: " + command.CommandName + "\n Error: \n " + exception.ToString());
                return false;
            }
            return true;
        }


        public void StartBot()
        {
            if (ServerConfiguration.Nickname != string.Empty)
            {
                GuildNickname = ServerConfiguration.Nickname;
            }
            int totalCounter = 0;
            int successCounter = 0;
            foreach(string commandString in ServerConfiguration.AddedCommands)
            {
                Command command = Command.GetCommandByName(commandString, out bool success);
                if (!success)
                {
                    Log.Warning($"Failed to add command to shard. (Failure to fetch by name) Name: {commandString}, GuildID: {GuildID}");
                    totalCounter++;
                    continue;
                }
                if(ComponentManager.CurrentComponents.Where(x => command.RequiredComponents.Contains(x.GetType())).Count() == 0)
                {
                    Log.Error($"Command {command.CommandName} cannot be added becuase it is missing required components. \nComponent list: {string.Join(", ", command.RequiredComponents.Select(x => x.Name))}");
                    return;
                }
                bool commandBuildSuccess = BuildShardCommand(command).Result;
                if (!commandBuildSuccess)
                {
                    Log.Warning($"Failed to add command to shard. (Failed to build) Name: {commandString}, GuildID: {GuildID}");
                    totalCounter++;
                    continue;
                }
                else
                {
                    successCounter++;
                    totalCounter++;
                }
            }
            Log.Info($"Added {successCounter} out of {totalCounter} of commands to GuildID {GuildID}");
        }

        public void OnShutdown()
        {
            Log.Info("Attempting to shut down thread " + GuildID.ToString());
            ComponentManager.OnShutdown();
            IsShuttingDown = true;
            PerBotCommands.Clear();
            _alreadyPresentCommands.Clear();
            _componentManager = null;
            _discordUserDataService = null;
            _serverConfig = null;
            _log = null;
        }
    }
}