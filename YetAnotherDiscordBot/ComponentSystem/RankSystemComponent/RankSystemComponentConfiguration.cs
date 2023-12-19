using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherDiscordBot.Configuration;

namespace YetAnotherDiscordBot.ComponentSystem.RankSystemComponent
{
    public class RankSystemComponentConfiguration : ComponentServerConfiguration
    {
        public override string Filename => "RankConfig.json";

        public bool DeleteDataOnBan = true;

        public bool DeleteDataOnKick = false;

        public uint MinimumTextLength = 3;

        public bool MustHaveSpaces = false;

        public bool AllowDuplicateMessages = false;

        public int StartingLevel = 0;

        public float CurrentXPMultiplier = 1.0f;

        public RankAlgorithmConfiguration RankAlgorithmConfiguratiom = new RankAlgorithmConfiguration();

        public RankXPRandomizerConfiguration RankXPRandomizerConfiguration = new RankXPRandomizerConfiguration();
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
                Log.GlobalDebug("Didn't roll for xp. Chance rolled: " + chanceRolled + " Required: " + (PercentChancePerMessage / 100f));
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