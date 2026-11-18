using System;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The player's lab upgrades: what they own and what each one does to a run.
    /// The rule from the design doc: upgrades add options and safety, not raw power.
    /// </summary>
    public static class Upgrades
    {
        public static event Action<UpgradeDefinition> Purchased;

        public static int Level(UpgradeType type)
        {
            int[] levels = SaveSystem.Data.meta.upgradeLevels;
            int index = (int)type;
            return levels != null && index < levels.Length ? levels[index] : 0;
        }

        public static void SetLevel(UpgradeType type, int level)
        {
            var meta = SaveSystem.Data.meta;
            int count = Enum.GetValues(typeof(UpgradeType)).Length;
            if (meta.upgradeLevels == null || meta.upgradeLevels.Length < count) Array.Resize(ref meta.upgradeLevels, count);
            meta.upgradeLevels[(int)type] = level;
        }

        /// <summary>Samples for the next level, or -1 if it is already at its top level.</summary>
        public static int NextCost(UpgradeDefinition upgrade)
        {
            int level = Level(upgrade.type);
            return level < upgrade.MaxLevel ? upgrade.costs[level] : -1;
        }

        public static bool TryBuy(UpgradeDefinition upgrade)
        {
            int cost = NextCost(upgrade);
            var meta = SaveSystem.Data.meta;
            if (cost < 0 || meta.samples < cost) return false;

            meta.samples -= cost;
            SetLevel(upgrade.type, Level(upgrade.type) + 1);
            SaveSystem.Save();
            Purchased?.Invoke(upgrade);
            Sfx.Play(SfxId.Buy);
            return true;
        }

        // ---------- what each upgrade is worth ----------

        /// <summary>Thicker Shadow: +1 max health per level.</summary>
        public static int BonusHealth => Level(UpgradeType.ThickerShadow);

        /// <summary>Quick Reflexes: the dodge recharges 15% faster per level.</summary>
        public static float DodgeRechargeMultiplier => 1f + 0.15f * Level(UpgradeType.QuickReflexes);

        /// <summary>Sample Pouch: share of samples kept on death, 25% rising to 35% then 45%.</summary>
        public static float SamplesKeptOnDeath => 0.25f + 0.1f * Level(UpgradeType.SamplePouch);

        /// <summary>Overcharge Coil: the hero power meter fills 20% faster per level.</summary>
        public static float PowerFillMultiplier => 1f + 0.2f * Level(UpgradeType.OverchargeCoil);

        /// <summary>Field Kit: start every run with one random common item.</summary>
        public static bool StartsWithItem => Level(UpgradeType.FieldKit) > 0;

        /// <summary>Lucky Find: treasure rooms offer a choice of 2 items.</summary>
        public static int TreasureChoices => Level(UpgradeType.LuckyFind) > 0 ? 2 : 1;
    }
}
