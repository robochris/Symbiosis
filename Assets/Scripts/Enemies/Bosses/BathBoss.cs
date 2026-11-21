using UnityEngine;
using UnityEngine.AI;

namespace Shrunk
{
    /// <summary>
    /// The Rubber Duck, and (with the squeak switched off and the slide switched on) the Bar of Soap before it.
    /// Duck: a squeak sends rings out across the floor, one after another; dodge through the edge. Then it bounces,
    /// coming down on a marked circle near the player.
    /// Soap: slides along a marked line, leaving suds that slow, two or three times, then rests.
    /// At two thirds and one third both speed up and send for friends.
    /// </summary>
    public class BathBoss : BossEnemy
    {
        public enum Act { Float, Squeak, Bounce, SlideWindup, Slide }

        [Header("Squeak")]
        public bool canSqueak = true;
        [Tooltip("Rings in a squeak, in each phase.")]
        public int[] ringsPerSqueak = { 1, 1, 2 };
        public float ringGap = 0.7f;
        public float ringSpeed = 6.5f;
        public float ringReach = 18f;
        public float squeakWarning = 0.7f;
        public Material ringMaterial;

        [Header("Bounce")]
        public float bounceTime = 0.8f;
        public float bounceRange = 8f;
        public float landingRadius = 2f;
        public float bounceHeight = 2.2f;

        [Header("Slide")]
        public bool canSlide;
        public int slidesInARow = 2;
        public float slideWindup = 0.8f;
        public float slideSpeed = 14f;
        public float slideTime = 0.7f;
        public float slideWidth = 1.8f;
        public float slideReach = 1.6f;
        public Material sudsMaterial;

        [Header("Moving")]
        public float moveSpeed = 2f;
        public float turnSpeed = 260f;
        public float restBetween = 2.2f;

        [Header("Phases")]
        [Range(0f, 1f)] public float phase2At = 0.66f;
        [Range(0f, 1f)] public float phase3At = 0.33f;
        public float[] phaseSpeed = { 1f, 1.15f, 1.3f };
        public Enemy[] friends;
        public int friendsPerPhase = 2;

        public Act Current { get; private set; }
        public int Squeaks { get; private set; }
        public int Rings { get; private set; }
        public int Bounces { get; private set; }
        public int Slides { get; private set; }
        /// <summary>Times one of its rings caught the player.</summary>
        public int RingHits { get; private set; }

        float _timer, _bounceClock;
        int _ringsLeft, _slidesLeft, _turn;
        bool _hit;
        Vector3 _from, _to, _visualRest;

        protected override void Awake()
        {
            base.Awake();
            if (visual != null) _visualRest = visual.localPosition;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = Act.Float;
            _timer = 1.5f;
            Squeaks = Rings = Bounces = Slides = RingHits = _turn = 0;
            RingWave.Caught += OnRingCaught;
        }

        void OnRingCaught(RingWave ring)
        {
            if (ring.source == this) RingHits++;
        }

        protected override void OnDisable()
        {
            RingWave.Caught -= OnRingCaught;
            HideChargeLine();
            if (visual != null) visual.localPosition = _visualRest;
            base.OnDisable();
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                HideChargeLine();
                if (Current != Act.Bounce) Move(Vector3.zero);
                return;
            }

            float health = Health01;
            if (Current == Act.Float && ((Phase == 1 && health <= phase2At) || (Phase == 2 && health <= phase3At))) NextPhase();

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            _timer -= Time.deltaTime * (Current == Act.Slide ? 1f : SpeedScale);

