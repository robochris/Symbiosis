using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Support: hurts nobody itself while it has company. It hangs back among the others and, every few seconds,
    /// swells and sends out a pulse that speeds up every enemy near it for a while. Kill it first, or fight a faster
    /// room. Left as the last one standing it panics and comes at the player, biting.
    /// </summary>
    public class SupportEnemy : Enemy
    {
        [Header("Positioning")]
        public float moveSpeed = 3.4f;
        public float turnSpeed = 360f;
        [Tooltip("Backs away when the player is closer than this.")]
        public float keepAway = 9f;
        [Tooltip("Drifts back to the others when it is further from them than this.")]
        public float leash = 6f;

        [Header("Pulse")]
        public float pulseInterval = 5f;
        [Tooltip("Swells for this long before each pulse: the telegraph, and the time to shoot it.")]
        public float windupTime = 0.7f;
        public float pulseRadius = 9f;
        [Tooltip("Seconds the speed lasts on whoever the pulse reaches.")]
        public float buffSeconds = 3f;
        public Color pulseColor = new Color(1f, 0.5f, 0.8f);

        [Header("Alone")]
        public float panicSpeed = 4.6f;
        public float biteRange = 1.7f;
        public float biteWindup = 0.45f;
        public float biteCooldown = 1.2f;

        public int Pulses { get; private set; }
        /// <summary>How many the last pulse reached.</summary>
        public int LastReached { get; private set; }
        public bool Alone { get; private set; }

        float _nextPulse, _windup, _nextBite, _biteWindup;
        bool _winding, _biting;

        protected override void OnEnable()
        {
            base.OnEnable();
            _winding = _biting = false;
            _nextPulse = Time.time + 2f;
            Pulses = 0;
            LastReached = 0;
            Alone = false;
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                SetVisualScale(1f);
                Move(Vector3.zero);
                return;
            }

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            // Where the others are, on average. Eggs and the like are not company.
            Vector3 sum = Vector3.zero;
            int company = 0;
            foreach (var other in Active)
            {
                if (other == this || other.Health.IsDead || other is EggEnemy || other is SupportEnemy) continue;
                sum += other.transform.position;
                company++;
            }
            Alone = company == 0;

            if (Alone)
            {
                Panic(targetPosition, toTarget, distance, targetHealth);
                return;
            }
            _biting = false;

            if (_winding)
            {
                SetVisualScale(Mathf.Lerp(1.4f, 1f, _windup / windupTime));
                Move(Vector3.zero);
                _windup -= Time.deltaTime * SpeedScale;
                if (_windup <= 0f) Pulse();
                return;
            }

            SetVisualScale(1f);
            Vector3 toOthers = sum / company - transform.position;
            toOthers.y = 0f;
            Vector3 heading = Vector3.zero;
            if (distance < keepAway) heading -= toTarget.normalized;
            if (toOthers.magnitude > leash) heading += toOthers.normalized * 0.8f;

            if (heading.sqrMagnitude > 0.01f)
            {
                Face(heading, turnSpeed);
                Move(heading.normalized * (moveSpeed * SpeedScale));
            }
            else
            {
                Face(toTarget, turnSpeed);
                Move(Vector3.zero);
            }

            if (Time.time >= _nextPulse)
            {
                _winding = true;
                _windup = windupTime;
            }
        }

        void Pulse()
        {
            _winding = false;
            _nextPulse = Time.time + pulseInterval / SpeedScale;
            Pulses++;
            LastReached = 0;
            Fx.Burst(transform.position + Vector3.up, pulseColor, 26, 10f, 0.22f, 0.5f);

            foreach (var other in Active)
            {
                if (other == this || other.Health.IsDead || other is EggEnemy) continue;
                if ((other.transform.position - transform.position).sqrMagnitude > pulseRadius * pulseRadius) continue;
                other.Buff(buffSeconds);
                LastReached++;
                Fx.Burst(other.transform.position + Vector3.up, pulseColor, 6, 3f, 0.16f, 0.4f);
            }
        }

        // Nobody left to speed up: it comes at the player itself.
        void Panic(Vector3 targetPosition, Vector3 toTarget, float distance, Health targetHealth)
        {
            _winding = false;
            if (_biting)
            {
                Face(toTarget, turnSpeed);
                SetVisualScale(Mathf.Lerp(1.3f, 1f, _biteWindup / biteWindup));
                Move(Vector3.zero);
                _biteWindup -= Time.deltaTime * SpeedScale;
                if (_biteWindup <= 0f)
                {
                    SetVisualScale(1f);
                    _biting = false;
                    _nextBite = Time.time + biteCooldown;
                    if (distance <= biteRange + 0.6f + ReachBonus(targetHealth)) targetHealth.TakeDamage(1, transform.forward);
                }
                return;
            }

            SetVisualScale(1f);
            Face(SteerToward(targetPosition) - transform.position, turnSpeed);
            Move(distance > biteRange * 0.8f ? transform.forward * (panicSpeed * SpeedScale) : Vector3.zero);
            if (distance <= biteRange + ReachBonus(targetHealth) && Time.time >= _nextBite)
            {
                _biting = true;
                _biteWindup = biteWindup;
            }
        }
    }
}
