using UnityEngine;

namespace Shrunk
{
    /// <summary>A biome as data: which rooms and enemies it uses. Adding a biome means making a new one of these.</summary>
    [CreateAssetMenu(menuName = "Shrunk/Biome", fileName = "NewBiome")]
    public class BiomeDefinition : ScriptableObject
    {
        public string displayName = "Biome";

        [Header("Rooms")]
        public Room startRoom;
        public Room exitRoom;
        public Room treasureRoom;
        public Room shopRoom;
        [Tooltip("Fight rooms, picked at random.")]
        public Room[] normalRooms;

        [Header("Enemies")]
        [Tooltip("Picked at random for each spawn.")]
        public Enemy[] enemies;
        [Tooltip("Enemies in a fight room on floor 1.")]
        public int baseEnemiesPerRoom = 2;
        [Tooltip("One more enemy per room every this many floors.")]
        public int floorsPerExtraEnemy = 3;

        public Room RoomFor(RoomType type, System.Random rng)
        {
            switch (type)
            {
                case RoomType.Start: return startRoom;
                case RoomType.Exit: return exitRoom;
                case RoomType.Treasure: return treasureRoom;
                case RoomType.Shop: return shopRoom;
                default: return normalRooms[rng.Next(normalRooms.Length)];
            }
        }
    }
}
