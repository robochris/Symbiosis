using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Birthday Cake's boss. It fights like the Ant Queen (hot wax in a fan, jawbreaker guards, sparkler boxes,
    /// charges that leave wax behind), with one trick of its own: the first time it is nearly out, the flame goes out,
    /// it stands helpless for a moment, and then it relights itself with a burst of fire and a good share of its health back.
    /// The moment it is out is the best chance to hurt it; being next to it when it relights is the worst place to be.
    /// </summary>
    public class TrickCandle : AntQueen
    {
        [Header("The trick")]
        [Range(0f, 1f), Tooltip("It goes out the first time its health drops to this share.")]
        public float goesOutAt = 0.35f;
        [Tooltip("Seconds it stands unlit before it relights.")]
        public float outTime = 2.4f;
        [Range(0f, 1f), Tooltip("Relighting brings its health back up to this share.")]
        public float relightsTo = 0.6f;
        [Tooltip("The fire that bursts out round it when it relights.")]
        public float relightRadius = 6.5f;
        public Transform flame;

        /// <summary>True from the moment it has gone out once. It only has the one trick.</summary>
        public bool TrickUsed { get; private set; }
        public bool IsOut => _out > 0f;

        float _out;

        protected override void OnEnable()
        {
            base.OnEnable();
            TrickUsed = false;
            _out = 0f;
            if (flame != null) flame.gameObject.SetActive(true);
        }

        protected override void Think()
        {
            if (_out > 0f)
            {
                Move(Vector3.zero);
                _out -= Time.deltaTime;
                if (_out <= 0f)
                {
                    if (flame != null) flame.gameObject.SetActive(true);
                    int back = Mathf.RoundToInt(Health.maxHealth * relightsTo) - Health.Current;
                    if (back > 0) Health.Heal(back);
                    Fx.Burst(transform.position + Vector3.up * 3f, new Color(1f, 0.6f, 0.15f), 40, 9f, 0.3f, 0.6f);
                    Sfx.Play(SfxId.BossAppear);
                }
                return;
            }

            if (!TrickUsed && !Health.IsDead && Health01 <= goesOutAt)
            {
                TrickUsed = true;
                _out = outTime;
                if (flame != null) flame.gameObject.SetActive(false);
                HideChargeLine();
                SetVisualScale(1f);
                // The warning for the relight: a disc that fills in round it for exactly as long as it is out.
                GroundHazard.SpawnBlast(transform.position, relightRadius, outTime, telegraphMaterial);
                return;
            }

            base.Think();
        }
    }
}
