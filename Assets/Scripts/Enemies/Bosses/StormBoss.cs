using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Thundercloud, and (with the lightning switched off) the Kite before it. It drifts at a distance and does
    /// three things in turn. Lightning: a few strikes marked on the floor round the player, each landing a moment
    /// after its mark. Gust: a wide strip painted from it through the player, then a long shove along the strip, away
    /// from it; the answer is to step off the strip, or to dodge against it. Rain: slow puddles dropped about the
    /// floor that stay a while. At two thirds and one third it speeds up and sends for birds.
    /// </summary>
    public class StormBoss : BossEnemy
    {
        public enum Act { Drift, Lightning, GustWindup, Gust, Rain }

        [Header("Moving")]
        public float moveSpeed = 2.2f;
        public float turnSpeed = 200f;
        public float keepRange = 10f;
        public float hoverHeight = 1.2f;

        [Header("Lightning")]
        public bool canLightning = true;
        [Tooltip("Strikes in a volley, in each phase.")]
        public int[] strikesPerVolley = { 3, 4, 6 };
        public float strikeRadius = 1.6f;
        public float strikeFuse = 1.1f;
        public float strikeGap = 0.25f;
        [Tooltip("How far ahead of a moving player the lightning lands: 1 = exactly where they will be when it goes off.")]
        public float strikeLead = 0.8f;
        [Tooltip("The world's own boss (the Thundercloud), not the mini-boss before it. Its achievements are for this one.")]
        public bool isWorldBoss = true;
        [Tooltip("How far round the player the strikes are scattered.")]
        public float strikeSpread = 4f;

        [Header("Gust")]
        public float gustWindup = 1.3f;
        public float gustTime = 1.4f;
        public float gustWidth = 3.4f;
        public float gustRange = 18f;
        [Tooltip("How fast a gust carries the player along the strip.")]
        public float gustSpeed = 9f;

        [Header("Rain")]
        public int puddlesPerShower = 3;
        public Material rainMaterial;

        [Header("Rhythm")]
        public float restBetween = 1.6f;

        [Header("Phases")]
        [Range(0f, 1f)] public float phase2At = 0.66f;
        [Range(0f, 1f)] public float phase3At = 0.33f;
        public float[] phaseSpeed = { 1f, 1.15f, 1.3f };
        public Enemy[] friends;
        public int friendsPerPhase = 2;

        public Act Current { get; private set; }
        public int Strikes { get; private set; }
        public int Gusts { get; private set; }
        public int Showers { get; private set; }
        /// <summary>Times a gust actually caught the player.</summary>
        public int Blown { get; private set; }
        /// <summary>Times the player was hurt by lightning.</summary>
        public int Struck { get; private set; }

        float _timer;
        int _strikesLeft, _turn;
        Vector3 _gustDirection;
        Vector3 _visualRest;

        protected override void Awake()
        {
            base.Awake();
            if (visual != null) _visualRest = visual.localPosition;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = Act.Drift;
            _timer = 1.5f;
            Strikes = Gusts = Showers = Blown = Struck = 0;
            _turn = 0;
            GroundHazard.HitPlayer += OnBlast;
        }

        protected override void OnDisable()
        {
            GroundHazard.HitPlayer -= OnBlast;
            HideChargeLine();
            base.OnDisable();
        }

        // Counted for the achievement: a strike of this cloud's that hurt the player.
        void OnBlast(GroundHazard blast)
        {
            if (blast.source == this) Struck++;
        }

        protected override void Think()
        {
            if (visual != null) visual.localPosition = _visualRest + Vector3.up * (hoverHeight + Mathf.Sin(Time.time * 1.7f) * 0.25f);
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                HideChargeLine();
                Move(Vector3.zero);
                return;
            }

            float health = Health01;
            if ((Phase == 1 && health <= phase2At) || (Phase == 2 && health <= phase3At)) NextPhase();

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;

            switch (Current)
            {
                case Act.Lightning:
                    Drift(targetPosition, toTarget, 0.4f);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer > 0f) break;
                    if (_strikesLeft > 0)
                    {
                        Vector2 scatter = Random.insideUnitCircle * strikeSpread;
                        // The first goes where the player is heading, the second where they stand; the rest round about.
                        Vector3 ahead = Ahead(targetPosition, strikeFuse * strikeLead);
                        int done = Volley() - _strikesLeft;
                        Vector3 at = done == 0 ? ahead : done == 1 ? targetPosition : ahead + new Vector3(scatter.x, 0f, scatter.y);
                        var blast = GroundHazard.SpawnBlast(new Vector3(at.x, 0f, at.z), strikeRadius, strikeFuse, telegraphMaterial);
                        blast.source = this;
                        Strikes++;
                        _strikesLeft--;
                        _timer = strikeGap;
                        break;
                    }
                    Rest();
                    break;

                case Act.GustWindup:
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime * SpeedScale;
                    // It turns to the player for most of the wind-up, then holds the line it will blow along.
                    if (_timer > gustWindup * 0.3f) Face(toTarget, turnSpeed);
                    ShowChargeLine(1f - Mathf.Clamp01(_timer / gustWindup), gustRange, gustWidth, telegraphMaterial);
                    SetVisualScale(1f + 0.15f * (1f - Mathf.Clamp01(_timer / gustWindup)));
                    if (_timer > 0f) break;
                    _gustDirection = transform.forward;
                    _gustDirection.y = 0f;
                    _gustDirection.Normalize();
                    Current = Act.Gust;
                    _timer = gustTime;
                    Gusts++;
                    _caughtThisGust = false;
                    Sfx.Play(SfxId.Dodge, 0.5f);
                    break;

                case Act.Gust:
                    Move(Vector3.zero);
                    SetVisualScale(0.92f);
                    _timer -= Time.deltaTime * SpeedScale;
                    Blow(targetPosition, targetHealth);
                    if (_timer > 0f) break;
                    HideChargeLine();
                    SetVisualScale(1f);
                    Rest();
                    break;

                case Act.Rain:
                    Move(Vector3.zero);
                    for (int i = 0; i < puddlesPerShower + Phase - 1; i++)
                    {
                        Vector2 scatter = Random.insideUnitCircle * 7f;
                        GroundHazard.SpawnStickyPatch(targetPosition + new Vector3(scatter.x, 0f, scatter.y), rainMaterial);
                    }
                    Showers++;
                    Rest();
                    break;

                default:
                    Drift(targetPosition, toTarget, 1f);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer <= 0f) NextMove();
                    break;
            }
        }

        bool _caughtThisGust;

        // While the gust blows, anyone standing on the strip is carried along it, away from the cloud.
        void Blow(Vector3 targetPosition, Health targetHealth)
        {
            var player = targetHealth != null ? targetHealth.GetComponent<PlayerController>() : null;
            if (player == null || player.IsDodging) return;
            Vector3 local = transform.InverseTransformPoint(targetPosition);
            bool onStrip = local.z > 0f && local.z <= gustRange && Mathf.Abs(local.x) <= gustWidth * 0.5f + 0.4f;
            if (!onStrip) return;
            if (!_caughtThisGust)
            {
                _caughtThisGust = true;
                Blown++;
            }
            player.Push(_gustDirection * gustSpeed);
        }

        int Volley() => strikesPerVolley != null && strikesPerVolley.Length >= Phase ? strikesPerVolley[Phase - 1] : 3;

        void NextMove()
        {
            // Lightning, gust, rain, in turn; without lightning, gust and rain.
            _turn++;
            int pick = canLightning ? _turn % 3 : 1 + _turn % 2;
            if (pick == 0)
            {
                Current = Act.Lightning;
                _strikesLeft = Volley();
                _timer = 0f;
            }
            else if (pick == 1)
            {
                Current = Act.GustWindup;
                _timer = gustWindup;
            }
            else
            {
                Current = Act.Rain;
            }
        }

        void Rest()
        {
            Current = Act.Drift;
            _timer = restBetween;
        }

        void Drift(Vector3 targetPosition, Vector3 toTarget, float share)
        {
            float distance = toTarget.magnitude;
            Vector3 heading;
            if (!CanSee(targetPosition))
            {
                heading = SteerToward(targetPosition) - transform.position;
                heading.y = 0f;
            }
            else
            {
                Vector3 radial = distance > 0.01f ? toTarget / distance : transform.forward;
                Vector3 tangent = Quaternion.Euler(0f, 90f, 0f) * radial;
                heading = tangent * 0.5f + radial * Mathf.Clamp((distance - keepRange) / 3f, -1f, 1f);
            }
            Face(toTarget, turnSpeed);
            Move(heading.sqrMagnitude < 0.01f ? Vector3.zero : heading.normalized * (moveSpeed * share * SpeedScale));
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
