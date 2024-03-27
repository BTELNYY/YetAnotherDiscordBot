using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Service;

namespace YetAnotherDiscordBot.Configuration
{
    public class ComponentConfiguration : Configuration
    {
        public virtual string Filename { get; } = "config.json";

        public ulong OwnerID { get; set; } = 0;

        public virtual bool Save()
        {
            return ConfigurationService.SaveComponentConfiguration(this);
        }

        public virtual bool Reset()
        {
            if(OwnerID == 0)
            {
                Log.GlobalError("OwnerID is 0, this is disallowed.");
                return false;
            }
            return ConfigurationService.ResetComponentConfiguration(this);
        }

        public virtual bool Delete()
        {
            return ConfigurationService.DeleteComponentConfiguration(this);
        }
    }
}
