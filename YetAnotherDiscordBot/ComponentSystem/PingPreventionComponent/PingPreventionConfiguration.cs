using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.ComponentSystem.ModerationComponent;
using YetAnotherDiscordBot.Configuration;

namespace YetAnotherDiscordBot.ComponentSystem.PingPreventionComponent
{
    public class PingPreventionConfiguration : ComponentConfiguration
    {
        public override string Filename => "PingPreventionConfiguration.json";

        public List<ulong> PreventedIDs { get; set; } = new List<ulong>();

        public ModerationComponent.ModerationComponent.Punishment Action { get; set; } = ModerationComponent.ModerationComponent.Punishment.Timeout;

        public int ActionDuration { get; set; } = 30;

        public bool RemoveUsersNotInServer { get; set; } = true;
    }
}
