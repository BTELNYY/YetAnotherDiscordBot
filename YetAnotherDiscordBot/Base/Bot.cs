using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot.Base
{
    public class Bot
    {
        public ulong GuildID { get; private set; } = 0;

        public DiscordSocketClient Client 
        {
            get
            {
                return Program.Client;
            }
        }

        public SocketGuild TargetGuild
        {
            get
            {
                return Client.GetGuild(GuildID);
            }
        }

        public Bot(ulong guildId) 
        {
            GuildID = guildId;
            if (Program.GuildToThread.ContainsKey(guildId))
            {
                Log.WriteError("GuildID is already handled! GuildID: " + guildId.ToString());
                return;
            }
            Program.GuildToThread.Add(guildId, this);
        }

        public void StartBot()
        {
            
        }
    }
}
