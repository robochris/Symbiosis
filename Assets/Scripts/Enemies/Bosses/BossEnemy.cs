using UnityEngine;

namespace Shrunk
{
    /// <summary>Shared by bosses: a name and phase for the health bar, and the telegraph colour for their big attacks.</summary>
    public abstract class BossEnemy : Enemy
    {
        /// <summary>The boss currently in play, if any. The HUD shows its health bar.</summary>
        public static BossEnemy InPlay { get; private set; }

        public string displayName = "Boss";
        [Tooltip("One line for its title card.")]
        public string subtitle = "";
        [Tooltip("Colour of the warning shapes for its big attacks.")]
        public Material telegraphMaterial;
        [Tooltip("Samples dropped when it dies.")]
        public int sampleReward = 10;

        public int Phase { get; protected set; } = 1;

        /// <summary>The player's ground velocity, smoothed: for attacks that land where the player is going, not where they were.</summary>
        protected Vector3 TargetVelocity { get; private set; }
        Vector3 _lastPlayerAt;
        bool _trackingPlayer;

        /// <summary>Where the player will be in this many seconds if they keep going.</summary>
        protected Vector3 Ahead(Vector3 targetPosition, float seconds)
        {
            Vector3 at = targetPosition + TargetVelocity * seconds;
            at.y = targetPosition.y;
            return at;
        }

        protected virtual void LateUpdate()
        {
            var player = PlayerController.Instance;
            if (player == null || Time.deltaTime <= 0f) return;
            Vector3 at = player.transform.position;
            if (!_trackingPlayer)
            {
                _trackingPlayer = true;
                _lastPlayerAt = at;
            }
            Vector3 velocity = (at - _lastPlayerAt) / Time.deltaTime;
            velocity.y = 0f;
            // A teleport or a gust is not running.
            if (velocity.magnitude > 20f) velocity = Vector3.zero;
            TargetVelocity = Vector3.Lerp(TargetVelocity, velocity, 0.15f);
            _lastPlayerAt = at;
        }
        public float Health01 => Health.maxHealth > 0 ? (float)Health.Current / Health.maxHealth : 0f;

        protected override void OnEnable()
        {
            base.OnEnable();
            InPlay = this;
            Phase = 1;
            _trackingPlayer = false;
            TargetVelocity = Vector3.zero;
            Sfx.Play(SfxId.BossAppear);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (InPlay == this) InPlay = null;
        }

