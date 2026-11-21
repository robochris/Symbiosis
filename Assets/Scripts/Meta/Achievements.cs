using System;
using System.Collections.Generic;

namespace Shrunk
{
    public enum AchievementId
    {
        FirstSteps, MothersDay, Regicide, Untouchable, Omelette, HandsOff,
        Deeper, Hoarder, ShellShock, BubbleWrap, SweetTooth, FullPockets,
        PaperTrail, PerformanceReview, SpringClean, Unplugged, Defrosted,
        LooseChange, FiledAway, TimeAndAHalf, AskedForIt, Reboot, CleanSweep, GoldDigger, QuietPlease, OnARoll, HouseWins, CheckedOut, TongueTied, Croaked, SilverLining
    }

    /// <summary>One achievement: what to do, and what doing it gives.</summary>
    public class Achievement
    {
        public AchievementId id;
        public string name;
        public string how;
        /// <summary>What it gives, as the player reads it.</summary>
        public string reward;
        /// <summary>A character style it unlocks, by name, or null.</summary>
        public string style;
        /// <summary>An item it puts into the drop pool, or null.</summary>
        public ItemType? item;
        /// <summary>A biome it adds to the portal room, by id, or null.</summary>
        public string biome;
    }

    /// <summary>
    /// The achievements, and everything they unlock. Power is earned by playing: every style, the last four items and
    /// the second biome each sit behind one of these. Progress is kept in the save file.
    /// </summary>
    public static class Achievements
    {
        /// <summary>For trying things out and for tests: on, nothing is locked.</summary>
        public static bool EverythingUnlocked;

        public static event Action<Achievement> Unlocked;

        public static readonly Achievement[] All =
        {
            new Achievement { id = AchievementId.FirstSteps, name = "First Steps", how = "Clear floor 1.", reward = "the Toy style", style = "Toy" },
            new Achievement { id = AchievementId.MothersDay, name = "Mother's Day", how = "Beat the Brood Mother.", reward = "Larva Buddy can now drop", item = ItemType.LarvaBuddy },
            new Achievement { id = AchievementId.Regicide, name = "Regicide", how = "Beat the Ant Queen.", reward = "a portal to the Birthday Cake", biome = "cake" },
            new Achievement { id = AchievementId.Untouchable, name = "Untouchable", how = "Clear a whole floor without being hit.", reward = "the Ghost style", style = "Ghost" },
            new Achievement { id = AchievementId.Omelette, name = "Omelette", how = "Beat the Ant Queen with every egg destroyed.", reward = "the Ember style", style = "Ember" },
            new Achievement { id = AchievementId.HandsOff, name = "Hands Off", how = "Beat the Ant Queen without breaking a single egg.", reward = "nothing. Yet. Something deeper noticed" },
            new Achievement { id = AchievementId.Deeper, name = "Deeper", how = "Step through a portal to a second biome.", reward = "the Chrome style", style = "Chrome" },
            new Achievement { id = AchievementId.Hoarder, name = "Hoarder", how = "Carry 6 items at once.", reward = "Pheromone Cloud can now drop", item = ItemType.PheromoneCloud },
            new Achievement { id = AchievementId.ShellShock, name = "Shell Shock", how = "Shatter 20 hardened shells.", reward = "Needle Tip can now drop", item = ItemType.NeedleTip },
            new Achievement { id = AchievementId.BubbleWrap, name = "Bubble Wrap", how = "Pop 50 larva pods.", reward = "Wing Buds can now drop", item = ItemType.WingBuds },
            new Achievement { id = AchievementId.SweetTooth, name = "Sweet Tooth", how = "Blow out the Trick Candle for good.", reward = "the Gold style, and a portal under the sofa", style = "Gold", biome = "sofa" },
            new Achievement { id = AchievementId.FullPockets, name = "Full Pockets", how = "Extract with 300 samples or more.", reward = "bragging rights, and a peg on the board" },
            new Achievement { id = AchievementId.PaperTrail, name = "Paper Trail", how = "Find every page of the notebook.", reward = "a door the radio would rather you had not" },
            new Achievement { id = AchievementId.PerformanceReview, name = "Performance Review", how = "Reach the bottom, and bore the cat into leaving.", reward = "the Lab Coat style", style = "Lab Coat" },
            new Achievement { id = AchievementId.SpringClean, name = "Spring Clean", how = "Switch off the Robot Vacuum.", reward = "the Lint style, and a portal into the fridge", style = "Lint", biome = "fridge" },
            new Achievement { id = AchievementId.Unplugged, name = "Unplugged", how = "Make the Robot Vacuum's recharge fail.", reward = "nothing but the look on its face, and it hasn't got one" },
            new Achievement { id = AchievementId.Defrosted, name = "Defrosted", how = "Switch off the Ice Maker.", reward = "the Frost style, and a portal to somewhere that is not in the house at all", style = "Frost", biome = "library" },
            new Achievement { id = AchievementId.LooseChange, name = "Loose Change", how = "Put down 10 mimics.", reward = "the certainty that no coin can be trusted" },
            new Achievement { id = AchievementId.FiledAway, name = "Filed Away", how = "Get every kind of item on file in the cabinet.", reward = "the Paper style", style = "Paper" },
            new Achievement { id = AchievementId.TimeAndAHalf, name = "Time and a Half", how = "Extract from a run on overtime.", reward = "nothing extra. Read your contract" },
            new Achievement { id = AchievementId.AskedForIt, name = "Asked For It", how = "Ring 5 dare bells and live.", reward = "the Brass style", style = "Brass" },
            new Achievement { id = AchievementId.Reboot, name = "Off and On Again", how = "Bring the machine its four parts, switch it on, and beat it.", reward = "the next cycle. It is like this one, with fewer excuses" },
            new Achievement { id = AchievementId.CleanSweep, name = "Clean Sweep", how = "Have every target on the range down at the same moment.", reward = "nothing. It was noticed, though" },
            new Achievement { id = AchievementId.GoldDigger, name = "Gold Digger", how = "Catch 5 gold bugs before they leave.", reward = "the Beetle style", style = "Beetle" },
            new Achievement { id = AchievementId.QuietPlease, name = "Quiet, Please", how = "Beat the Librarian without once making a noise in the quiet.", reward = "the Ink style", style = "Ink" },
            new Achievement { id = AchievementId.OnARoll, name = "On a Roll", how = "Put down 15 in a row without a pause.", reward = "nothing but momentum" },
            new Achievement { id = AchievementId.CheckedOut, name = "Checked Out", how = "Beat the Librarian.", reward = "a portal to somewhere with weather", biome = "forest" },
            new Achievement { id = AchievementId.TongueTied, name = "Tongue-Tied", how = "Beat the Toad without once being caught by its tongue.", reward = "the Moss style", style = "Moss" },
            new Achievement { id = AchievementId.Croaked, name = "Croaked", how = "Beat the Toad.", reward = "a portal up into the weather", biome = "clouds" },
            new Achievement { id = AchievementId.SilverLining, name = "Silver Lining", how = "Beat the Thundercloud without once being struck by its lightning.", reward = "the Storm style", style = "Storm" },
            new Achievement { id = AchievementId.HouseWins, name = "The House Wins", how = "Have the claw machine drop it 10 times.", reward = "the machine's respect. It has none" },
        };

