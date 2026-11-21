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
        QueensJelly,
        StaticSock,
        BirthdayCandle,
        LostMarble,
        Sprinkles,
        FinchsDose,
        RubberBand,
        IceCube,
        FridgeMagnet,
        SpareBattery,
        Lunchbox,
        /// <summary>Hits harder, at the cost of a point of health for good.</summary>
        PaperHat,
        /// <summary>Everything drops one more coin.</summary>
        PiggyBank,
        /// <summary>Quicker on the feet and the trigger; the dodge takes longer to come back.</summary>
        ColdCoffee,
        /// <summary>The player's puddles splash up high enough to catch fliers.</summary>
        RainBoot,
        /// <summary>The first floor blast or puddle that would hurt the player in each room does not.</summary>
        Umbrella,
        /// <summary>Once a run, a hit that would finish the player is shrugged off.</summary>
        SaveState,
        /// <summary>The dodge goes much further.</summary>
        KiteString
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
        [Tooltip("One dry line shown after the description. For the laugh, not the rules.")]
        public string flavour;

        /// <summary>What it does, then its joke.</summary>
        public string Blurb => string.IsNullOrEmpty(flavour) ? description : description + "  " + flavour;
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
