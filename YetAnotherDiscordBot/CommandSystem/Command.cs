using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.ComponentSystem;
using System.Net.Mime;
using System.Reflection;
using YetAnotherDiscordBot.Attributes;
using YetAnotherDiscordBot.CommandSystem;

namespace YetAnotherDiscordBot.CommandBase
{
    public class Command
    {
        public virtual string CommandName { get; } = "commandname";
        public virtual string Description { get; } = "Command Description";

        public bool IsDMEnabled
        {
            get
            {
                return ContentTypes.Contains(InteractionContextType.BotDm);
            }
        }

        public bool IsPrivateMessageEnabled
        {
            get
            {
                return ContentTypes.Contains(InteractionContextType.PrivateChannel);
            }
        }

        public bool IsGuildEnabled
        {
            get
            {
                return ContentTypes.Contains(InteractionContextType.Guild);
            }
        }

        public virtual InteractionContextType[] ContentTypes { get; set; } = new List<InteractionContextType>() 
        {
            InteractionContextType.Guild,
        }.ToArray();
        public virtual GuildPermission RequiredPermission { get; } = GuildPermission.UseApplicationCommands;
        public virtual List<CommandOption> Options { get; set; } = new List<CommandOption>();
        public virtual bool IsDefaultEnabled { get; } = true;
        public virtual List<string> Aliases { get; } = new();
        public virtual List<Type> RequiredComponents { get; } = new List<Type>();
        public BotShard? OwnerShard { get; private set; }
        public List<SocketSlashCommandDataOption> OptionsProcessed { get; private set; } = new List<SocketSlashCommandDataOption>();

        private MethodInfo? _targetExecuteMethod;

        public MethodInfo TargetCommandMethod
        {
            get
            {
                if(_targetExecuteMethod != null)
                {
                    return _targetExecuteMethod;
                }
                Type t = GetType();
                MethodInfo? method = t.GetMethods().Where(x => x.GetCustomAttribute<CommandMethod>() != null).FirstOrDefault();
                if(method != null)
                {
                    _targetExecuteMethod = method;
                }
                else
                {
                    throw new InvalidOperationException("Failed to find Target Execute Method in this command!");
                }
                return _targetExecuteMethod;
            }
        }

        public virtual bool UseLegacyExecute { get; } = false;

        private Log? _log;

        public Log Log
        {
            get
            {
                if(_log == null)
                {
                    if(OwnerShard == null)
                    {
                        _log = new Log(0);
                    }
                    else
                    {
                        _log = new Log(OwnerShard.GuildID);
                    }
                }
                return _log;
            }
        }

        public void Execute(SocketSlashCommand command)
        {
            if (command.GuildId.HasValue)
            {
                OwnerShard = Program.GetShard(command.GuildId.Value);
            }
            //Names of all the params
            List<ParameterInfo> parameters = TargetCommandMethod.GetParameters().ToList();
            List<SocketSlashCommandDataOption> options = command.Data.Options.ToList();
            List<object> invokeArgs = new List<object>();
            foreach (ParameterInfo parameter in parameters)
            {
                if (parameter.ParameterType == typeof(SocketSlashCommand))
                {
                    invokeArgs.Add(command);
                    continue;
                }
                string? paramName = parameter.Name ?? throw new InvalidOperationException("All parameters must have names!");
                SocketSlashCommandDataOption? option = options.Find(x => x.Name == paramName);
                if (option == null)
                {
                    if (!parameter.HasDefaultValue)
                    {
                        throw new InvalidOperationException($"Parameter {paramName} which is marked as required could not be found in the provided command invocation.");
                    }
                    else
                    {
                        object defaultValue = parameter.DefaultValue ?? throw new InvalidOperationException("Default value is null.");
                        invokeArgs.Add(defaultValue);
                        continue;
                    }
                }
                IGenericTypeAdapter? adapter = YetAnotherDiscordBot.Service.CommandService.GetAdapter(option.Value.GetType()) ?? throw new InvalidOperationException($"No adapter for type: {option.Value.GetType().FullName}");
                object? adapted = adapter.AdaptGeneric(option, OwnerShard) ?? throw new InvalidOperationException("Adapter returned a null value.");
                invokeArgs.Add(adapted);
            }
            TargetCommandMethod.Invoke(this, invokeArgs.ToArray());
        }

        public virtual void LegacyExecute(SocketSlashCommand command)
        {
            if(command.GuildId.HasValue)
            {
                OwnerShard = Program.GetShard(command.GuildId.Value);
            }
            else
            {
                Log.GlobalWarning("Ran a command without a guildid, probably a dm command. this is not allowed.");
            }
            OptionsProcessed = GetOptionsOrdered(command.Data.Options.ToList()).ToList();
        }

        public virtual void BuildAliases()
        {

        }
        public virtual void BuildOptions()
        {

        }

        public virtual IEnumerable<SocketSlashCommandDataOption> GetOptionsOrdered(IEnumerable<SocketSlashCommandDataOption> options)
        {
            SocketSlashCommandDataOption[] array = new SocketSlashCommandDataOption[Options.Count];
            foreach (var option in options)
            {
                int counter = 0;
                foreach (var optionbase in Options)
                {
                    if (optionbase != null)
                    {
                        if (optionbase.Name == option.Name)
                        {
                            if (array[counter] != null)
                            {
                                Log.GlobalError("Command options have been duplicated.");
                            }
                            array[counter] = option;
                        }
                    }
                    counter++;
                }
            }
            return array;
        }

        public static Command GetCommandByName(string className, out bool success)
        {
            Type? commandType = Type.GetType(className);
            if(commandType == null)
            {
                Log.GlobalError("Can't get a command class by name! Name: " + className);
                success = false;
                return new Command();
            }
            else
            {
                Command? command = Activator.CreateInstance(commandType) as Command;
                if(command == null)
                {
                    Log.GlobalError("Failed to create class instance by Type reference! Found type but couldn't create class instance. Name: " + className);
                    success = false;
                    return new Command();
                }
                else
                {
                    success = true;
                    return command;
                }
            }
        }

        public void DisplayError(SocketSlashCommand command)
        {
            command.RespondAsync("An error has occured, see log for details.", ephemeral: true);
        }
    }
}
