using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>One line of a biome's enemy list.</summary>
    [Serializable]
    public class EnemySpawnEntry
    {
        public Enemy prefab;
        [Tooltip("How often it is picked compared to the others.")]
        public int weight = 1;
        [Tooltip("First floor of the biome it can appear on.")]
        public int minFloor = 1;
    }

    /// <summary>A biome as data: how its rooms look, which enemies live there and which mutations they can roll. Adding a biome means making a new one of these.</summary>
    [CreateAssetMenu(menuName = "Shrunk/Biome", fileName = "NewBiome")]
    public class BiomeDefinition : ScriptableObject
    {
        public string displayName = "Biome";

        [Header("Room look")]
        public Material floorMaterial;
        public Material wallMaterial;
        public Material doorMaterial;
        public float wallHeight = 6f;
        [Tooltip("On for underground biomes: a roof over every room, so the camera never sees over the walls.")]
        public bool ceiling = true;
        public Material ceilingMaterial;

        [Header("Special room props (placed in the middle of the room)")]
        public GameObject startProp;
        [Tooltip("The way down. Hidden until the exit room is cleared.")]
        public GameObject exitProp;
        public GameObject treasureProp;
        public GameObject shopProp;
        [Tooltip("After the biome boss: the portal home.")]
        public GameObject extractPortalProp;
        [Tooltip("After the biome boss: the portal to the next biome, one depth deeper.")]
        public GameObject deeperPortalProp;

        [Header("Enemies")]
        public EnemySpawnEntry[] spawnTable;
        [Tooltip("Floor 5.")]
        public BossEnemy miniBoss;
        [Tooltip("Floor 10.")]
        public BossEnemy boss;
        [Tooltip("What a Swarm Call brings in.")]
        public Enemy swarmEnemy;
        [Tooltip("Enemies in a fight room on floor 1.")]
        public int baseEnemiesPerRoom = 2;
        [Tooltip("One more enemy per room every this many floors.")]
        public int floorsPerExtraEnemy = 3;

        [Header("Mutations")]
        [Tooltip("What enemies here can roll: the universal ones plus this biome's own.")]
        public MutationDefinition[] mutations;

        public GameObject PropFor(RoomType type)
        {
            switch (type)
            {
                case RoomType.Start: return startProp;
                case RoomType.Exit: return exitProp;
                case RoomType.Treasure: return treasureProp;
                case RoomType.Shop: return shopProp;
                default: return null;
            }
        }

        /// <summary>A random enemy from the ones allowed on this floor, by weight.</summary>
        public Enemy PickEnemy(int floor, System.Random rng)
        {
            int total = 0;
            foreach (var entry in spawnTable) if (entry.prefab != null && floor >= entry.minFloor) total += Mathf.Max(0, entry.weight);
            if (total <= 0) return swarmEnemy;

            int roll = rng.Next(total);
            foreach (var entry in spawnTable)
            {
                if (entry.prefab == null || floor < entry.minFloor) continue;
                roll -= Mathf.Max(0, entry.weight);
                if (roll < 0) return entry.prefab;
            }
            return swarmEnemy;
        }

        public Enemy EnemyNamed(string prefabName)
        {
            foreach (var entry in spawnTable) if (entry.prefab != null && entry.prefab.name == prefabName) return entry.prefab;
            return null;
        }

        public MutationDefinition Mutation(MutationType type)
        {
            foreach (var mutation in mutations) if (mutation != null && mutation.type == type) return mutation;
            return null;
        }
    }
}
