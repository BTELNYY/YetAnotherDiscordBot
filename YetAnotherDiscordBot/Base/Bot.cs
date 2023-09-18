using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.Handlers;

namespace YetAnotherDiscordBot.Base
{
    public class Bot
    {
        public Dictionary<string, CommandBase.Command> PerBotCommands = new Dictionary<string, CommandBase.Command>();

        public ulong GuildID { get; private set; } = 0;

        public bool IsFakeBot 
        {   
            get
            {
                return _isfakebot;
            }
            set 
            { 
                if(GuildID != 0)
                {
                    return;
                }
                _isfakebot = value;
            } 
        }

        private bool _isfakebot = false;

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
                Log.Error("GuildID is already handled! GuildID: " + guildId.ToString());
                return;
            }
            Program.GuildToThread.Add(guildId, this);
        }

        public Bot()
        {
            IsFakeBot = true;
        }


        public void OnSlashCommandExecuted(SocketSlashCommand command)
        {
            if (IsFakeBot)
            {
                Log.Error("Attempted to run command on fake bot!");
                return;
            }
            if (!CommandHandler.Commands.ContainsKey(command.CommandName) || PerBotCommands.ContainsKey(command.CommandName))
            {
                Log.Error("Command Not registered in Dict: " + command.CommandName);
                command.RespondAsync("Sorry, this command is not registered internally, contact the developer about this.");
                var result = Client.GetGlobalApplicationCommandAsync(command.CommandId);
                result.AsTask().Wait();
                result.Result.DeleteAsync().Wait();
                return;
            }
            try
            {
                if (!CommandHandler.Commands.ContainsKey(command.CommandName))
                {
                    PerBotCommands[command.CommandName].Execute(command);
                }
                else
                {
                    CommandHandler.Commands[command.CommandName].Execute(command);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Executing Command " + command.CommandName + " threw an exception: \n" + ex.ToString());
                return;
            }
            return;
        }

        public void StartBot()
        {
            if (IsFakeBot)
            {
                Log.Error("Can't start fake bots!");
            }
        }
    }
}
