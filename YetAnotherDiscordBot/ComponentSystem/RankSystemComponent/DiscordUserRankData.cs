using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Configuration;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot.ComponentSystem.RankSystemComponent
{
    public class DiscordUserRankData : DiscordUserData
    {
        public override string Filename => "Rank.json";

        public uint Level = 0;

        public float XP = 0;

        public float PersonalXPMultiplier = 1.0f;

        public bool XPLocked = false;

        public DiscordUserRankData(ulong id, DiscordUserDataService service) : base(id, service)
        {
        }
    }
}
