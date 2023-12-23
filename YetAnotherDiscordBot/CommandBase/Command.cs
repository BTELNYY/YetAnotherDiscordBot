﻿using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.ComponentSystem;

namespace YetAnotherDiscordBot.CommandBase
{
    public class Command
    {
        public virtual string CommandName { get; } = "commandname";
        public virtual string Description { get; } = "Command Description";
        public virtual bool IsDMEnabled { get; } = false;
        public virtual GuildPermission RequiredPermission { get; } = GuildPermission.UseApplicationCommands;
        public virtual List<CommandOptionsBase> Options { get; } = new List<CommandOptionsBase>();
        public virtual bool IsDefaultEnabled { get; } = true;
        public virtual List<string> Aliases { get; } = new();
        public BotShard? OwnerShard { get; private set; }
        public virtual List<Type> RequiredComponents { get; } = new List<Type>();

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


        public virtual void Execute(SocketSlashCommand command)
        {
            if(command.GuildId.HasValue)
            {
                OwnerShard = Program.GetShard(command.GuildId.Value);
            }
            else
            {
                Log.GlobalWarning("Ran a command without a guildid, probably a dm command. this is not allowed.");
            }
        }

        public virtual void BuildAliases()
        {

        }
        public virtual void BuildOptions()
        {

        }

        public virtual SocketSlashCommandDataOption[] GetOptionsOrdered(List<SocketSlashCommandDataOption> options)
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
    }
}
