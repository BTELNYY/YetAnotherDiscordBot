using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Configuration;

namespace YetAnotherDiscordBot.ComponentSystem.RankSystemComponent
{
    public class RankSystemComponentConfiguration : ComponentConfiguration
    {
        public override string Filename => "RankConfig.json";

        public bool DeleteDataOnBan { get; set; } = true;

        public bool DeleteDataOnKick { get; set; } = false;

        public uint MinimumTextLength { get; set; } = 3;

        public bool MustHaveSpaces { get; set; } = false;

        public bool AllowDuplicateMessages { get; set; } = false;

        public int StartingLevel { get; set; } = 0;

        public float CurrentXPMultiplier { get; set; } = 1.0f;

        public RankAlgorithmConfiguration RankAlgorithmConfiguratiom { get; set; } = new RankAlgorithmConfiguration();

        public RankXPRandomizerConfiguration RankXPRandomizerConfiguration { get; set; } = new RankXPRandomizerConfiguration();

        public RankRoleConfiguration RankRoleConfiguration { get; set; } = new RankRoleConfiguration();
    }

    public class RankRoleConfiguration
    {
        public List<RankRoleData> RoleData { get; set; } = new List<RankRoleData>();

        public bool RankRoleSystemEnabled { get; set; } = true;
    }

    public class RankRoleData
    {
        public RoleAction RoleAction { get; set; } = RoleAction.Add;

        public ulong RoleId { get; set; } = 0;

        public uint RequiredLevel { get; set; } = 0;
    }

    public enum RoleAction
    {
        Add,
        Remove,
    }

    public class RankXPRandomizerConfiguration
    {
        public float XPMinimum = 0.25f;

        public float XPMaximum = 5.0f;

        public int PercentChancePerMessage = 35;

        public float Randomize()
        {
            Random random = new Random();
            float chanceRolled = random.NextSingle();
            if(chanceRolled < (PercentChancePerMessage / 100f))
            {
                float rolled = random.NextSingle() * (XPMaximum - XPMinimum) + XPMinimum;
                rolled = MathF.Round(rolled, 1);
                return rolled;
            }
            else
            {
                return 0.0f;
            }
        }
    }

    public class RankAlgorithmConfiguration
    {
        public float MinimumPossibleXP = 50;

        public float PainMultiplier = 5;

        public uint Exponent = 2;

        public float FindXP(uint level)
        {
            return MathF.Round((PainMultiplier * MathF.Pow(level, Exponent)) + MinimumPossibleXP, 1);
        }

        public float FindLevel(float xp)
        {
            if(xp == MinimumPossibleXP)
            {
                return 0;
            }
            return MathF.Round(MathF.Pow((xp - MinimumPossibleXP) / PainMultiplier, 1.0f / Exponent), 0);
        }
    }
}