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
        public float Health01 => Health.maxHealth > 0 ? (float)Health.Current / Health.maxHealth : 0f;

        protected override void OnEnable()
        {
            base.OnEnable();
            InPlay = this;
            Phase = 1;
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

        /// <summary>Which item it leaves: a common one, unless the boss has one of its own.</summary>
        protected virtual ItemDefinition RewardItem(ItemLibrary items)
        {
            var picks = items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), 1);
            return picks.Count > 0 ? picks[0] : null;
        }

        protected static bool Chance(float probability) => Random.value < probability;
    }
}
