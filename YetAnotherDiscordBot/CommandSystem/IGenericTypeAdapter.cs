using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot.CommandSystem
{
    public interface IGenericTypeAdapter
    {
        object? AdaptGeneric(SocketSlashCommandDataOption option, BotShard shard);
        ApplicationCommandOptionType Type { get; }
    }
}
