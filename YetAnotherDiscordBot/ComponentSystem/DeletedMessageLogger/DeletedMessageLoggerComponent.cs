using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot.ComponentSystem.DeletedMessageLogger
{
    public class DeletedMessageLoggerComponent : Component
    {
        public override string Name => nameof(DeletedMessageLoggerComponent);

        public override string Description => "Component to intercept deleted messages and display them to a channel.";

        
    }
}
