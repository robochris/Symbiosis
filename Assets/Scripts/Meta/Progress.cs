using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    public enum RewardKind { Item, Style, Perk }

    /// <summary>Small standing advantages handed out by level. Never raw power: that is what samples buy.</summary>
    public enum Perk
    {
        /// <summary>Every run starts with a few coins.</summary>
        LooseChange,
        /// <summary>The shop's prices are a tenth lower.</summary>
        BargainHunter,
        /// <summary>A boss leaves two items to choose between instead of one.</summary>
        SecondOpinion,
    }

    /// <summary>What one level gives.</summary>
    public class LevelReward
    {
        public int level;
        public RewardKind kind;
        public ItemType item;
        public string style;
        public Perk perk;
        /// <summary>How it reads on the toast and in the lab: "Sprinkles can drop".</summary>
        public string line;
    }

    /// <summary>
    /// Experience and levels: the long arc between runs. XP comes from going deeper, beating bosses, getting home and
    /// finishing jobs. Each level up to the top hands out one thing in a fixed order: a new item in the drop pool, a
    /// new style, or a small perk. Past the top every level is a box of samples. Samples still buy the lab upgrades;
    /// levels add variety, not strength.
    /// </summary>
    public static class Progress
    {
        public const int FloorXp = 10, MiniBossXp = 30, BossXp = 60, ExtractXp = 25, JobXp = 50;
        public const int SamplesPastTheTop = 25;
        public const int LooseChangeCoins = 5;

        /// <summary>Tests set this to pretend a level (the defaults pin it at the top, so old tests see every item).</summary>
        public static int? LevelOverride;
        /// <summary>Tests set this to switch every perk on or off regardless of level.</summary>
        public static bool? PerksOverride;

        /// <summary>Raised for every level gained, with the level reached.</summary>
        public static event Action<int> LevelledUp;

        public static readonly LevelReward[] Ladder =
        {
            new LevelReward { level = 2, kind = RewardKind.Item, item = ItemType.RubberBand, line = "Rubber Band can drop" },
            new LevelReward { level = 3, kind = RewardKind.Style, style = "Sprout", line = "the Sprout style" },
            new LevelReward { level = 4, kind = RewardKind.Item, item = ItemType.Sprinkles, line = "Sprinkles can drop" },
            new LevelReward { level = 5, kind = RewardKind.Perk, perk = Perk.LooseChange, line = $"Loose Change: every run starts with {LooseChangeCoins} coins" },
            new LevelReward { level = 6, kind = RewardKind.Item, item = ItemType.IceCube, line = "Ice Cube can drop" },
            new LevelReward { level = 7, kind = RewardKind.Style, style = "Coral", line = "the Coral style" },
            new LevelReward { level = 8, kind = RewardKind.Item, item = ItemType.FridgeMagnet, line = "Fridge Magnet can drop" },
            new LevelReward { level = 9, kind = RewardKind.Perk, perk = Perk.BargainHunter, line = "Bargain Hunter: the shop is a tenth cheaper" },
            new LevelReward { level = 10, kind = RewardKind.Item, item = ItemType.Lunchbox, line = "Lunchbox can drop" },
            new LevelReward { level = 11, kind = RewardKind.Style, style = "Midnight", line = "the Midnight style" },
            new LevelReward { level = 12, kind = RewardKind.Item, item = ItemType.PiggyBank, line = "Piggy Bank can drop" },
            new LevelReward { level = 13, kind = RewardKind.Item, item = ItemType.BirthdayCandle, line = "Birthday Candle can drop" },
            new LevelReward { level = 14, kind = RewardKind.Perk, perk = Perk.SecondOpinion, line = "Second Opinion: bosses leave two items to choose from" },
            new LevelReward { level = 15, kind = RewardKind.Item, item = ItemType.ColdCoffee, line = "Cold Coffee can drop" },
            new LevelReward { level = 16, kind = RewardKind.Style, style = "Gold Leaf", line = "the Gold Leaf style" },
            new LevelReward { level = 17, kind = RewardKind.Item, item = ItemType.RainBoot, line = "Rain Boot can drop" },
            new LevelReward { level = 18, kind = RewardKind.Item, item = ItemType.KiteString, line = "Kite String can drop" },
            new LevelReward { level = 19, kind = RewardKind.Item, item = ItemType.Umbrella, line = "Umbrella can drop" },
            new LevelReward { level = 20, kind = RewardKind.Item, item = ItemType.SaveState, line = "Save State can drop" },
            new LevelReward { level = 21, kind = RewardKind.Item, item = ItemType.BubbleWrap, line = "Bubble Wrap can drop" },
            new LevelReward { level = 22, kind = RewardKind.Item, item = ItemType.YoYo, line = "Yo-yo can drop" },
            new LevelReward { level = 23, kind = RewardKind.Item, item = ItemType.MagnifyingGlass, line = "Magnifying Glass can drop" },
        };

        public static int TopLevel => Ladder[Ladder.Length - 1].level;

        static readonly string[] Ranks =
        {
            "Work Experience", "Intern", "Lab Assistant", "Junior Technician", "Technician", "Senior Technician",
            "Research Assistant", "Researcher", "Senior Researcher", "Fellow", "Lecturer", "Principal Investigator",
            "Head of Department", "Professor", "Professor Emeritus", "Taller Than Finch",
        };

        /// <summary>The title that goes with a level. Past the last one it stays the last one.</summary>
        public static string Rank(int level) => Ranks[Mathf.Clamp(level, 1, Ranks.Length) - 1];

        static MetaSave Meta => SaveSystem.Data.meta;

        public static int Xp => Meta.xp;
        public static int Level => LevelOverride ?? LevelFor(Meta.xp);

        /// <summary>XP from one level to the next: quick at first, slower later.</summary>
        public static int ToNext(int level) => 60 + 20 * (Mathf.Max(1, level) - 1);

        /// <summary>Total XP needed to reach a level.</summary>
        public static int XpFor(int level)
        {
            int total = 0;
            for (int l = 1; l < level; l++) total += ToNext(l);
            return total;
        }

        public static int LevelFor(int xp)
        {
            int level = 1;
            while (xp >= XpFor(level + 1)) level++;
            return level;
        }

        /// <summary>XP gained since the run started: for the line the lab greets the player with.</summary>
        public static int RunXp { get; private set; }
        /// <summary>Well rested: XP still to be doubled, after a day or more away.</summary>
        public static int RestedLeft { get; set; }
        public const int RestedPool = 120, NewToolXp = 40, FirstWinXp = 50;
        /// <summary>Tests switch the little XP gifts off, so the XP they count is their own.</summary>
        public static bool BonusesOff;
        public static void StartRun() => RunXp = 0;

        /// <summary>Adds XP and hands out whatever the levels passed give.</summary>
        public static void Add(int amount)
        {
            if (amount <= 0) return;
            if (RestedLeft > 0)
            {
                int extra = Mathf.Min(amount, RestedLeft);
                RestedLeft -= extra;
                amount += extra;
            }
            int before = LevelFor(Meta.xp);
            Meta.xp += amount;
            RunXp += amount;
            Season.Add(amount);
            int after = LevelFor(Meta.xp);
            for (int level = before + 1; level <= after; level++)
            {
                LevelReward reward = At(level);
                if (reward == null) Meta.samples += SamplesPastTheTop;
                LevelledUp?.Invoke(level);
                var hud = UnityEngine.Object.FindAnyObjectByType<Hud>();
                if (hud != null) hud.Toast($"Level {level}: {Rank(level)}. {Says(level)}.", 4.5f);
                Sfx.Play(SfxId.Item);
            }
            if (after > before) SaveSystem.Save();
        }

        /// <summary>What a level gives, in words.</summary>
        public static string Says(int level)
        {
            LevelReward reward = At(level);
            return reward != null ? char.ToUpper(reward.line[0]) + reward.line.Substring(1) : $"{SamplesPastTheTop} samples";
        }

        public static LevelReward At(int level)
        {
            foreach (var reward in Ladder) if (reward.level == level) return reward;
            return null;
        }

        /// <summary>The next thing on the ladder, or null once it is all handed out.</summary>
        public static LevelReward Next
        {
            get
            {
                int level = Level;
                foreach (var reward in Ladder) if (reward.level > level) return reward;
                return null;
            }
        }

        /// <summary>One line for the lab: the level, the bar, and what is next.</summary>
        public static string Summary()
        {
            int level = Level;
            int into = Meta.xp - XpFor(level), need = ToNext(level);
            LevelReward next = Next;
            // Two short lines rather than one that wraps: the level and the bar, then what is next.
            string toward = next != null ? $"Next: {next.line}" : $"Next: {SamplesPastTheTop} samples";
            return $"Level {level} · {Rank(level)} · {Mathf.Clamp(into, 0, need)}/{need} XP\n{toward}";
        }

        // ---------- what the ladder holds back ----------

        /// <summary>False while the ladder still holds this item back.</summary>
        public static bool ItemUnlocked(ItemType type)
        {
            foreach (var reward in Ladder)
                if (reward.kind == RewardKind.Item && reward.item == type) return Level >= reward.level;
            return true;
        }

        /// <summary>The level that gives this style, or 0 if the ladder has nothing to do with it.</summary>
        public static int LevelForStyle(string style)
        {
            foreach (var reward in Ladder)
                if (reward.kind == RewardKind.Style && reward.style == style) return reward.level;
            return 0;
        }

        public static bool Has(Perk perk)
        {
            if (PerksOverride.HasValue) return PerksOverride.Value;
            foreach (var reward in Ladder)
                if (reward.kind == RewardKind.Perk && reward.perk == perk) return Level >= reward.level;
            return false;
        }

        /// <summary>A shop price after Bargain Hunter.</summary>
        public static int ShopPrice(int price) => Has(Perk.BargainHunter) && price > 1 ? Mathf.Max(1, Mathf.RoundToInt(price * 0.9f)) : price;
    }
}
