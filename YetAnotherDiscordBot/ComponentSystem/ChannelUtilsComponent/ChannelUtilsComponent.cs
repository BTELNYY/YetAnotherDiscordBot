using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Attributes;
using YetAnotherDiscordBot.ComponentSystem.ChannelUtilsComponent.Commands;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot.ComponentSystem.ChannelUtilsComponent
{
    [BotComponent]
    public class ChannelUtilsComponent : Component
    {
        public override string Name => "ChannelUtilsComponent";

        public override string Description => "Provides Utilities for managing channels";

        public override List<Type> RequiredComponents => new List<Type>()
        {
            typeof(ModerationComponent.ModerationComponent)
        };

        public override List<Type> ImportedCommands => new List<Type>()
        {
            typeof(SetChannelSlowdown),
            typeof(GetAllMessages),
        };

        private ChannelUtilsComponentConfiguration? _configuration;

        public ChannelUtilsComponentConfiguration Configuration
        {
            get
            {
                if(_configuration == null)
                {
                    _configuration = ConfigurationService.GetComponentConfiguration<ChannelUtilsComponentConfiguration>(OwnerShard.GuildID, out bool success, true, true);
                    if (!success)
                    {
                        Log.Error("Failed to get configuration!");
                    }
                }
                return _configuration;
            }
        }

        public override void OnValidated()
        {
            base.OnValidated();
            _configuration = ConfigurationService.GetComponentConfiguration<ChannelUtilsComponentConfiguration>(OwnerShard.GuildID, out bool success, true, true);
            if (!success)
            {
                Log.Error("Failed to get configuration!");
            }
        }
    }
}
