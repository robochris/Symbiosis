using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Floor 5 mini boss. A giant ant that crawls after the player and keeps laying larvae, which grow into Workers
    /// if they are left alone. Below half health she burrows and comes up under the player.
    /// The egg sac on her back takes double damage and she turns slowly, so getting behind her is the skill being taught;
    /// her tail slam is what stops the player camping there.
    /// </summary>
    public class BroodMother : BossEnemy
    {
        public enum State { Crawl, BiteWindup, Burrowed }

        [Header("Crawl")]
        public float crawlSpeed = 2.2f;
        [Tooltip("She scuttles when the player is far off, so a big arena can't be used to keep away from her for ever.")]
        public float scuttleSpeed = 5f;
        [Tooltip("Crawl speed up to the first distance, scuttle speed from the second, blended in between.")]
        public float scuttleFrom = 7f;
        public float scuttleFull = 14f;
        [Tooltip("Degrees per second. Slow on purpose, so the player can get round behind her.")]
        public float turnSpeed = 65f;

        [Header("Bite (front)")]
        public float biteRange = 3.4f;
        public float biteWindup = 0.6f;
        public float biteInterval = 2.2f;
        public int biteDamage = 1;

        [Header("Larvae")]
        public LarvaEnemy larvaPrefab;
        public float layInterval = 8f;
        public int larvaePerLay = 3;

        [Header("Tail slam (behind)")]
        [Tooltip("Slams when the player is behind her and this close.")]
        public float tailRange = 5.5f;
        public float tailOffset = 3.6f;
        public float tailRadius = 2.8f;
        public float tailFuse = 0.8f;
        public float tailCooldown = 3f;

        [Header("Burrow (below half health)")]
        public float burrowInterval = 9f;
        [Tooltip("Seconds the dust ring shows before she comes up.")]
        public float burrowWarning = 1.2f;
        public float burrowRadius = 3f;
        public int ringLarvae = 5;
        public float ringRadius = 4f;

        [Header("Weak point")]
        [Tooltip("Damage multiplier for hits to the egg sac, from behind.")]
        public int weakPointMultiplier = 2;

        public State Current { get; private set; }
        public int LarvaeLaid { get; private set; }
        public int Burrows { get; private set; }
        public int TailSlams { get; private set; }

        float _timer;
        float _nextLay;
        float _nextBite;
        float _nextTail;
        float _nextBurrow;
        Vector3 _emergeAt;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = State.Crawl;
            Targetable = true;
            _nextLay = Time.time + layInterval * 0.5f;
            _nextBite = _nextTail = 0f;
            _nextBurrow = Time.time + 2f;
            LarvaeLaid = Burrows = TailSlams = 0;
        }

        // A shot travelling the same way she faces came from behind: it hit the egg sac.
        int WeakPoint(Vector3 direction)
        {
            direction.y = 0f;
            return Vector3.Dot(direction.normalized, transform.forward) > 0.3f ? weakPointMultiplier : 1;
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                Move(Vector3.zero);
                return;
            }

            if (Health.DamageScaler == null) Health.DamageScaler = WeakPoint;
            Phase = Health.Current * 2 <= Health.maxHealth ? 2 : 1;

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            float facing = distance > 0.01f ? Vector3.Dot(toTarget / distance, transform.forward) : 1f;

            switch (Current)
            {
                case State.Burrowed:
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f) Emerge();
                    return;

                case State.BiteWindup:
                    SetVisualScale(Mathf.Lerp(1.15f, 1f, _timer / biteWindup));
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer <= 0f)
                    {
                        SetVisualScale(1f);
                        if (distance <= biteRange + ReachBonus(targetHealth) + 0.5f && facing > 0.3f) targetHealth.TakeDamage(biteDamage, transform.forward);
                        _nextBite = Time.time + biteInterval;
                        Current = State.Crawl;
                    }
                    return;
            }

            // Crawl after the player.
            Face(SteerToward(targetPosition) - transform.position, turnSpeed * SpeedScale);
            Move(transform.forward * (SpeedAt(distance) * SpeedScale));

            if (Phase == 2 && Time.time >= _nextBurrow)
            {
                Burrow(targetPosition);
                return;
            }

            if (Phase == 1 && Time.time >= _nextLay)
            {
                _nextLay = Time.time + layInterval;
                // Laid behind her, in a small fan.
                for (int i = 0; i < larvaePerLay; i++)
                {
                    float angle = 180f + (i - (larvaePerLay - 1) * 0.5f) * 35f;
                    Lay(transform.position + Quaternion.Euler(0f, angle, 0f) * transform.forward * 4f);
                }
            }

            if (facing < -0.35f && distance <= tailRange && Time.time >= _nextTail)
            {
                _nextTail = Time.time + tailCooldown;
                TailSlams++;
                GroundHazard.SpawnBlast(transform.position - transform.forward * tailOffset, tailRadius, tailFuse, telegraphMaterial);
            }

            if (facing > 0.5f && distance <= biteRange + ReachBonus(targetHealth) && Time.time >= _nextBite)
            {
                Current = State.BiteWindup;
                _timer = biteWindup;
            }
        }

        /// <summary>How fast she moves with the player this far away.</summary>
        public float SpeedAt(float distance) => Mathf.Lerp(crawlSpeed, scuttleSpeed, Mathf.InverseLerp(scuttleFrom, scuttleFull, distance));

        void Lay(Vector3 position)
        {
            if (larvaPrefab == null) return;
            LarvaeLaid++;
            RequestMinion(larvaPrefab, position, false);
        }

        // Goes under: can't be hit, and a dust ring shows where she will come up.
        void Burrow(Vector3 under)
        {
            Burrows++;
            Current = State.Burrowed;
            _timer = burrowWarning;
            _emergeAt = new Vector3(under.x, transform.position.y, under.z);
            Targetable = false;
            Controller.enabled = false;
            if (visual != null) visual.gameObject.SetActive(false);
            GroundHazard.SpawnBlast(_emergeAt, burrowRadius, burrowWarning, telegraphMaterial);
        }

        void Emerge()
        {
            transform.position = _emergeAt;
            Controller.enabled = true;
            if (visual != null) visual.gameObject.SetActive(true);
            Targetable = true;
            Current = State.Crawl;
            _nextBurrow = Time.time + burrowInterval;

            for (int i = 0; i < ringLarvae; i++)
            {
                Lay(transform.position + Quaternion.Euler(0f, 360f * i / ringLarvae, 0f) * Vector3.forward * ringRadius);
            }
        }

        public override void DropRewards()
        {
            base.DropRewards();

            // The doc: the Brood Mother drops one item.
            var assets = GameAssets.Instance;
            if (assets == null || assets.items == null) return;
            var picks = assets.items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), 1);
            if (picks.Count > 0) Pickup.Item(picks[0], transform.position, 0);
        }
    }
}
