using YetAnotherDiscordBot.Configuration;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot.ComponentSystem
{
    public class Component
    {
        public virtual string Name { get; } = nameof(Component);

        public virtual string Description { get; } = "";

        public bool HasLoaded { get; set; } = false;

        public virtual ComponentConfiguration Configuration
        {
            get
            {
                if(_configuration != null)
                {
                    object? configConverted = Convert.ChangeType(_configuration, ConfigurationClass);
                    if(configConverted != null)
                    {
                        _configuration = (ComponentConfiguration)configConverted;
                    }
                }
                else
                {
                    object? instance = Activator.CreateInstance(ConfigurationClass);
                    if(instance == null)
                    {
                        throw new InvalidOperationException($"Can't create by type {ConfigurationClass.Name}!");
                    }
                    else
                    {
                        ComponentConfiguration component = (ComponentConfiguration)instance;
                        object currentConfig = ConfigurationService.GetComponentConfiguration(component, OwnerShard.GuildID, out bool success);
                        if (!success)
                        {
                            throw new InvalidOperationException("Failed to load configuration!");
                        }
                        _configuration = (ComponentConfiguration)currentConfig;
                    }
                }
                return _configuration;
            }
        }

        public virtual Type ConfigurationClass { get; } = typeof(ComponentConfiguration);

        private ComponentConfiguration? _configuration;

        public virtual List<Type> RequiredComponents { get; } = new List<Type>();

        public virtual List<Type> MutuallyExclusiveComponents { get; } = new List<Type>();

        public virtual List<Type> ImportedCommands { get; } = new List<Type>();

        private Log? _log;

        public Log Log
        {
            get
            {
                if(_log == null)
                {
                    if (_ownerShard == null)
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

        private BotShard? _ownerShard;

        public BotShard OwnerShard 
        {
            get
            {
                if(_ownerShard == null)
                {
                    Log.GlobalError("Tried to get OwnerShard on not ready component.");
                    throw new NotSupportedException("Can't get BotShard on non-ready components. Run OnAdded first.");
                }
                else
                {
                    return _ownerShard;
                }
            }
        }

        public virtual void OnAdded(BotShard shard)
        {
            _ownerShard = shard;
            _log = new Log(_ownerShard.GuildID);
        }

        public virtual void OnRemoved()
        {

        }

        public virtual void OnShutdown()
        {

        }

        /// <summary>
        /// Called when the Bot Thread begins validating the component.
        /// </summary>
        public virtual void OnValidate()
        {

        }

        /// <summary>
        /// Called when the Bot Thread validates a component.
        /// </summary>
        public virtual void OnValidated()
        {

        }

        public virtual bool Start()
        {
            HasLoaded = true;
            return true;
        }
    }
}