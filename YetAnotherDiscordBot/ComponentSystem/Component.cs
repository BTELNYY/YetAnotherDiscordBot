namespace YetAnotherDiscordBot.ComponentSystem
{
    public class Component
    {
        public virtual string Name { get; } = nameof(Component);

        public virtual string Description { get; } = "";

        public virtual List<Type> RequiredComponents { get; } = new List<Type>();

        public virtual List<Type> MutuallyExclusiveComponents { get; } = new List<Type>();

        public virtual List<Type> ImportedCommands { get; } = new List<Type>();

        private BotShard? _ownerShard;

        public BotShard OwnerShard 
        {
            get
            {
                if(_ownerShard == null)
                {
                    Log.Error("Tried to get OwnerShard on not ready component.");
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
        }

        public virtual void OnRemoved()
        {

        }

        public virtual void OnShutdown()
        {

        }

        public virtual void OnValidate()
        {

        }

        public virtual void OnValidated()
        {

        }

        public virtual bool Start()
        {
            return true;
        }
    }
}