        /// <summary>What the boss leaves behind, on top of the usual coins. Called when it dies.</summary>
        public virtual void DropRewards()
        {
            // A pile of samples, in a few lumps so it reads as a pile.
            int lumps = Mathf.Clamp(sampleReward / 5, 1, 6);
            for (int i = 0; i < lumps; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 2f;
                Pickup.Sample(transform.position + new Vector3(offset.x, 0f, offset.y), Mathf.Max(1, sampleReward / lumps));
            }

            // Every boss, big or small, leaves exactly one item. Ordinary enemies leave none.
            var assets = GameAssets.Instance;
            if (assets == null || assets.items == null) return;
            ItemDefinition item = RewardItem(assets.items);
            if (item == null) return;
            var first = Pickup.Item(item, transform.position + Vector3.forward * 2f, 0);
            // Second Opinion: a second one beside it. Taking either clears the other away, as in a treasure room.
            var floors = FloorManager.Instance;
            Room room = floors != null ? floors.CurrentRoom : null;
            if (!Progress.Has(Perk.SecondOpinion) || room == null) return;
            var picks = assets.items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), 2);
            ItemDefinition other = null;
            foreach (var pick in picks) if (pick != item && other == null) other = pick;
            if (other == null) return;
            first.treasureRoom = room;
            Pickup.Item(other, transform.position + Vector3.forward * 2f + Vector3.right * 2.2f, 0).treasureRoom = room;
        }

        /// <summary>
        /// Boss items: every world's boss keeps one of its own. The first time a boss is beaten it always leaves its
        /// item; from then on that item is loose in the boss pool, and any boss may leave any item in the pool.
        /// </summary>
        public static readonly System.Collections.Generic.Dictionary<string, ItemType> Trophies = new System.Collections.Generic.Dictionary<string, ItemType>
        {
            ["AntQueen"] = ItemType.QueensCommand,
            ["TrickCandle"] = ItemType.EverlastingWick,
            ["RobotVacuum"] = ItemType.DustBag,
            ["IceMaker"] = ItemType.FrostCore,
            ["TheLibrarian"] = ItemType.OverdueNotice,
            ["TheToad"] = ItemType.Croak,
            ["TheThundercloud"] = ItemType.BottledLightning,
            ["TheGlitch"] = ItemType.ExtraLife,
            ["TheGoldfish"] = ItemType.Fishbowl,
            ["TheJackInTheBox"] = ItemType.SpringHeel,
            ["TheRubberDuck"] = ItemType.SqueakyToy,
            ["TheSpider"] = ItemType.SilkThread,
            ["TheLawnmower"] = ItemType.GrassClippings,
            ["TheEncyclopedia"] = ItemType.Footnote,
            ["TheTap"] = ItemType.PlugChain,
        };
        /// <summary>How often a boss (any boss) leaves an item from the boss pool instead of a common one.</summary>
        public const float PoolChance = 0.3f;
        /// <summary>Kept for the tests: with this set, a boss leaves its own item at this chance and nothing from the pool.</summary>
        public static float? TrophyChanceOverride;
        /// <summary>The boss item the last drop was, the first time: for the screen to make something of it.</summary>
        public static ItemDefinition LastFirstDrop { get; private set; }

        /// <summary>Boss items set loose so far: every one whose boss has been beaten at least once.</summary>
        public static System.Collections.Generic.List<ItemType> Pool()
        {
            var pool = new System.Collections.Generic.List<ItemType>();
            var released = SaveSystem.Data.meta.bossItems;
            foreach (var pair in Trophies) if (released.Contains(pair.Key)) pool.Add(pair.Value);
            return pool;
        }

        /// <summary>Which item it leaves: its own the first time, sometimes one from the boss pool, otherwise a common one.</summary>
        protected virtual ItemDefinition RewardItem(ItemLibrary items)
        {
            string key = name.Replace("(Clone)", "").Trim();
            bool hasOwn = Trophies.TryGetValue(key, out ItemType trophy);
            if (TrophyChanceOverride.HasValue)
            {
                if (hasOwn && Random.value < TrophyChanceOverride.Value && items.Get(trophy) != null) return items.Get(trophy);
                return Common(items);
            }

            // The first time: its own, always, and from now on the pool has it.
            var released = SaveSystem.Data.meta.bossItems;
            if (hasOwn && !released.Contains(key) && items.Get(trophy) != null)
            {
                released.Add(key);
                SaveSystem.Save();
                LastFirstDrop = items.Get(trophy);
                var hud = FindAnyObjectByType<Hud>();
                if (hud != null) hud.Toast($"A boss item: the {LastFirstDrop.displayName}. From now on any boss might leave it.", 5f);
                return LastFirstDrop;
            }

            // After that: now and then, something from the pool, preferring what the player is not carrying yet.
            var pool = Pool();
            if (pool.Count > 0 && Random.value < PoolChance)
            {
                var player = PlayerController.Instance;
                var fresh = pool.FindAll(type => player == null || player.Items == null || player.Items.Count(type) == 0);
                var from = fresh.Count > 0 ? fresh : pool;
                var pick = items.Get(from[Random.Range(0, from.Count)]);
                if (pick != null) return pick;
            }
            return Common(items);
        }

        static ItemDefinition Common(ItemLibrary items)
        {
            var picks = items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), 1);
            return picks.Count > 0 ? picks[0] : null;
        }

        protected static bool Chance(float probability) => Random.value < probability;
    }
}
