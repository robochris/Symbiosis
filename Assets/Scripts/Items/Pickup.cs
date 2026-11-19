using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    public enum PickupKind { Coin, Sample, Item, Health }

    /// <summary>
    /// Something lying on the floor to collect. Coins and samples fly to the player when they get close.
    /// Items are taken by walking onto them; in the shop that also costs coins. A crumb gives a point of health
    /// back, and is left where it is by anybody who is not hurt.
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

        /// <summary>A crumb: one point of health for whoever needs it. In the shop it costs coins.</summary>
        public static Pickup Health(Vector3 position, int price = 0)
        {
            var pickup = Create(PickupKind.Health, PrimitiveType.Sphere, "Crumb (health)", position + Vector3.up * 0.5f, new Vector3(0.5f, 0.38f, 0.45f),
                GameAssets.Instance != null ? GameAssets.Instance.health : null, 1);
            pickup.price = price;
            return pickup;
        }

        public const int CrumbPrice = 8;
        /// <summary>A health point was picked up (free or bought).</summary>
        public static event Action HealthCollected;

        string CrumbPrompt(Vector3 playerPosition)
        {
            Vector3 to = playerPosition - transform.position;
            to.y = 0f;
            if (to.magnitude > 2.6f) return null;
            var player = PlayerController.Instance;
            bool hurt = player != null && player.Health.Current < player.Health.maxHealth;
            if (!hurt) return "A crumb. You are not hungry enough.";
            return price > 0 ? $"A crumb: one health back. {price} coins (you have {RunState.Coins})." : "A crumb: one health back.";
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

        void OnEnable()
        {
            All.Add(this);
            WorldPrompt.Add(Prompt, transform);
        }

        void OnDisable()
        {
            All.Remove(this);
            WorldPrompt.Remove(Prompt);
        }

        string Prompt(Vector3 playerPosition) => kind == PickupKind.Health ? CrumbPrompt(playerPosition) : null;

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

            if (kind == PickupKind.Health)
            {
                // Only for the hurt: anybody else walks over it and it stays for later.
                if (distance > 0.9f || player.Health.Current >= player.Health.maxHealth) return;
                if (price > 0 && !RunState.TrySpendCoins(price)) return;
                player.Health.Heal(amount);
                HealthCollected?.Invoke();
                Sfx.Play(SfxId.Item);
                Fx.Burst(transform.position, new Color(1f, 0.5f, 0.55f), 10, 4f, 0.16f, 0.4f);
                Destroy(gameObject);
                return;
            }

            if (kind == PickupKind.Item)
            {
                // Free items sit on a pedestal, so reach is generous; shop items need a deliberate step onto them.
                float reach = price > 0 ? 0.9f : 1.6f;
                if (distance > reach) return;
                if (price > 0 && !RunState.TrySpendCoins(price)) return;

                var items = player.GetComponent<PlayerItems>();
                if (items != null) items.Add(item);
                ItemCollected?.Invoke(item);
                Sfx.Play(SfxId.Item);
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
            var carried = player.Items;
            float pull = MagnetRange * (carried != null ? carried.Stats.magnetMultiplier : 1f);
            if (_age > 0.35f && distance <= pull)
            {
                transform.position += to.normalized * Mathf.Min(MagnetSpeed * Time.deltaTime, distance);
            }
            if (distance <= 0.7f)
            {
                if (kind == PickupKind.Coin) RunState.AddCoins(amount);
                else RunState.AddSamples(amount);
                Sfx.Play(SfxId.Coin, 0.6f);
                Destroy(gameObject);
            }
        }
    }

    /// <summary>What enemies leave behind. Tougher and more mutated enemies leave more.</summary>
    public static class Loot
    {
        /// <summary>Chance of an item from an unmutated enemy; each mutation point adds this much again.</summary>
        public const float ItemChance = 0.02f;
        /// <summary>Samples from an unmutated enemy; each mutation point adds one more.</summary>
        public const int SamplesPerEnemy = 2;
        public const int EliteSamples = 5;
        public const float EliteItemChance = 0.3f;
        /// <summary>Chance an enemy leaves a crumb when the player is hurt. Never when they are not.</summary>
        public const float CrumbChance = 0.05f;
        /// <summary>Tests set this to make the roll certain (1) or impossible (0).</summary>
        public static float? CrumbChanceOverride;

        public static void Drop(Enemy enemy)
        {
            if (!enemy.dropsLoot) return;
            Vector3 origin = enemy.transform.position;
            int points = enemy.Mutations != null ? enemy.Mutations.TotalCost : 0;

            if (enemy is BossEnemy boss) boss.DropRewards();
            int coins = Mathf.Min(8, 1 + enemy.Health.maxHealth / 4);
            for (int i = 0; i < coins; i++) Pickup.Coin(origin + Scatter(), 1);

            Pickup.Sample(origin + Scatter(), SamplesPerEnemy + points);

            // Something to eat: always from a boss, now and then from anything else if the player is hurt.
            var eater = PlayerController.Instance;
            bool hurt = eater != null && eater.Health.Current < eater.Health.maxHealth;
            float luck = eater != null && eater.Items != null ? eater.Items.Stats.crumbMultiplier : 1f;
            if (enemy is BossEnemy || (hurt && UnityEngine.Random.value < (CrumbChanceOverride ?? (enemy.IsElite ? CrumbChance * 4f : CrumbChance) * luck)))
            {
                Pickup.Health(origin + Scatter());
            }
            // An elite is worth hunting: a second lump of samples, and a real chance of an item.
            if (enemy.IsElite) Pickup.Sample(origin + Scatter(), EliteSamples);

            var assets = GameAssets.Instance;
            // Bosses have their own item rewards.
            if (!(enemy is BossEnemy) && assets != null && assets.items != null && UnityEngine.Random.value < (enemy.IsElite ? EliteItemChance : ItemChance * (1 + points)))
            {
                ItemDefinition item = PickFor(enemy, assets.items, new System.Random(UnityEngine.Random.Range(0, int.MaxValue)));
                if (item != null) Pickup.Item(item, origin, 0);
            }
        }

        /// <summary>How often a dropped item is one of the enemy's favoured ones rather than anything at all.</summary>
        public const float FavouredShare = 0.65f;

        /// <summary>Which item this enemy would drop: usually one of its own, otherwise any common one.</summary>
        public static ItemDefinition PickFor(Enemy enemy, ItemLibrary library, System.Random rng)
        {
            if (enemy.favouredDrops != null && enemy.favouredDrops.Length > 0 && rng.NextDouble() < FavouredShare)
            {
                var favoured = library.Get(enemy.favouredDrops[rng.Next(enemy.favouredDrops.Length)]);
                if (favoured != null && Achievements.ItemUnlocked(favoured.type)) return favoured;
            }
            var picks = library.RandomCommon(rng, 1);
            return picks.Count > 0 ? picks[0] : null;
        }

        static Vector3 Scatter()
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.9f;
            return new Vector3(offset.x, 0f, offset.y);
        }
    }
}
