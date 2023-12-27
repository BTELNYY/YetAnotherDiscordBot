using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot.Configuration
{
    public class ComponentConfiguration : Configuration
    {
        public virtual string Filename { get; } = "config.json";
    }
}
