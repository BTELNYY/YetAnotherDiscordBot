using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Base;
using YetAnotherDiscordBot.CommandBase;
using YetAnotherDiscordBot.Commands;

namespace YetAnotherDiscordBot.Handlers
{
    public class CommandHandler
    {
        public static Dictionary<string, Command> Commands { get; private set; } = new Dictionary<string, Command>();

        public static void GlobalCommandInit()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Log.Info("Registering Global Commands...");
            BuildCommand(new Ping());
            stopwatch.Stop();
            Log.Info("Done! Took {ms}ms".Replace("{ms}", stopwatch.ElapsedMilliseconds.ToString()));
        }

        public static void BuildCommand(Command command)
        {
            DiscordSocketClient client = Program.Client;
            SlashCommandBuilder scb = new();
            scb.WithName(command.CommandName);
            scb.WithDescription(command.Description);
            scb.WithDMPermission(command.IsDMEnabled);
            command.BuildOptions();
            foreach (CommandOptionsBase cop in command.Options)
            {
                Log.Debug("Building option: " + cop.Name);
                scb.AddOption(cop.Name, cop.OptionType, cop.Description, cop.Required);
            }
            scb.DefaultMemberPermissions = command.RequiredPermission;
            scb.IsDefaultPermission = command.IsDefaultEnabled;
            command.BuildAliases();
            Commands.Add(command.CommandName, command);
            Log.Info("Registering Aliases for: " + command.CommandName + "; Alias: " + string.Join(", ", command.Aliases.ToArray()));
            foreach (string alias in command.Aliases)
            {
                Commands.Add(alias, command);
            }
            try
            {
                Log.Info("Building Command: " + command.CommandName);
                if (command.PrivateCommand)
                {
                    client.GetGuild(command.PrivateServerID).CreateApplicationCommandAsync(scb.Build());
                }
                else
                {
                    client.CreateGlobalApplicationCommandAsync(scb.Build());
                }
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Log.Error(json);
            }
            catch (Exception exception)
            {
                Log.Error("Failed to build command: " + command.CommandName + "\n Error: \n " + exception.ToString());
            }
        }

        public static Task SlashCommandExecuted(SocketSlashCommand command)
        {
            if (command.GuildId != null && Program.GuildToThread.TryGetValue((ulong)command.GuildId, out Bot? bot) && bot != null)
            {
                bot.OnSlashCommandExecuted(command);
            }
            else
            {
                Log.Error("Guild isn't managed by a thread!");
            }
            return Task.CompletedTask;
        }
    }
}
