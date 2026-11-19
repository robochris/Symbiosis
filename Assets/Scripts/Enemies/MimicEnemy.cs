using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Mimic: sits in a room looking like a small heap of coins. It is not. Walk up to collect it, or shoot it,
    /// and it gets up on its legs and comes at the player like a Chaser, with rather more health. Aim assist and
    /// the edge arrows ignore it while it is pretending. If it is the last thing left in a locked room it gives
    /// itself away after a moment, so a fight can never be stuck waiting on a heap of coins.
    /// </summary>
    public class MimicEnemy : ChaserEnemy
    {
        [Header("Pretending")]
        [Tooltip("Shown while it is pretending to be coins.")]
        public GameObject disguise;
        [Tooltip("Shown once it has got up.")]
        public GameObject body;
        [Tooltip("Gets up when the player is this close.")]
        public float wakeDistance = 3.2f;
        [Tooltip("Seconds it takes to get up: the player's chance to step back.")]
        public float wakeTime = 0.5f;
        [Tooltip("Left as the only enemy in the room, it gives itself away after this long.")]
        public float aloneTime = 2.5f;

        public bool Asleep { get; private set; }
        public bool Waking => _waking > 0f;

        float _waking, _alone;

        protected override void OnEnable()
        {
            base.OnEnable();
            Asleep = true;
            _waking = 0f;
            _alone = 0f;
            Targetable = false;
            Show();
            Health.Damaged += OnShot;
        }

        protected override void OnDisable()
        {
            Health.Damaged -= OnShot;
            base.OnDisable();
        }

        void Show()
        {
            if (disguise != null) disguise.SetActive(Asleep);
            if (body != null) body.SetActive(!Asleep);
        }

        // Shooting the heap of coins is one way to find out.
        void OnShot(int amount, Vector3 direction)
        {
            if (Asleep && !Waking) _waking = wakeTime;
        }

        protected override void Think()
        {
            if (!Asleep)
            {
                base.Think();
                return;
            }

            Move(Vector3.zero);

            if (Waking)
            {
                // The coins shake, and then they have legs.
                SetVisualScale(1f + 0.12f * Mathf.Sin(Time.time * 40f));
                _waking -= Time.deltaTime;
                if (_waking <= 0f) GetUp();
                return;
            }

            if (TryGetTarget(out Vector3 targetPosition, out _))
            {
                Vector3 to = targetPosition - transform.position;
                to.y = 0f;
                if (to.magnitude <= wakeDistance) _waking = wakeTime;
            }

            // Nothing else left to fight: it cannot keep the room shut by sitting still.
            bool company = false;
            foreach (var other in Active)
            {
                if (other == this || other.Health.IsDead || !other.gameObject.activeSelf) continue;
                if (other is MimicEnemy mimic && mimic.Asleep) continue;
                company = true;
                break;
            }
            _alone = company ? 0f : _alone + Time.deltaTime;
            if (_alone >= aloneTime) _waking = wakeTime;
        }

        void GetUp()
        {
            Asleep = false;
            Targetable = true;
            SetVisualScale(1f);
            Show();
            Fx.Burst(transform.position + Vector3.up * 0.5f, new Color(1f, 0.82f, 0.2f), 14, 5f, 0.16f, 0.4f);
            Sfx.Play(SfxId.BossAppear, 0.5f);
        }
    }
}
