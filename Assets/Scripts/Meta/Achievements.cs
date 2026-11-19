using System;
using System.Collections.Generic;

namespace Shrunk
{
    public enum AchievementId
    {
        FirstSteps, MothersDay, Regicide, Untouchable, Omelette, HandsOff,
        Deeper, Hoarder, ShellShock, BubbleWrap, SweetTooth, FullPockets
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
            new Achievement { id = AchievementId.SweetTooth, name = "Sweet Tooth", how = "Blow out the Trick Candle for good.", reward = "the Gold style", style = "Gold" },
            new Achievement { id = AchievementId.FullPockets, name = "Full Pockets", how = "Extract with 300 samples or more.", reward = "bragging rights, and a peg on the board" },
        };

        public const int ShellsNeeded = 20, PodsNeeded = 50, ItemsNeeded = 6, SamplesNeeded = 300;

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
