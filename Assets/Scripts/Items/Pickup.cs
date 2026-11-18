using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    public enum PickupKind { Coin, Sample, Item }

    /// <summary>
    /// Something lying on the floor to collect. Coins and samples fly to the player when they get close.
    /// Items are taken by walking onto them; in the shop that also costs coins.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        public static readonly List<Pickup> All = new List<Pickup>();
        /// <summary>An item was collected (free or bought).</summary>
        public static event Action<ItemDefinition> ItemCollected;

        public PickupKind kind;
        public int amount = 1;
        public ItemDefinition item;
        [Tooltip("Coins needed to take it. 0 = free.")]
        public int price;
        /// <summary>Set on treasure-room items: taking one removes the others offered in that room.</summary>
        [System.NonSerialized] public Room treasureRoom;

        const float MagnetRange = 4.5f;
        const float MagnetSpeed = 13f;

        float _age;
        float _spin;

        public static Pickup Coin(Vector3 position, int amount) =>
            Create(PickupKind.Coin, PrimitiveType.Cylinder, "Coin", position + Vector3.up * 0.3f, new Vector3(0.4f, 0.04f, 0.4f),
                GameAssets.Instance != null ? GameAssets.Instance.coin : null, amount);

        public static Pickup Sample(Vector3 position, int amount) =>
            Create(PickupKind.Sample, PrimitiveType.Cube, "Sample", position + Vector3.up * 0.3f, Vector3.one * 0.3f,
                GameAssets.Instance != null ? GameAssets.Instance.sample : null, amount);

        public static Pickup Item(ItemDefinition item, Vector3 position, int price)
        {
            var pickup = Create(PickupKind.Item, PrimitiveType.Sphere, item.displayName + " (item)", position + Vector3.up * 0.9f, Vector3.one * 0.6f, item.material, 1);
            pickup.item = item;
            pickup.price = price;
            return pickup;
        }

        static Pickup Create(PickupKind kind, PrimitiveType shape, string name, Vector3 position, Vector3 scale, Material material, int amount)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            go.transform.position = position;
            go.transform.localScale = scale;
            if (kind == PickupKind.Coin) go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;

            var pickup = go.AddComponent<Pickup>();
            pickup.kind = kind;
            pickup.amount = amount;
            return pickup;
        }

        public static void ClearAll()
        {
            foreach (var pickup in All.ToArray()) if (pickup != null) Destroy(pickup.gameObject);
            All.Clear();
        }

        /// <summary>The closest item pickup within reach of being read, for the HUD to describe.</summary>
        public static Pickup NearestItem(Vector3 position, float range)
        {
            Pickup best = null;
            float bestDistance = range * range;
            foreach (var pickup in All)
            {
                if (pickup.kind != PickupKind.Item) continue;
                Vector3 to = pickup.transform.position - position;
                to.y = 0f;
                if (to.sqrMagnitude < bestDistance)
                {
                    bestDistance = to.sqrMagnitude;
                    best = pickup;
                }
            }
            return best;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            _age += Time.deltaTime;
            _spin += 120f * Time.deltaTime;
            if (kind != PickupKind.Coin) transform.rotation = Quaternion.Euler(0f, _spin, 0f);
            else transform.rotation = Quaternion.Euler(90f, _spin, 0f);

            var player = PlayerController.Instance;
            if (player == null || player.Health.IsDead) return;

            Vector3 to = player.transform.position - transform.position;
            to.y = 0f;
            float distance = to.magnitude;

            if (kind == PickupKind.Item)
            {
                // Free items sit on a pedestal, so reach is generous; shop items need a deliberate step onto them.
                float reach = price > 0 ? 0.9f : 1.6f;
                if (distance > reach) return;
                if (price > 0 && !RunState.TrySpendCoins(price)) return;

                var items = player.GetComponent<PlayerItems>();
                if (items != null) items.Add(item);
                ItemCollected?.Invoke(item);
                if (treasureRoom != null)
                {
                    foreach (var other in All.ToArray())
                    {
                        if (other != this && other.treasureRoom == treasureRoom) Destroy(other.gameObject);
                    }
                }
                Destroy(gameObject);
                return;
            }

            // A short delay so drops are seen landing before they fly in.
            if (_age > 0.35f && distance <= MagnetRange)
            {
                transform.position += to.normalized * Mathf.Min(MagnetSpeed * Time.deltaTime, distance);
            }
            if (distance <= 0.7f)
            {
                if (kind == PickupKind.Coin) RunState.AddCoins(amount);
                else RunState.AddSamples(amount);
                Destroy(gameObject);
            }
        }
    }

    /// <summary>What enemies leave behind. Tougher and more mutated enemies leave more.</summary>
    public static class Loot
    {
        /// <summary>Chance of an item from an unmutated enemy; each mutation point adds this much again.</summary>
        public const float ItemChance = 0.02f;

        public static void Drop(Enemy enemy)
        {
            Vector3 origin = enemy.transform.position;
            int points = enemy.Mutations != null ? enemy.Mutations.TotalCost : 0;

            int coins = 1 + enemy.Health.maxHealth / 4;
            for (int i = 0; i < coins; i++) Pickup.Coin(origin + Scatter(), 1);

            Pickup.Sample(origin + Scatter(), 1 + points);

            var assets = GameAssets.Instance;
            if (assets != null && assets.items != null && UnityEngine.Random.value < ItemChance * (1 + points))
            {
                var picks = assets.items.RandomCommon(new System.Random(UnityEngine.Random.Range(0, int.MaxValue)), 1);
                if (picks.Count > 0) Pickup.Item(picks[0], origin, 0);
            }
        }

        static Vector3 Scatter()
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.9f;
            return new Vector3(offset.x, 0f, offset.y);
        }
    }
}
