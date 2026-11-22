using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>Two items that are better together, with a name for it and a little extra on top.</summary>
    public class Synergy
    {
        public string name;
        public ItemType a, b;
        public string does;
    }

    /// <summary>
    /// Synergies: certain pairs of items, carried together, give a little more than the two of them. The first time a
    /// pair comes together in any run, a card names it; after that it just works. PlayerItems adds the extra.
    /// </summary>
    public static class Synergies
    {
        public static readonly Synergy[] All =
        {
            new Synergy { name = "Packed Lunch Money", a = ItemType.PiggyBank, b = ItemType.Lunchbox, does = "every coin drop is one more again" },
            new Synergy { name = "Steam", a = ItemType.IceCube, b = ItemType.FireflyLantern, does = "shots hit one harder" },
            new Synergy { name = "Elastic", a = ItemType.RubberBand, b = ItemType.YoYo, does = "shots bounce off one more wall" },
            new Synergy { name = "Eagle Eye", a = ItemType.CompoundEye, b = ItemType.MagnifyingGlass, does = "one more shot in ten hits for double" },
            new Synergy { name = "Pogo", a = ItemType.SpringHeel, b = ItemType.KiteString, does = "the dodge goes further still" },
            new Synergy { name = "Party Popper", a = ItemType.BubbleWrap, b = ItemType.SqueakyToy, does = "the pops are bigger" },
            new Synergy { name = "Acid Rain", a = ItemType.AcidGland, b = ItemType.SplitterMandible, does = "the acid is stronger" },
            new Synergy { name = "Sugar Rush", a = ItemType.ColdCoffee, b = ItemType.Sprinkles, does = "you move a tenth faster" },
            new Synergy { name = "Static Cling", a = ItemType.StaticSock, b = ItemType.SpareBattery, does = "charged shots come one shot sooner" },
            new Synergy { name = "Royal Guard", a = ItemType.QueensCommand, b = ItemType.ChitinPlate, does = "shots go through one more" },
        };

        /// <summary>Whether this pair is active for the carried items.</summary>
        public static bool Active(Synergy synergy, PlayerItems items) => items != null && items.Count(synergy.a) > 0 && items.Count(synergy.b) > 0;

        public static bool Has(string name, PlayerItems items)
        {
            foreach (var synergy in All) if (synergy.name == name) return Active(synergy, items);
            return false;
        }

        /// <summary>Called whenever an item is added: a pair that has just come together for the first time ever gets its card.</summary>
        public static Synergy Check(PlayerItems items, ItemType added)
        {
            foreach (var synergy in All)
            {
                if (synergy.a != added && synergy.b != added) continue;
                if (!Active(synergy, items)) continue;
                var seen = SaveSystem.Data.meta.synergiesFound;
                bool first = !seen.Contains(synergy.name);
                if (first) seen.Add(synergy.name);
                if (ScreenCards.Instance != null && !ScreenCards.Instance.CardShowing)
                    ScreenCards.Instance.ShowCard("SYNERGY: " + synergy.name.ToUpperInvariant(), $"Together, they are more: {synergy.does}." + (first ? " (A new one for the notebook.)" : ""), 3f);
                Sfx.Play(SfxId.PowerOn, 0.6f);
                return synergy;
            }
            return null;
        }
    }
}
