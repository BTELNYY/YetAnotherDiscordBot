using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Configuration;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot
{
    public class Log
    {
        private ulong _discordTarget;

        public ulong DiscordTarget
        {
            get
            {
                return _discordTarget;
            }
        }

        public string FilePath
        {
            get
            {
                return ConfigurationService.ServerConfigFolder + DiscordTarget.ToString() + "/logs/";
            }
        }

        static GlobalConfiguration InternalConfig
        {
            get
            {
                return ConfigurationService.GlobalConfiguration;
            }
        }

        public string FileName
        {
            get
            {
                string date = DateTime.Now.ToString("dd-MM-yyyy");
                return date + ".log";
            }
        }

        public Log(ulong discordID)
        {
            _discordTarget = discordID;
            Directory.CreateDirectory(FilePath);
        }

        public void Fatal(string message)
        {
            Write(LogLevel.Fatal_Error, message);
        }

        public void Critical(string message)
        {
            Write(LogLevel.Critical, message);
        }

        public void Error(string message)
        {
            Write(LogLevel.Error, message);
        }

        public void Warning(string message)
        {
            Write(LogLevel.Warning, message);
        }

        public void Info(string message)
        {
            Write(LogLevel.Info, message);
        }

        public void Debug(string message)
        {
            Write(LogLevel.Debug, message);
        }

        public void Verbose(string message)
        {
            Write(LogLevel.Verbose, message);
        }

        public void Write(LogLevel level, string message)
        {
            StackFrame stackFrame = new StackFrame(2);
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = FilePath + FileName;
            if(DiscordTarget == 0)
            {
                GlobalWarning("Discord Target is 0, writing to global log. This should generally not happen.");
                file = InternalConfig.LogPath + FileName;
            }
            string stackframe = string.Empty;
            if (InternalConfig.PrintStackFrames)
            {
                if (InternalConfig.StackframePrintLevels.Contains(level))
                {
                    stackframe = "Stackframe: \n " + stackFrame.ToString();
                }
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" {level.ToString().ToUpper().Replace("_", " ")}]: " + message + "\n" + stackframe);
            sw.Close();
            WriteConsole(level, message, DiscordTarget);
        }

        public static void WriteConsole(LogLevel level, string message, ulong id = 0)
        {
            ConsoleColor matchedColor = ConsoleColor.White;
            switch (level)
            {
                case LogLevel.Fatal_Error:
                    matchedColor = ConsoleColor.DarkRed;
                    break;
                case LogLevel.Critical:
                    matchedColor = ConsoleColor.DarkYellow;
                    break;
                case LogLevel.Error:
                    matchedColor = ConsoleColor.Red;
                    break;
                case LogLevel.Warning:
                    matchedColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Debug:
                    matchedColor = ConsoleColor.Gray;
                    break;
                case LogLevel.Verbose:
                    matchedColor = ConsoleColor.Gray;
                    break;
            }
            Console.ForegroundColor = matchedColor;
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string messageFormatted = "";
            if (id != 0)
            {
                messageFormatted = "[" + time + $" {level.ToString().ToUpper().Replace("_", " ")} {id}]: " + message;
            }
            else
            {
                messageFormatted = "[" + time + $" {level.ToString().ToUpper().Replace("_", " ")}]: " + message;
            }
            Console.WriteLine(messageFormatted);
        }

        public static void WriteLineColor(string msg, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        public static void GlobalSuccess(string msg)
        {
            if (!InternalConfig.EnableLogging)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = InternalConfig.LogPath + date + ".log";
            if (InternalConfig.ShowLogsInConsole)
            {
                Console.WriteLine("[" + time + $" SUCCESS]: " + msg);
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" SUCCESS]: " + msg + "\n");
            sw.Close();
            Console.ResetColor();
        }

        public static void GlobalError(string msg)
        {
            if (!InternalConfig.EnableLogging)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = InternalConfig.LogPath + date + ".log";
            if (InternalConfig.ShowLogsInConsole)
            {
                Console.WriteLine($"[" + time + $" ERROR]: " + msg);
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" ERROR]: " + msg + "\n");
            sw.Close();
            Console.ResetColor();
        }

        public static void GlobalFatal(string msg)
        {
            if (!InternalConfig.EnableLogging)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = InternalConfig.LogPath + date + ".log";
            if (InternalConfig.ShowLogsInConsole)
            {
                Console.WriteLine("[" + time + $" FATAL ERROR]: " + msg);
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" FATAL ERROR]: " + msg + "\n");
            sw.Close();
            Console.ResetColor();
        }

        public static void GlobalWarning(string msg)
        {
            if (!InternalConfig.EnableLogging)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = InternalConfig.LogPath + date + ".log";
            if (InternalConfig.ShowLogsInConsole)
            {
                Console.WriteLine("[" + time + $" WARNING]: " + msg);
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" WARNING]: " + msg + "\n");
            sw.Close();
            Console.ResetColor();
        }

        public static void GlobalInfo(string msg)
        {
            if (!InternalConfig.EnableLogging)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.White;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = InternalConfig.LogPath + date + ".log";
            if (InternalConfig.ShowLogsInConsole)
            {
                Console.WriteLine("[" + time + $" INFO]: " + msg);
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" INFO]: " + msg + "\n");
            sw.Close();
            Console.ResetColor();
        }

        public static void GlobalDebug(string msg)
        {
            if (!InternalConfig.EnableLogging)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = InternalConfig.LogPath + date + ".log";
            if (InternalConfig.ShowLogsInConsole)
            {
                Console.WriteLine("[" + time + $" DEBUG]: " + msg);
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" DEBUG]: " + msg + "\n");
            sw.Close();
            Console.ResetColor();
        }

        public static void GlobalVerbose(string msg)
        {
            if (!InternalConfig.EnableLogging)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = InternalConfig.LogPath + date + ".log";
            if (InternalConfig.ShowLogsInConsole)
            {
                Console.WriteLine("[" + time + $" VERBOSE]: " + msg);
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" VERBOSE]: " + msg + "\n");
            sw.Close();
            Console.ResetColor();
        }

        public static void GlobalCritical(string msg)
        {
            if (!InternalConfig.EnableLogging)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = InternalConfig.LogPath + date + ".log";
            if (InternalConfig.ShowLogsInConsole)
            {
                Console.WriteLine("[" + time + $" CRITICAL]: " + msg);
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" CRITICAL]: " + msg + "\n");
            sw.Close();
            Console.ResetColor();
        }
    }

    public enum LogLevel
    {
        Fatal_Error,
        Critical,
        Error,
        Warning,
        Info,
        Debug,
        Verbose,
    }
}
