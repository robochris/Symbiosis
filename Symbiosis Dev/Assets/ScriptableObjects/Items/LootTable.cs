using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyLootTable", menuName = "Loot/EnemyLootTable")]
public class LootTable : ScriptableObject
{
    public LootItem[] possibleDrops;

    // Returns a list of all items that should drop
    public List<GameObject> GetDrops()
    {
        List<GameObject> drops = new List<GameObject>();

        foreach (var lootItem in possibleDrops)
        {
            if (lootItem.guaranteedDrop)
            {
                // Always drop
                drops.Add(lootItem.itemPrefab);
            }
            else
            {
                // Chance-based drop
                float roll = Random.value;
                if (roll <= lootItem.dropChance)
                {
                    drops.Add(lootItem.itemPrefab);
                }
            }
        }

        return drops;
    }
}

[System.Serializable]
public class LootItem
{
    public GameObject itemPrefab;      // Prefab with ItemPickup script attached
    [Range(0f, 1f)] public float dropChance;
    public bool guaranteedDrop;
}
