using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot.Configuration
{
    public class ServerConfiguration : Configuration
    {
        public string Nickname = string.Empty;

        public List<string> AddedComponents = new List<string>();

        public List<string> AddedCommands = new List<string>();
    }
}
