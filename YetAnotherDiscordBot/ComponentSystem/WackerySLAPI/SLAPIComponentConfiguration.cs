using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Configuration;

namespace YetAnotherDiscordBot.ComponentSystem.WackerySLAPI
{
    public class SLAPIComponentConfiguration : ComponentServerConfiguration
    {
        public override string Filename => "SLAPIConfiguration.json";

        public string EndpointURL { get; set; } = "default";

        public Dictionary<string, string> IDToNamesOfServers { get; set; } = new Dictionary<string, string>()
        {

        };

        public bool DisplayRawIDs { get; set; } = true;

        public string AuthToken { get; set; } = "none";

        public ulong ChannelID { get; set; } = 0;

        public int UpdateEverySeconds { get; set; } = 60;
    }
}