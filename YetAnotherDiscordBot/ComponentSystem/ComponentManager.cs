using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot.ComponentSystem
{
    public class ComponentManager
    {
        public List<Component> CurrentComponents = new List<Component>();

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
            }
            foreach(Component c in CurrentComponents)
            {
                c.OnValidate();
                if (CurrentComponents.Where(x => c.RequiredComponents.Contains(x)).ToList().Count == 0)
                {
                    Log.Error($"Component {c.Name} is missing required dependencies. \n List of dependencies: {string.Join(", ", c.RequiredComponents.Select(x => x.Name))}" );
                    CurrentComponents.Remove(c);
                    c.OnRemoved();
                    continue;
                }
                if (CurrentComponents.Where(x => c.MutuallyExclusiveComponents.Contains(x)).ToList().Count > 0)
                {
                    Log.Error($"Component {c.Name} has mutually exclusive components already loaded. \n List of Mutually Exclusive Components: {string.Join(", ", c.MutuallyExclusiveComponents.Select(x => x.Name))}");
                    CurrentComponents.Remove(c);
                    c.OnRemoved();
                    continue;
                }
                successCounter++;
                c.OnValidated();
            }
            Log.Info($"Successfully added {successCounter} out of {totalCounter} components to shard ID {BotShard.GuildID}");
            int validatedTotal = 0;
            int validatedSuccess = 0;
            foreach(Component validated in CurrentComponents)
            {
                if (validated.Start())
                {
                    validatedSuccess++;
                }
                validatedTotal++;
            }
        }


        public static Component GetComponentByName(string name, out bool success)
        {
            Type? type = Type.GetType(name);
            if(type == null)
            {
                Log.Error("Failed to find Component by name. Name: " + name);
                success = false;
                return new Component();
            }
            else
            {
                Component? component = (Component?)Activator.CreateInstance(type);
                if(component == null)
                {
                    Log.Error("Type specified isnt a component. Name: " + name);
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
    }
}