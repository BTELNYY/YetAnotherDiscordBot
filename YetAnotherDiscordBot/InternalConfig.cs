﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot
{
    public static class InternalConfig
    {
        public static bool ShowLogsInConsole { get; } = true;
        public static bool EnableLogging { get; } = true;
        public static string LogPath { get; } = $"./{ConfigurationService.ConfigFolder}/logs/";
    }
}
