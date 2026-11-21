using UnityEngine;
using UnityEngine.AI;

namespace Shrunk
{
    /// <summary>
    /// The Toad, and (with the tongue switched off) the Froglet before it. It sits, then hops: each landing is marked
    /// on the floor as a circle the moment it jumps, and it comes down where the player was heading. After a run of
    /// hops it opens its mouth and paints a strip toward the player, follows for most of the wind-up, holds, and
    /// lashes its tongue along it. Caught, and the player is hit and dragged toward it. The answer is to step off the
    /// strip when it stops turning, and to shoot it while it sits. At two thirds and one third it hops more and sends
    /// for the flies.
    /// </summary>
    public class ToadBoss : BossEnemy
    {
        public enum State { Sit, Hop, Aim, Rest }

        [Header("Hops")]
        public float sitTime = 1.6f;
        [Tooltip("Hops in a run, in each phase.")]
        public int[] hopsPerRun = { 2, 3, 4 };
        public float hopTime = 0.75f;
        public float hopGap = 0.35f;
        [Tooltip("The furthest one hop goes.")]
        public float hopRange = 9f;
        public float hopHeight = 2.5f;
        public float landingRadius = 2.3f;
        public float turnSpeed = 260f;

        [Header("Tongue")]
        public bool canTongue = true;
        public float tongueAim = 1.2f;
        [Tooltip("For this long at the end of the aim it has stopped turning: the time to step aside.")]
        public float tongueHold = 0.4f;
        public float tongueRange = 13f;
        public float tongueWidth = 1.1f;
        [Tooltip("How fast a catch reels the player in, and for how long.")]
        public float tonguePull = 16f;
        public float pullTime = 0.28f;
        public Material tongueMaterial;

        [Header("Phases")]
        [Range(0f, 1f)] public float phase2At = 0.66f;
        [Range(0f, 1f)] public float phase3At = 0.33f;
        public float[] phaseSpeed = { 1f, 1.15f, 1.3f };
        public Enemy[] friends;
        public int friendsPerPhase = 2;

        public State Current { get; private set; }
        public int Hops { get; private set; }
        public int Lashes { get; private set; }
        /// <summary>Times the tongue caught the player.</summary>
        public int Caught { get; private set; }
        public bool Airborne => Current == State.Hop;

        float _timer;
        int _hopsLeft;
        Vector3 _hopFrom, _hopTo;
        Vector3 _lastTargetPosition, _targetVelocity;
        Vector3 _visualRest;
        GameObject _tongue;
        float _tongueUntil;
        PlayerController _reeling;
        float _reelUntil;

        protected override void Awake()
        {
            base.Awake();
            if (visual != null) _visualRest = visual.localPosition;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = State.Sit;
            _timer = 1.2f;
            Hops = Lashes = Caught = 0;
            _reeling = null;
            _hopsLeft = 0;
            _pendingHop = false;
            Targetable = true;
        }

        protected override void OnDisable()
        {
            HideChargeLine();
            if (_tongue != null) _tongue.SetActive(false);
            if (visual != null) visual.localPosition = _visualRest;
            base.OnDisable();
        }

        protected override void Think()
        {
            if (_tongue != null && _tongue.activeSelf && Time.time >= _tongueUntil) _tongue.SetActive(false);
            Reel();

            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                HideChargeLine();
                if (Current != State.Hop) Move(Vector3.zero);
                return;
            }

            if (Time.deltaTime > 0f)
            {
                Vector3 velocity = (targetPosition - _lastTargetPosition) / Time.deltaTime;
                velocity.y = 0f;
                if (velocity.sqrMagnitude > 400f) velocity = Vector3.zero; // a teleport, not a run
                _targetVelocity = Vector3.Lerp(_targetVelocity, velocity, 0.15f);
            }
            _lastTargetPosition = targetPosition;

            float health = Health01;
            if (Current != State.Hop && ((Phase == 1 && health <= phase2At) || (Phase == 2 && health <= phase3At))) NextPhase();

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;

            switch (Current)
            {
                case State.Sit:
                    Move(Vector3.zero);
                    Face(toTarget, turnSpeed);
                    SetVisualScale(1f + Mathf.Sin(Time.time * 5f) * 0.03f);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer <= 0f)
                    {
                        _hopsLeft = hopsPerRun != null && hopsPerRun.Length >= Phase ? hopsPerRun[Phase - 1] : 2;
                        StartHop(targetPosition);
                    }
                    break;

                case State.Hop:
                    Hop(targetPosition);
                    break;

                case State.Aim:
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer > tongueHold) Face(toTarget, turnSpeed * 1.4f);
                    // Puffs up as it gets ready.
                    SetVisualScale(1f + 0.12f * (1f - Mathf.Clamp01(_timer / tongueAim)));
                    ShowChargeLine(1f - Mathf.Clamp01(_timer / tongueAim), tongueRange, tongueWidth, telegraphMaterial);
                    if (_timer <= 0f) Lash(targetPosition, targetHealth);
                    break;

