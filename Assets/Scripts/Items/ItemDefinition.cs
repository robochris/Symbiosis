using UnityEngine;

namespace Shrunk
{
    public enum ItemType
    {
        NeedleTip,
        SplitterMandible,
        AcidGland,
        FireflyLantern,
        CompoundEye,
        ExtraLegs,
        WingBuds,
        ChitinPlate,
        SugarCube,
        PheromoneCloud,
        LarvaBuddy,
        QueensJelly
    }

    /// <summary>Where an item shows on the character. Two items in one slot: the newer one shows.</summary>
    public enum ItemSlot { Head, Eyes, Back, Hands, Aura, Body, Companion }

    /// <summary>An item as data: what it is called, where it shows, what it costs in the shop.</summary>
    [CreateAssetMenu(menuName = "Shrunk/Item", fileName = "NewItem")]
    public class ItemDefinition : ScriptableObject
    {
        public ItemType type;
        public string displayName = "Item";
        [TextArea] public string description;
        public ItemSlot slot;
        [Tooltip("Rare items never appear in treasure rooms or the shop; they come from bosses.")]
        public bool rare;
        [Tooltip("Coins in the shop.")]
        public int price = 20;
        [Tooltip("Its look, built as an attachment so it works on every character style. Positioned relative to the middle of the body.")]
        public GameObject visual;
        [Tooltip("Colour of the pickup orb.")]
        public Material material;
    }
}
