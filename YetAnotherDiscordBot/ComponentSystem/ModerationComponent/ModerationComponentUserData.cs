using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Configuration;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot.ComponentSystem.ModerationComponent
{
    public class ModerationComponentUserData : DiscordUserData
    {
        public List<Punishment> Punishments { get; set; } = new List<Punishment>();

        public ModerationComponentUserData(ulong id, DiscordUserDataService service) : base(id, service)
        {
        }
    }

    public struct Punishment
    {
        public ModerationComponent.Punishment PunishmentAction;

        public ulong AuthorId;

        public string Reason;

        public ulong Timestamp;
    }
}
