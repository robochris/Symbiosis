using UnityEngine;

namespace Shrunk
{
    /// <summary>A biome as data: how its rooms look and which enemies live there. Adding a biome means making a new one of these.</summary>
    [CreateAssetMenu(menuName = "Shrunk/Biome", fileName = "NewBiome")]
    public class BiomeDefinition : ScriptableObject
    {
        public string displayName = "Biome";

        [Header("Room look")]
        public Material floorMaterial;
        public Material wallMaterial;
        public Material doorMaterial;
        public float wallHeight = 3f;

        [Header("Special room props (placed in the middle of the room)")]
        public GameObject startProp;
        [Tooltip("The way down. Hidden until the exit room is cleared.")]
        public GameObject exitProp;
        public GameObject treasureProp;
        public GameObject shopProp;

        [Header("Enemies")]
        [Tooltip("Picked at random for each spawn.")]
        public Enemy[] enemies;
        [Tooltip("Enemies in a fight room on floor 1.")]
        public int baseEnemiesPerRoom = 2;
        [Tooltip("One more enemy per room every this many floors.")]
        public int floorsPerExtraEnemy = 3;

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
    }
}
