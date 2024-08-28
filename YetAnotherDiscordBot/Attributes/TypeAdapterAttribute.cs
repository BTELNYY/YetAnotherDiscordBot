using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot.Attributes
{
    public class TypeAdapterAttribute : Attribute
    {
        public TypeAdapterAttribute(Type adapterType)
        { 
            AdapterType = adapterType;
        }

        public Type AdapterType { get; set; }
    }
}
