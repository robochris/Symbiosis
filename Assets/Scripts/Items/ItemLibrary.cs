using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>Every item in the game, in one list.</summary>
    [CreateAssetMenu(menuName = "Shrunk/Item Library", fileName = "ItemLibrary")]
    public class ItemLibrary : ScriptableObject
    {
        public ItemDefinition[] items;

        public ItemDefinition Get(ItemType type)
        {
            foreach (var item in items) if (item != null && item.type == type) return item;
            return null;
        }

        /// <summary>Random non-rare items the player has unlocked, no repeats.</summary>
        public List<ItemDefinition> RandomCommon(System.Random rng, int count)
        {
            var pool = new List<ItemDefinition>();
            foreach (var item in items) if (item != null && !item.rare && Achievements.ItemUnlocked(item.type)) pool.Add(item);

            var picked = new List<ItemDefinition>();
            while (picked.Count < count && pool.Count > 0)
            {
                int i = rng.Next(pool.Count);
                picked.Add(pool[i]);
                pool.RemoveAt(i);
            }
            return picked;
        }
    }
}
