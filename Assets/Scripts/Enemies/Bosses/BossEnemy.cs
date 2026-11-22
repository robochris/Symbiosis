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

        /// <summary>The rare items some bosses keep, by the boss's name.</summary>
        public static readonly System.Collections.Generic.Dictionary<string, ItemType> Trophies = new System.Collections.Generic.Dictionary<string, ItemType>
        {
            ["TheToad"] = ItemType.Croak,
            ["TheThundercloud"] = ItemType.BottledLightning,
            ["TheGlitch"] = ItemType.ExtraLife,
            ["TheJackInTheBox"] = ItemType.SpringHeel,
        };
        /// <summary>How often a boss with a trophy leaves it instead of a common item.</summary>
        public const float TrophyChance = 0.35f;
        /// <summary>Tests set this to make the trophy certain (1) or impossible (0).</summary>
        public static float? TrophyChanceOverride;

        /// <summary>Which item it leaves: a common one, unless the boss has one of its own.</summary>
        protected virtual ItemDefinition RewardItem(ItemLibrary items)
        {
            string key = name.Replace("(Clone)", "").Trim();
            if (Trophies.TryGetValue(key, out ItemType trophy) && Random.value < (TrophyChanceOverride ?? TrophyChance))
            {
                var kept = items.Get(trophy);
                if (kept != null) return kept;
            }
            var picks = items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), 1);
            return picks.Count > 0 ? picks[0] : null;
        }

        protected static bool Chance(float probability) => Random.value < probability;
    }
}