        public const int ShellsNeeded = 20, PodsNeeded = 50, ItemsNeeded = 6, SamplesNeeded = 300, MimicsNeeded = 10, DaresNeeded = 5, GoldBugsNeeded = 5, StreakNeeded = 15, ClawLossesNeeded = 10;

        public static Achievement Get(AchievementId id)
        {
            foreach (var achievement in All) if (achievement.id == id) return achievement;
            return null;
        }

        static List<int> Earned => SaveSystem.Data.meta.achievements;

        public static bool Has(AchievementId id) => Earned.Contains((int)id);
        public static int Count => Earned.Count;

        /// <summary>Earns an achievement. Returns false if it was already earned.</summary>
        public static bool Unlock(AchievementId id)
        {
            if (Has(id)) return false;
            Earned.Add((int)id);
            SaveSystem.Save();
            Unlocked?.Invoke(Get(id));
            return true;
        }

        /// <summary>The names of the ones still to do.</summary>
        public static List<string> Remaining()
        {
            var names = new List<string>();
            foreach (var achievement in All) if (!Has(achievement.id)) names.Add(achievement.name);
            return names;
        }

        // ---------- what is unlocked ----------

        /// <summary>Styles are locked unless an achievement gives them. The first style is always free.</summary>
        public static bool StyleUnlocked(string styleName)
        {
            if (EverythingUnlocked) return true;
            int level = Progress.LevelForStyle(styleName);
            if (level > 0) return Progress.Level >= level;
            foreach (var achievement in All)
            {
                if (achievement.style == styleName) return Has(achievement.id);
            }
            return true;
        }

        /// <summary>Items drop unless an achievement holds them back.</summary>
        public static bool ItemUnlocked(ItemType type)
        {
            if (EverythingUnlocked) return true;
            if (!Progress.ItemUnlocked(type)) return false;
            foreach (var achievement in All)
            {
                if (achievement.item.HasValue && achievement.item.Value == type) return Has(achievement.id);
            }
            return true;
        }

        public static bool BiomeUnlocked(string biomeId)
        {
            if (EverythingUnlocked) return true;
            foreach (var achievement in All)
            {
                if (achievement.biome == biomeId) return Has(achievement.id);
            }
            return true;
        }

        /// <summary>What unlocks a style, for the mirror to say. Null if nothing holds it.</summary>
        public static Achievement ForStyle(string styleName)
        {
            foreach (var achievement in All) if (achievement.style == styleName) return achievement;
            return null;
        }
    }
}
