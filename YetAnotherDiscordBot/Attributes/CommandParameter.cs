using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot.Attributes
{

    [AttributeUsage(AttributeTargets.Parameter)]
    public class CommandParameter : Attribute
    {
        public CommandParameter(string description)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        public CommandParameter(string description, string nameOverride) : this(description)
        {
            NameOverride = nameOverride ?? throw new ArgumentNullException(nameof(nameOverride));
        }

        public CommandParameter(string description, string nameOverride, object defaultValueOverride) : this(description, nameOverride)
        {
            DefaultValueOverride = defaultValueOverride ?? throw new ArgumentNullException(nameof(defaultValueOverride));
        }

        public string Description { get; set; } = string.Empty;

        public string NameOverride { get; set; } = string.Empty;

        public object DefaultValueOverride { get; set; } = new object();

        
    }
}
