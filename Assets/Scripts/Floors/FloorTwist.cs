using UnityEngine;

namespace Shrunk
{
    /// <summary>Something different about one floor, said as the player arrives. Most floors have none.</summary>
    public enum FloorTwist
    {
        None,
        /// <summary>More enemies in every room, and each one drops more coins.</summary>
        Crowded,
        /// <summary>The light is off. Every enemy is worth more samples.</summary>
        PowerCut,
        /// <summary>Every fight room has an elite in it.</summary>
        Promotions,
        /// <summary>Everything in the shop is half price.</summary>
        Sale,
        /// <summary>A gold bug in every fight room.</summary>
        GoldRush,
        /// <summary>The haze closes right in. Every enemy is worth a sample more.</summary>
        Mist,
        /// <summary>Everything moves faster, and pays a coin more.</summary>
        RushHour,
        /// <summary>Red light; every enemy is tougher and worth three samples more.</summary>
        BloodMoon,
        /// <summary>The dodge goes much further, and floats over the floor.</summary>
        LowGravity,
        /// <summary>Every kill is confetti, and a coin more.</summary>
        Party,
        /// <summary>Treasure rooms offer one more item to choose from.</summary>
        Lucky,
        /// <summary>Everything is bigger and tougher, and worth two samples more.</summary>
        Giants
    }

    /// <summary>What each twist is called, what it does, and which floors get one.</summary>
    public static class FloorTwists
    {
        public const int CrowdedExtra = 2, CrowdedCoins = 2, PowerCutSamples = 2, MistSamples = 1, RushCoins = 1;
        /// <summary>How close the haze comes in a mist, and how much quicker everything is in a rush.</summary>
        public const float MistStart = 3f, MistEnd = 20f, RushSpeed = 1.25f;
        /// <summary>How much of the light is left in a power cut.</summary>
        public const float PowerCutAmbient = 0.45f, PowerCutSun = 0.3f;

        public static readonly FloorTwist[] All = { FloorTwist.Crowded, FloorTwist.PowerCut, FloorTwist.Promotions, FloorTwist.Sale, FloorTwist.GoldRush, FloorTwist.Mist, FloorTwist.RushHour,
            FloorTwist.BloodMoon, FloorTwist.LowGravity, FloorTwist.Party, FloorTwist.Lucky, FloorTwist.Giants };
        public const float BloodMoonHealth = 1.4f, GiantsHealth = 1.3f, GiantsSize = 1.3f;
        public const int BloodMoonSamples = 3, GiantsSamples = 2, PartyCoins = 1;

        public static string Name(FloorTwist twist)
        {
            switch (twist)
            {
                case FloorTwist.Crowded: return "Crowded";
                case FloorTwist.PowerCut: return "Power cut";
                case FloorTwist.Promotions: return "Promotions";
                case FloorTwist.Sale: return "Closing-down sale";
                case FloorTwist.GoldRush: return "Gold rush";
                case FloorTwist.Mist: return "Mist";
                case FloorTwist.RushHour: return "Rush hour";
                case FloorTwist.BloodMoon: return "Blood moon";
                case FloorTwist.LowGravity: return "Low gravity";
                case FloorTwist.Party: return "Party";
                case FloorTwist.Lucky: return "Lucky";
                case FloorTwist.Giants: return "Giants";
                default: return "";
            }
        }

        /// <summary>What it does, as the player reads it on arriving.</summary>
        public static string Line(FloorTwist twist)
        {
            switch (twist)
            {
                case FloorTwist.Crowded: return "more of them in every room, and they have brought their wallets.";
                case FloorTwist.PowerCut: return "somebody has turned the light off. Samples are worth more in the dark.";
                case FloorTwist.Promotions: return "there is a manager in every room.";
                case FloorTwist.Sale: return "everything in the shop is half price.";
                case FloorTwist.GoldRush: return "a gold bug in every room. They do not wait about.";
                case FloorTwist.Mist: return "you cannot see far. Neither can they. Samples are worth more in it.";
                case FloorTwist.RushHour: return "everything is in a hurry, you included. They pay a coin more for it.";
                case FloorTwist.BloodMoon: return "the light has gone red. Everything is tougher, and worth a lot more.";
                case FloorTwist.LowGravity: return "something is off with the gravity. Your dodge floats, and goes a long way.";
                case FloorTwist.Party: return "somebody is having a party. Every kill is confetti, and a coin more.";
                case FloorTwist.Lucky: return "a lucky floor. Treasure rooms offer an extra choice.";
                case FloorTwist.Giants: return "everything here is bigger than it should be. Tougher, and worth more.";
                default: return "";
            }
        }

        /// <summary>
        /// The twist for one floor, decided by its seed so a saved floor comes back the same. Never on the first two
        /// floors of a run, on the first floor of a biome (it has its own card), on a boss's floor, or at the bottom.
        /// </summary>
        public static FloorTwist Roll(int seed, int floorNumber, BiomeDefinition biome, float chance, FloorTwist? always = null)
        {
            if (floorNumber < 3 || FloorLayout.IsBossFloor(floorNumber) || FloorLayout.FloorInBiome(floorNumber) == 1) return FloorTwist.None;
            if (biome != null && biome.isBottom) return FloorTwist.None;
            if (always.HasValue) return always.Value;
            var rng = new System.Random(seed ^ 0x7A11C0DE);
            if (rng.NextDouble() >= chance) return FloorTwist.None;
            return All[rng.Next(All.Length)];
        }

        /// <summary>The Daily Experiment's twist: the same for everybody on the day, on every floor that can have one.</summary>
        public static FloorTwist ForDay(int daySeed) => All[(daySeed & 0x7fffffff) % All.Length];

        public static int Discount(int price) => price <= 0 ? price : Mathf.Max(1, Mathf.CeilToInt(price * 0.5f));
    }
}
