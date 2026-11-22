using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Lawnmower, and (with the sweep switched off) the Hedge Trimmer before it.
    /// Sweep: a band across the room is marked, with a gap or two left in it; after a moment the blade runs along the
    /// band and hurts whoever is on it. Stand in a gap. Charge: a marked line, then a run along it.
    /// At two thirds and one third it speeds up and sends for friends.
    /// </summary>
    public class MowerBoss : BossEnemy
    {
        public enum Act { Idle, SweepWarning, ChargeWindup, Charge }

        [Header("Sweep")]
        public bool canSweep = true;
        public float sweepWarning = 1.4f;
        public float bandWidth = 3.6f;
        public float bandLength = 26f;
        [Tooltip("Gaps left in the band, in each phase (fewer later).")]
        public int[] gapsPerSweep = { 2, 2, 1 };
        public float gapSize = 3f;
        public float markRadius = 1.2f;

        [Header("Charge")]
        public float chargeWindup = 0.9f;
        public float chargeSpeed = 15f;
        public float chargeTime = 0.7f;
        public float chargeWidth = 2f;
        public float chargeReach = 1.8f;
        [Tooltip("Seconds ahead of a moving player the charge is aimed.")]
        public float chargeLead = 0.5f;

        [Header("Moving")]
        public float moveSpeed = 2.4f;
        public float turnSpeed = 240f;
        public float restBetween = 1.8f;

        [Header("Phases")]
        [Range(0f, 1f)] public float phase2At = 0.66f;
        [Range(0f, 1f)] public float phase3At = 0.33f;
        public float[] phaseSpeed = { 1f, 1.15f, 1.3f };
        public Enemy[] friends;
        public int friendsPerPhase = 2;

        public Act Current { get; private set; }
        public int Sweeps { get; private set; }
        public int Charges { get; private set; }

        float _timer;
        int _turn;
        bool _hit;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = Act.Idle;
            _timer = 1.5f;
            Sweeps = Charges = _turn = 0;
        }

        protected override void OnDisable()
        {
            HideChargeLine();
            base.OnDisable();
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                HideChargeLine();
                Move(Vector3.zero);
                return;
            }

            float health = Health01;
            if (Current == Act.Idle && ((Phase == 1 && health <= phase2At) || (Phase == 2 && health <= phase3At))) NextPhase();

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            _timer -= Time.deltaTime * (Current == Act.Charge ? 1f : SpeedScale);

            switch (Current)
            {
                case Act.SweepWarning:
                    Move(Vector3.zero);
                    if (_timer > 0f) break;
                    Rest();
                    break;

                case Act.ChargeWindup:
                    Move(Vector3.zero);
                    if (_timer > chargeWindup * 0.3f) Face(Ahead(targetPosition, chargeLead) - transform.position, turnSpeed * 1.5f);
                    ShowChargeLine(1f - Mathf.Clamp01(_timer / chargeWindup), chargeSpeed * chargeTime, chargeWidth, telegraphMaterial);
                    if (_timer > 0f) break;
                    HideChargeLine();
                    Current = Act.Charge;
                    _timer = chargeTime;
                    _hit = false;
                    Charges++;
                    break;

                case Act.Charge:
                    Move(transform.forward * chargeSpeed);
                    if (!_hit && targetHealth != null && toTarget.magnitude <= chargeReach) _hit = targetHealth.TakeDamage(1, transform.forward);
                    if (_timer <= 0f) Rest();
                    break;

                default:
                    Face(toTarget, turnSpeed);
                    Move(toTarget.magnitude > 7f ? (SteerToward(targetPosition) - transform.position).normalized * (moveSpeed * SpeedScale) : Vector3.zero);
                    if (_timer > 0f) break;
                    _turn++;
                    if (canSweep && _turn % 2 == 1) Sweep(targetPosition);
                    else
                    {
                        Current = Act.ChargeWindup;
                        _timer = chargeWindup;
                    }
                    break;
            }
        }

        // A band of marks across the room through the player, square to the way the mower faces, with gaps in it.
        void Sweep(Vector3 targetPosition)
        {
            Vector3 across = transform.right;
            across.y = 0f;
            across.Normalize();
            Vector3 forward = Vector3.Cross(across, Vector3.up);
            Vector3 centre = Ahead(targetPosition, sweepWarning * 0.3f);
            int gaps = gapsPerSweep != null && gapsPerSweep.Length >= Phase ? gapsPerSweep[Phase - 1] : 1;
            var gapAt = new float[gaps];
            for (int g = 0; g < gaps; g++) gapAt[g] = Random.Range(-bandLength * 0.4f, bandLength * 0.4f);
            for (float d = -bandLength * 0.5f; d <= bandLength * 0.5f; d += markRadius * 1.8f)
            {
                bool inGap = false;
                foreach (float g in gapAt) inGap |= Mathf.Abs(d - g) < gapSize * 0.5f;
                if (inGap) continue;
                // Two rows, so stepping off the band takes more than a step.
                for (int row = -1; row <= 1; row += 2)
                {
                    Vector3 at = centre + across * d + forward * (row * bandWidth * 0.25f);
                    GroundHazard.SpawnBlast(new Vector3(at.x, 0f, at.z), markRadius, sweepWarning, telegraphMaterial);
                }
            }
            LastGaps = gapAt;
            Sweeps++;
            Current = Act.SweepWarning;
            _timer = sweepWarning;
            Sfx.Play(SfxId.BossAppear, 0.5f);
        }

        /// <summary>Where the last sweep's gaps were, along the band from the player. For tests.</summary>
        public float[] LastGaps { get; private set; }

        void Rest()
        {
            Current = Act.Idle;
            _timer = restBetween;
        }

        void NextPhase()
        {
            Phase++;
            SpeedMultiplier = phaseSpeed != null && phaseSpeed.Length >= Phase ? phaseSpeed[Phase - 1] : 1f;
            Sfx.Play(SfxId.BossAppear);
            for (int i = 0; friends != null && friends.Length > 0 && i < friendsPerPhase; i++)
            {
                Enemy friend = friends[Random.Range(0, friends.Length)];
                float side = i % 2 == 0 ? -1f : 1f;
                if (friend != null) RequestMinion(friend, transform.position + transform.right * (side * 3.5f), false);
            }
        }
    }
}
