using UnityEngine;

namespace Shrunk
{
    /// <summary>Shared assets that code needs to reach at runtime: the item list and the materials for effects and pickups. One per scene, on the Game object.</summary>
    public class GameAssets : MonoBehaviour
    {
        public static GameAssets Instance { get; private set; }

        public ItemLibrary items;
        public LarvaBuddy larvaPrefab;

        [Header("Effect materials")]
        public Material playerAcid;
        public Material fire;
        public Material confused;

        [Header("Pickup materials")]
        public Material coin;
        public Material sample;
        public Material note;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
