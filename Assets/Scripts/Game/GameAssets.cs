using UnityEngine;

namespace Shrunk
{
    /// <summary>Shared assets that code needs to reach at runtime: the item list and the materials for effects and pickups. One per scene, on the Game object.</summary>
    public class GameAssets : MonoBehaviour
    {
        public static GameAssets Instance { get; private set; }

        public ItemLibrary items;
        public LarvaBuddy larvaPrefab;
        /// <summary>The rival Void (see RivalVoid).</summary>
        public Enemy rivalPrefab;

        [Header("Effect materials")]
        public Material playerAcid;
        public Material fire;
        public Material confused;
        public Material chilled;
        [Tooltip("See-through: what the camera draws things with while they are in its way.")]
        public Material faded;

        [Header("Pickup materials")]
        public Material coin;
        public Material sample;
        public Material note;
        [Tooltip("What a crumb of health is made of.")]
        public Material health;

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
