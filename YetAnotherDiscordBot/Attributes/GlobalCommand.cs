using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot.Attributes
{
    /// <summary>
    /// Used for defining global commands. Use <see cref="ComponentSystem.Component.ImportedCommands"/> to define component commands.
    /// </summary>
    public class GlobalCommand : Attribute
    {
    }
}
