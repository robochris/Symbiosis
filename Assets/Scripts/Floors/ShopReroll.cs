using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The bell on the shop's counter: step on the mat beside it and, for a few coins, the beetle clears the shelf
    /// and puts out three different things. It costs more every time it is rung on the same floor. Made by the
    /// floor as it is built.
    /// </summary>
    public class ShopReroll : MonoBehaviour
    {
        public const int FirstPrice = 5, PriceStep = 5;

        public Room room;
        public Vector3[] spots;
        public int price = FirstPrice;
        /// <summary>On a closing-down sale floor, what the bell puts out is half price too.</summary>
        public bool sale;
        public StepPad pad = new StepPad { radius = 0.9f };

        public int Rerolls { get; private set; }

        public static ShopReroll Create(Room room, Vector3 position, Vector3[] spots, Material matMaterial, Material bellMaterial)
        {
            var go = new GameObject("Shop Bell");
            go.transform.SetParent(room.transform, false);
            go.transform.position = position;
            Part(PrimitiveType.Cylinder, "Mat", go.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.6f, 0.02f, 1.6f), matMaterial);
            Part(PrimitiveType.Sphere, "Bell", go.transform, new Vector3(0f, 0.55f, 1.3f), new Vector3(0.55f, 0.4f, 0.55f), bellMaterial);
            Part(PrimitiveType.Cylinder, "Stand", go.transform, new Vector3(0f, 0.18f, 1.3f), new Vector3(0.7f, 0.18f, 0.7f), matMaterial);

            var reroll = go.AddComponent<ShopReroll>();
            reroll.room = room;
            reroll.spots = spots;
            // Regular Customer: the first ring on a floor is on the house.
            if (Upgrades.FreeBell) reroll.price = 0;
            return reroll;
        }

        static void Part(PrimitiveType shape, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
        }

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || !pad.SteppedOn(transform.position, player.transform.position)) return;
            TryReroll();
        }

        /// <summary>Rings the bell: if the coins are there, the shelf is cleared and restocked. Returns whether it was.</summary>
        public bool TryReroll()
        {
            var assets = GameAssets.Instance;
            if (assets == null || assets.items == null || spots == null) return false;
            if (!RunState.TrySpendCoins(price)) return false;

            // What is still on the shelf goes back under the counter.
            var had = new HashSet<ItemType>();
            foreach (var pickup in Pickup.All.ToArray())
            {
                if (pickup.kind != PickupKind.Item || pickup.price <= 0 || !room.ContainsInside(pickup.transform.position, 0f)) continue;
                had.Add(pickup.item.type);
                Destroy(pickup.gameObject);
            }

            // Three different things, and where it can be managed, not the three that were just there.
            var rng = new System.Random(Random.Range(0, int.MaxValue));
            List<ItemDefinition> picks = null;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                picks = assets.items.RandomCommon(rng, spots.Length);
                bool same = picks.Count > 0;
                foreach (var pick in picks) same &= had.Contains(pick.type);
                if (!same) break;
            }
            for (int i = 0; picks != null && i < picks.Count && i < spots.Length; i++)
            {
                int price = Progress.ShopPrice(picks[i].price);
                Pickup.Item(picks[i], room.Centre + spots[i], sale ? FloorTwists.Discount(price) : price);
            }

            Rerolls++;
            price = price <= 0 ? FirstPrice : price + PriceStep;
            Sfx.Play(SfxId.Item);
            return true;
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 2.4f) return null;
            return price <= 0
                ? "The bell: three different things on the shelf, and this time it is on the house. Step on the mat to ring it."
                : $"The bell: {price} coins for three different things on the shelf (you have {RunState.Coins}). Step on the mat to ring it.";
        }
    }
}
