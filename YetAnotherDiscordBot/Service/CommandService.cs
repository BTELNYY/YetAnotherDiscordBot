using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Attributes;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.CommandSystem;
using YetAnotherDiscordBot.CommandSystem.BuiltIn;

namespace YetAnotherDiscordBot.Service
{
    public class CommandService
    {
        public static Dictionary<string, Command> Commands { get; private set; } = new Dictionary<string, Command>();

        public static Dictionary<Type, IGenericTypeAdapter> Adapters { get; private set; } = new Dictionary<Type, IGenericTypeAdapter>();

        public static void GlobalCommandInit()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Log.GlobalInfo("Registering Global Commands...");
            ImportGlobalCommands(Assembly.GetExecutingAssembly());
            stopwatch.Stop();
            Log.GlobalInfo("Done! Took {ms}ms".Replace("{ms}", stopwatch.ElapsedMilliseconds.ToString()));
        }

        //No discord at this point.
        public static void Start()
        {
            List<Type> adapters = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetCustomAttribute<TypeAdapterAttribute>() != null).ToList();
            foreach(Type adapter in adapters)
            {
                if (!adapter.IsSubclassOfRawGeneric(typeof(TypeAdapter<>)))
                {
                    Log.GlobalError($"Cannot use {adapter.Name} from {adapter.Namespace} becuase it does not implement the TypeAdapter<> class.");
                    continue;
                }
                IGenericTypeAdapter? genericTypeAdapter = (IGenericTypeAdapter?)Activator.CreateInstance(adapter);
                if(genericTypeAdapter == null)
                {
                    Log.GlobalError("Failed to create adapter!");
                    continue;
                }
                TypeAdapterAttribute? attribute = adapter.GetCustomAttribute<TypeAdapterAttribute>();
                if(attribute == null)
                {
                    Log.GlobalError("Failed get attribute!");
                    continue;
                }
                if (Adapters.ContainsKey(attribute.AdapterType))
                {
                    Log.GlobalError($"The type {attribute.AdapterType.Name} has already been given an adapter!");
                    continue;
                }
                Log.GlobalInfo($"Registered {adapter.FullName} for {attribute.AdapterType.FullName}");
                Adapters.Add(attribute.AdapterType, genericTypeAdapter);
            }
        }

        public static void ImportGlobalCommands(Assembly assembly)
        {
            Type[] commands = assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(Command)) && x.GetCustomAttribute<GlobalCommand>() != null).ToArray();
            foreach (Type command in commands)
            {
                BuildCommand(command, true);
            }
        }

        public static void BuildCommand(Type type, bool validateAsGlobalCommand = false)
        {
            if (!(type.IsSubclassOf(typeof(Command)) && type.GetCustomAttribute<GlobalCommand>() != null))
            {
                throw new InvalidOperationException($"Supplied type {type.Name} is invalid and cannot be loaded as a command.");
            }
            object? obj = Activator.CreateInstance(type);
            if (obj == null || obj is not Command)
            {
                throw new InvalidOperationException($"Supplied type {type.Name} is invalid and cannot be loaded as a command.");
            }
            Command command = (Command)obj;
            if (validateAsGlobalCommand && command.RequiredComponents.Any())
            {
                throw new ArgumentException("Tried validating command as Global command but the command has required components, This is not allowed.", "command");
            }
            BuildCommand(command);
        }

        public static void BuildCommand(Command command, bool validateAsGlobalCommand = false)
        {
            if (validateAsGlobalCommand && command.RequiredComponents.Any())
            {
                throw new ArgumentException("Tried validating command as Global command but the command has required components, This is not allowed.", "command");
            }
            DiscordSocketClient client = Program.Client;
            SlashCommandBuilder scb = new();
            scb.WithName(command.CommandName);
            scb.WithDescription(command.Description);
            scb.WithContextTypes(command.ContentTypes);
            if (command.UseLegacyExecute)
            {
                command.BuildOptions();
                foreach (CommandOption cop in command.Options)
                {
                    Log.GlobalDebug("Building option: " + cop.Name);
                    scb.AddOption(cop.Name, cop.OptionType, cop.Description, cop.Required);
                }
            }
            else
            {
                MethodInfo targetMethod = command.TargetCommandMethod;
                List<ParameterInfo> @params = targetMethod.GetParameters().ToList();
                if(@params.First().ParameterType != typeof(SocketSlashCommand))
                {
                    Log.GlobalError($"The first param for the command {command.CommandName} is not a SocketSlashCommand, without this, the command won't work.");
                }
                foreach(ParameterInfo param in @params)
                {
                    CommandParameter? attribute = param.GetCustomAttribute<CommandParameter>();
                    if (param.ParameterType == typeof(SocketSlashCommand))
                    {
                        continue;
                    }
                    if (!Adapters.ContainsKey(param.ParameterType))
                    {
                        Log.GlobalError($"Illegal Paramater type: {param.ParameterType.FullName}");
                        continue;
                    }
                    IGenericTypeAdapter adapter = Adapters[param.ParameterType];
                    SlashCommandOptionBuilder builder = new SlashCommandOptionBuilder();
                    builder.WithName(param.Name);
                    builder.WithRequired(!param.HasDefaultValue);
                    builder.WithType(adapter.Type);
                    builder.WithDescription("(No description was set)");
                    if(attribute != null)
                    {
                        builder = attribute.Builder;
                        if(builder.Type != adapter.Type)
                        {
                            Log.GlobalWarning($"Builder type and adapter type mismatch. Builder type has been overriden.");
                            builder.Type = adapter.Type;
                        }
                        if(builder.IsRequired != !param.HasDefaultValue)
                        {
                            Log.GlobalWarning($"Builder required flag mismatch with parameter. Builder has been overriden.");
                            builder.IsRequired = !param.HasDefaultValue;
                        }
                        if(builder.Name != param.Name)
                        {
                            Log.GlobalWarning($"Builder param name has been mismatched with the param name. Builder name has been overriden.");
                            builder.Name = param.Name;
                        }
                    }
                    scb.AddOption(builder);
                }
            }
            scb.DefaultMemberPermissions = command.RequiredPermission;
            scb.IsDefaultPermission = command.IsDefaultEnabled;
            command.BuildAliases();
            Commands.Add(command.CommandName, command);
            Log.GlobalInfo("Registering Aliases for: " + command.CommandName + "; Alias: " + string.Join(", ", command.Aliases.ToArray()));
            foreach (string alias in command.Aliases)
            {
                Commands.Add(alias, command);
            }
            try
            {
                Log.GlobalInfo("Building Command: " + command.CommandName);
                client.CreateGlobalApplicationCommandAsync(scb.Build());
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Log.GlobalError(json);
            }
            catch (Exception exception)
            {
                Log.GlobalError("Failed to build command: " + command.CommandName + "\n Error: \n " + exception.ToString());
            }
        }

        public static Task SlashCommandExecuted(SocketSlashCommand command)
        {
            //command.DeferAsync(false);
            if (command.GuildId != null && Program.GuildToThread.TryGetValue((ulong)command.GuildId, out BotShard? bot) && bot != null)
            {
                bot.OnSlashCommandExecuted(command);
            }
            else
            {
                Log.GlobalError("Guild isn't managed by a thread!");
                if (command.GuildId.HasValue)
                {
                    Program.StartShard(command.GuildId.Value);
                }
            }
            return Task.CompletedTask;
        }
    }
}