                default:
                    Move(Vector3.zero);
                    Face(toTarget, turnSpeed * 0.5f);
                    SetVisualScale(1f);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer > 0f) break;
                    // Between the hops of a run the rest is short, and ends in the next hop rather than a sit.
                    if (_pendingHop)
                    {
                        _pendingHop = false;
                        StartHop(targetPosition);
                    }
                    else
                    {
                        Current = State.Sit;
                        _timer = sitTime;
                    }
                    break;
            }
        }

        void StartHop(Vector3 targetPosition)
        {
            // Where the player is going, not where they are; never further than one hop.
            Vector3 aim = targetPosition + _targetVelocity * (hopTime * 0.6f);
            Vector3 offset = aim - transform.position;
            offset.y = 0f;
            if (offset.magnitude > hopRange) offset = offset.normalized * hopRange;
            Vector3 landing = transform.position + offset;
            if (NavMesh.SamplePosition(landing, out var hit, 2.5f, NavMesh.AllAreas)) landing = hit.position;
            landing.y = transform.position.y;

            _hopFrom = transform.position;
            _hopTo = landing;
            _timer = 0f;
            Current = State.Hop;
            Hops++;
            Face(offset, 720f);
            GroundHazard.SpawnBlast(new Vector3(landing.x, 0f, landing.z), landingRadius, hopTime / SpeedScale, telegraphMaterial);
            Sfx.Play(SfxId.Dodge, 0.7f);
        }

        void Hop(Vector3 targetPosition)
        {
            float before = _timer;
            _timer = Mathf.Min(hopTime, _timer + Time.deltaTime * SpeedScale);
            // Across the floor at an even pace, with the arc drawn on the model only.
            Vector3 step = (_hopTo - _hopFrom) * ((_timer - before) / hopTime);
            step.y = -0.2f;
            Controller.Move(step);
            Knockback = Vector3.zero;
            float t = _timer / hopTime;
            if (visual != null) visual.localPosition = _visualRest + Vector3.up * (Mathf.Sin(t * Mathf.PI) * hopHeight);
            SetVisualScale(t < 0.5f ? 0.9f : 1.05f);
            if (_timer < hopTime) return;

            if (visual != null) visual.localPosition = _visualRest;
            SetVisualScale(1f);
            _hopsLeft--;
            if (_hopsLeft > 0)
            {
                // A short breath, then the next.
                Current = State.Rest;
                _timer = hopGap;
                _pendingHop = true;
                return;
            }
            if (canTongue)
            {
                Current = State.Aim;
                _timer = tongueAim;
            }
            else
            {
                Current = State.Rest;
                _timer = 1.2f;
            }
        }

        bool _pendingHop;

        void Lash(Vector3 targetPosition, Health targetHealth)
        {
            float length = Mathf.Max(1f, ChargeLineLength);
            HideChargeLine();
            Lashes++;
            ShowTongue(length);
            Sfx.Play(SfxId.Shoot, 0.6f);

            Vector3 local = transform.InverseTransformPoint(targetPosition);
            bool onStrip = local.z > 0f && local.z <= length + 0.5f && Mathf.Abs(local.x) <= tongueWidth * 0.5f + 0.45f;
            if (onStrip && targetHealth != null && targetHealth.TakeDamage(1, transform.forward))
            {
                Caught++;
                _reeling = targetHealth.GetComponent<PlayerController>();
                _reelUntil = Time.time + pullTime;
            }

            Current = State.Rest;
            _timer = 1.4f;
        }

        // Caught: a push is one frame's worth, so the tongue keeps pulling for a moment, and stops short of the mouth.
        void Reel()
        {
            if (_reeling == null) return;
            Vector3 toToad = transform.position - _reeling.transform.position;
            toToad.y = 0f;
            if (Time.time >= _reelUntil || toToad.magnitude < 2.2f)
            {
                _reeling = null;
                return;
            }
            _reeling.Push(toToad.normalized * tonguePull);
        }

        void ShowTongue(float length)
        {
            if (_tongue == null)
            {
                _tongue = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _tongue.name = "Tongue";
                Destroy(_tongue.GetComponent<Collider>());
                _tongue.transform.SetParent(transform, false);
                var renderer = _tongue.GetComponent<Renderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (tongueMaterial != null) renderer.sharedMaterial = tongueMaterial;
            }
            _tongue.transform.localPosition = new Vector3(0f, 0.6f, length * 0.5f);
            _tongue.transform.localScale = new Vector3(0.35f, 0.18f, length);
            _tongue.SetActive(true);
            _tongueUntil = Time.time + 0.2f;
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
