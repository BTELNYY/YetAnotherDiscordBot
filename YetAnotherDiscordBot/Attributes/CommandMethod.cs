using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandMethod : Attribute
    {
        public CommandMethod()
        {

        }
    }
}
