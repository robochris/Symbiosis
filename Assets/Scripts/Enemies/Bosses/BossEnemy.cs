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
        }

        protected static bool Chance(float probability) => Random.value < probability;
    }
}
