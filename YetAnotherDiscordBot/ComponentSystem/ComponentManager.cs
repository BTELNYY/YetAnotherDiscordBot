﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;

namespace YetAnotherDiscordBot.ComponentSystem
{
    public class ComponentManager
    {
        public List<Component> CurrentComponents = new List<Component>();

        public T GetComponent<T>(out bool success) where T : Component
        {
            Component? component = CurrentComponents.Find(x => x.GetType() == typeof(T));
            if(component != null)
            {
                if(component is T result)
                {
                    success = true;
                    return result;
                }
            }
            success = false;
            return (T)new Component();
        }

        public BotShard BotShard
        {
            get
            {
                return _botShard;
            }
        }

        private BotShard _botShard;

        public ComponentManager(BotShard shard)
        {
            _botShard = shard;
        }

        private Log? _log;

        public Log Log
        {
            get
            {
                if (_log == null)
                {
                    _log = new Log(_botShard.GuildID);
                }
                return _log;
            }
        }

        public void Start()
        {
            int totalCounter = 0;
            int successCounter = 0;
            foreach(string name in BotShard.ServerConfiguration.AddedComponents)
            {
                Component component = GetComponentByName(name, out bool success);
                if (!success)
                {
                    Log.Warning("Failed to add component! Getting returned error.");
                    totalCounter++;
                    continue;
                }
                CurrentComponents.Add(component);
                component.OnAdded(BotShard);
                totalCounter++;
                successCounter++;
            }
            Log.Info($"Added {successCounter} out of {totalCounter} of components.");
            CheckComponents();
        }


        public void CheckComponents()
        {
            int totalCounter = BotShard.ServerConfiguration.AddedComponents.Count;
            int successCounter = 0;
            List<Component> invalidComponents = new();
            foreach (Component c in CurrentComponents)
            {
                c.OnValidate();
                if (c.RequiredComponents.Count > 0 && CurrentComponents.Where(x => c.RequiredComponents.Contains(x.GetType())).ToList().Count == 0)
                {
                    Log.Error($"Component {c.Name} is missing required dependencies. \n List of dependencies: {string.Join(", ", c.RequiredComponents.Select(x => x.Name))}");
                    invalidComponents.Add(c);
                    continue;
                }
                if (c.MutuallyExclusiveComponents.Count > 0 && CurrentComponents.Where(x => c.MutuallyExclusiveComponents.Contains(x.GetType())).ToList().Count > 0)
                {
                    Log.Error($"Component {c.Name} has mutually exclusive components already loaded. \n List of Mutually Exclusive Components: {string.Join(", ", c.MutuallyExclusiveComponents.Select(x => x.Name))}");
                    invalidComponents.Add(c);
                    continue;
                }
                foreach (Type cmd in c.ImportedCommands)
                {
                    if (!cmd.IsSubclassOf(typeof(Command)))
                    {
                        Log.Warning($"Type {cmd.FullName} isn't a subclass of Type command.");
                        continue;
                    }
                    Command? command = (Command?)Activator.CreateInstance(cmd);
                    if (command == null)
                    {
                        Log.Warning($"Command for type {cmd.FullName} is null, activator failed.");
                        continue;
                    }
                    if (CurrentComponents.Where(x => command.RequiredComponents.Contains(x.GetType())).Count() == 0)
                    {
                        Log.Error($"Command {command.CommandName} cannot be added becuase it is missing required components. \nComponent list: {string.Join(", ", command.RequiredComponents.Select(x => x.Name))}");
                        continue;
                    }
                    bool success = BotShard.BuildShardCommand(command).Result;
                    if (!success)
                    {
                        Log.Warning($"Failure to build command  {command.CommandName}");
                        continue;
                    }
                }
                successCounter++;
                c.OnValidated();
            }
            invalidComponents.ForEach(c =>
            {
                CurrentComponents.Remove(c);
                c.OnRemoved();
            });
            invalidComponents.Clear();
            Log.Info($"Successfully added {successCounter} out of {totalCounter} components to shard ID {BotShard.GuildID}");
            int validatedTotal = 0;
            int validatedSuccess = 0;
            foreach (Component validated in CurrentComponents)
            {
                if (validated.HasLoaded)
                {
                    validatedSuccess++;
                }
                else
                {
                    if (validated.Start())
                    {
                        validatedSuccess++;
                    }
                }
                validatedTotal++;
            }
            Log.Info($"Successfully started {validatedSuccess} out of {validatedTotal} validated components. Total components listed was {BotShard.ServerConfiguration.AddedComponents.Count}");
        }

        public static Component GetComponentByName(string name, out bool success)
        {
            Type? type = Type.GetType(name);
            if(type == null)
            {
                Log.GlobalError("Failed to find Component by name. Name: " + name);
                success = false;
                return new Component();
            }
            else
            {
                Component? component = (Component?)Activator.CreateInstance(type);
                if(component == null)
                {
                    Log.GlobalError("Type specified isnt a component. Name: " + name);
                    success = false;
                    return new Component();
                }
                else
                {
                    success = true;
                    return component;
                }
            }
        }

        public void OnShutdown()
        {
            CurrentComponents.ForEach(c =>
            {
                c.OnShutdown();
                c.OnRemoved();
            });
            CurrentComponents.Clear();
        }
    }
}