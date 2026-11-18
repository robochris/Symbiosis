using UnityEngine;

namespace Shrunk
{
    public enum UpgradeType
    {
        ThickerShadow,
        QuickReflexes,
        SamplePouch,
        OverchargeCoil,
        FieldKit,
        LuckyFind
    }

    /// <summary>A lab upgrade as data: what it does and what each level costs in samples.</summary>
    [CreateAssetMenu(menuName = "Shrunk/Upgrade", fileName = "NewUpgrade")]
    public class UpgradeDefinition : ScriptableObject
    {
        public UpgradeType type;
        public string displayName = "Upgrade";
        [TextArea] public string description;
        [Tooltip("Samples for level 1, level 2, and so on. The number of entries is the number of levels.")]
        public int[] costs = { 100 };

        public int MaxLevel => costs.Length;
    }
}
