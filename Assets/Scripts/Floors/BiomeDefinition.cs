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
        [Tooltip("Short name with no spaces, written into saved runs.")]
        public string id = "biome";
        [Tooltip("A second storey: raised platforms with ramps in the corners of fight rooms.")]
        public bool ledges;
        [Tooltip("During fights the floor floods every so often; only ledges stay dry (see Flood).")]
        public bool floods;
        public Material ledgeTopMaterial;
        [Tooltip("Shown on the portal that leads here.")]
        public string mutationTheme = "";
        [Tooltip("The bottom: nothing lives here but its boss, and after the boss there is only the way home.")]
        public bool isBottom;

        [Header("Light and sky")]
        [Tooltip("What shows where there is nothing: the dark of the earth underground, the sky outdoors.")]
        public Color skyColor = new Color(0.08f, 0.06f, 0.05f);
        public Color ambientColor = new Color(0.95f, 0.88f, 0.82f);
        public float sunIntensity = 0.9f;
        public bool fog = true;
        public Color fogColor = new Color(0.08f, 0.06f, 0.05f);
        public float fogStart = 22f;
        public float fogEnd = 90f;
        [Tooltip("Off: walls throw no shadows across the floor, which would hide half of a tall-walled room.")]
        public bool roomCastsShadows = false;

        [Header("Room look")]
        public Material floorMaterial;
        public Material wallMaterial;
        public Material doorMaterial;
        public float wallHeight = 6f;
        [Tooltip("On for underground biomes: a roof over every room, so the camera never sees over the walls.")]
        public bool ceiling = true;
        public Material ceilingMaterial;
        [Tooltip("The pebbles that lie about fight rooms as cover. Empty = no cover.")]
        public Material coverMaterial;

        [Header("Room features (an empty material switches that feature's look off)")]
        [Tooltip("Food store: giant crumbs, and one sugar cube.")]
        public Material crumbMaterial;
        public Material sugarMaterial;
        [Tooltip("Root hall: roots from floor to ceiling.")]
        public Material rootMaterial;
        [Tooltip("Nursery: larva pods.")]
        public Material podMaterial;
        [Tooltip("What a live pod hatches into.")]
        public LarvaEnemy podHatchling;
        [Tooltip("Dew room: the wet patches and the droplets.")]
        public Material dewMaterial;

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
        [Tooltip("The portal that leads INTO this biome from another one's portal room. Empty = the plain deeper portal.")]
        public GameObject portalProp;

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
        [Tooltip("Bigger rooms hold more: one more enemy for every this many open tiles past the first lot.")]
        public int tilesPerExtraEnemy = 60;
        public int maxExtraEnemiesForSize = 4;

        /// <summary>Enemies in a fight room, before the dice add one or not.</summary>
        public int EnemiesFor(int floorInBiome, int openTiles)
        {
            int count = baseEnemiesPerRoom + (floorInBiome - 1) / Mathf.Max(1, floorsPerExtraEnemy);
            if (tilesPerExtraEnemy > 0) count += Mathf.Clamp(Mathf.RoundToInt((float)openTiles / tilesPerExtraEnemy) - 1, 0, maxExtraEnemiesForSize);
            return count;
        }

        [Tooltip("Glowing mushrooms: the caps (unlit, and the colour of their light) and the stems. Empty = no lamps.")]
        public Material glowMaterial;
        public Material glowStemMaterial;
        public float glowLightRange = 11f;
        public float glowLightIntensity = 4f;

        [Tooltip("What an elite's crown is made of.")]
        public Material eliteMaterial;

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