            switch (Current)
            {
                case Act.Squeak:
                    Move(Vector3.zero);
                    // Puffs up before each ring.
                    SetVisualScale(1f + 0.15f * Mathf.Clamp01(1f - _timer / squeakWarning));
                    if (_timer > 0f) break;
                    SetVisualScale(1f);
                    RingWave.Spawn(transform.position, ringSpeed, ringReach, ringMaterial, this);
                    Rings++;
                    Sfx.Play(SfxId.BossAppear, 0.4f);
                    if (--_ringsLeft > 0)
                    {
                        _timer = ringGap;
                        break;
                    }
                    StartBounce(targetPosition);
                    break;

                case Act.Bounce:
                    _bounceClock += Time.deltaTime * SpeedScale;
                    float t = Mathf.Clamp01(_bounceClock / bounceTime);
                    Vector3 step = (_to - _from) * (Time.deltaTime * SpeedScale / bounceTime);
                    step.y = -0.2f;
                    Controller.Move(step);
                    Knockback = Vector3.zero;
                    if (visual != null) visual.localPosition = _visualRest + Vector3.up * (Mathf.Sin(t * Mathf.PI) * bounceHeight);
                    if (t < 1f) break;
                    if (visual != null) visual.localPosition = _visualRest;
                    Bounces++;
                    Rest();
                    break;

                case Act.SlideWindup:
                    Move(Vector3.zero);
                    if (_timer > slideWindup * 0.3f) Face(toTarget, turnSpeed * 1.5f);
                    ShowChargeLine(1f - Mathf.Clamp01(_timer / slideWindup), slideSpeed * slideTime, slideWidth, telegraphMaterial);
                    if (_timer > 0f) break;
                    HideChargeLine();
                    Current = Act.Slide;
                    _timer = slideTime;
                    _hit = false;
                    Slides++;
                    _sudsAt = 0f;
                    break;

                case Act.Slide:
                    Move(transform.forward * slideSpeed);
                    if (!_hit && targetHealth != null && toTarget.magnitude <= slideReach) _hit = targetHealth.TakeDamage(1, transform.forward);
                    // Suds behind it as it goes.
                    _sudsAt -= Time.deltaTime;
                    if (_sudsAt <= 0f)
                    {
                        _sudsAt = 0.15f;
                        GroundHazard.SpawnStickyPatch(transform.position, sudsMaterial);
                    }
                    if (_timer > 0f) break;
                    if (--_slidesLeft > 0)
                    {
                        Current = Act.SlideWindup;
                        _timer = slideWindup;
                    }
                    else Rest();
                    break;

                default:
                    Face(toTarget, turnSpeed);
                    Move(toTarget.magnitude > 7f ? (SteerToward(targetPosition) - transform.position).normalized * (moveSpeed * SpeedScale) : Vector3.zero);
                    if (_timer > 0f) break;
                    NextMove(targetPosition);
                    break;
            }
        }

        float _sudsAt;

        void NextMove(Vector3 targetPosition)
        {
            _turn++;
            if (canSlide && (!canSqueak || _turn % 2 == 0))
            {
                _slidesLeft = slidesInARow + Phase - 1;
                Current = Act.SlideWindup;
                _timer = slideWindup;
                return;
            }
            if (canSqueak)
            {
                _ringsLeft = ringsPerSqueak != null && ringsPerSqueak.Length >= Phase ? ringsPerSqueak[Phase - 1] : 1;
                Current = Act.Squeak;
                _timer = squeakWarning;
                Squeaks++;
                return;
            }
            StartBounce(targetPosition);
        }

        void StartBounce(Vector3 targetPosition)
        {
            Vector3 offset = targetPosition - transform.position;
            offset.y = 0f;
            if (offset.magnitude > bounceRange) offset = offset.normalized * bounceRange;
            Vector3 landing = transform.position + offset;
            if (NavMesh.SamplePosition(landing, out var hit, 2f, NavMesh.AllAreas)) landing = hit.position;
            landing.y = transform.position.y;
            _from = transform.position;
            _to = landing;
            _bounceClock = 0f;
            Current = Act.Bounce;
            GroundHazard.SpawnBlast(new Vector3(landing.x, 0f, landing.z), landingRadius, bounceTime / SpeedScale, telegraphMaterial);
        }

        void Rest()
        {
            Current = Act.Float;
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
