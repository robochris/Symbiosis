using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Under the Sofa's mini boss: a dust bunny the size of a car, and it hops. It crouches, a disc on the floor shows
    /// where it will come down, and it lands there hard, shedding clumps of fluff that grow into dust bunnies if they
    /// are left. Between hops it sits and gets its breath back, which is the time to shoot it; its tail takes double.
    /// Now and then it sneezes a fan of dust instead. Below half health it rests less and every landing leaves a
    /// ring of sticky lint.
    /// </summary>
    public class GrandmotherDust : BossEnemy
    {
        public enum State { Rest, Crouch, Air, SneezeWindup }

        [Header("Hop")]
        public float crouchTime = 0.75f;
        public float airTime = 0.7f;
        [Tooltip("The furthest one hop carries it.")]
        public float hopRange = 16f;
        public float hopHeight = 5f;
        public float landRadius = 3.4f;
        public float restTime = 1.8f;
        public float turnSpeed = 220f;

        [Header("Fluff")]
        public LarvaEnemy fluffPrefab;
        public int fluffPerLanding = 2;
        [Tooltip("No more fluff is shed while this many clumps are still about.")]
        public int maxFluff = 6;

        [Header("Sneeze")]
        public Bullet projectilePrefab;
        [Tooltip("Sneezes instead of hopping every this many turns.")]
        public int sneezeEvery = 3;
        public float sneezeWindup = 0.7f;
        public int sneezeShots = 5;
        public float sneezeSpread = 13f;
        public float projectileSpeed = 8f;
        public float projectileRange = 24f;

        [Header("Shedding (below half health)")]
        [Range(0f, 1f)] public float shedAt = 0.5f;
        public float shedRest = 0.6f;
        public int stickyPatches = 5;
        public Material stickyMaterial;

        [Header("Weak point")]
        [Tooltip("Damage multiplier for hits to the tail, from behind.")]
        public int weakPointMultiplier = 2;

        public State Current { get; private set; }
        public int Hops { get; private set; }
        public int Sneezes { get; private set; }
        public int FluffShed { get; private set; }

        static readonly Dictionary<Bullet, Pool<Bullet>> Pools = new Dictionary<Bullet, Pool<Bullet>>();

        float _timer;
        int _turns;
        Vector3 _from, _to;
        Vector3 _visualHome;
        bool _homeKnown;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = State.Rest;
            _timer = 1f;
            _turns = 0;
            Hops = Sneezes = FluffShed = 0;
            if (visual != null && !_homeKnown)
            {
                _homeKnown = true;
                _visualHome = visual.localPosition;
            }
            Ground();
        }

        protected override void OnDisable()
        {
            Ground();
            base.OnDisable();
        }

        void Ground()
        {
            if (Controller != null) Controller.enabled = true;
            if (visual != null && _homeKnown) visual.localPosition = _visualHome;
        }

        // A shot travelling the same way it faces came from behind: it hit the tail.
        int WeakPoint(Vector3 direction)
        {
            direction.y = 0f;
            return Vector3.Dot(direction.normalized, transform.forward) > 0.3f ? weakPointMultiplier : 1;
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                if (Current != State.Air) Move(Vector3.zero);
                return;
            }

            if (Health.DamageScaler == null) Health.DamageScaler = WeakPoint;
            Phase = Health01 <= shedAt ? 2 : 1;

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;

            switch (Current)
            {
                case State.Rest:
                    Face(toTarget, turnSpeed * 0.35f);
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer > 0f) break;

                    _turns++;
                    if (projectilePrefab != null && sneezeEvery > 0 && _turns % sneezeEvery == 0)
                    {
                        Current = State.SneezeWindup;
                        _timer = sneezeWindup;
                    }
                    else
                    {
                        BeginHop(toTarget);
                    }
                    break;

                case State.SneezeWindup:
                    // Rears back and swells: the warning.
                    Face(toTarget, turnSpeed);
                    SetVisualScale(Mathf.Lerp(1.25f, 1f, _timer / sneezeWindup));
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer <= 0f)
                    {
                        SetVisualScale(1f);
                        Sneeze(toTarget);
                        Current = State.Rest;
                        _timer = RestTime * 0.7f;
                    }
                    break;

                case State.Crouch:
                    Face(_to - transform.position, turnSpeed);
                    SetVisualScale(Mathf.Lerp(0.8f, 1f, _timer / crouchTime));
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f)
                    {
                        SetVisualScale(1f);
                        Hops++;
                        Current = State.Air;
                        _timer = airTime;
                        _from = transform.position;
                        Controller.enabled = false;
                    }
                    break;

                case State.Air:
                    _timer -= Time.deltaTime;
                    float along = 1f - Mathf.Clamp01(_timer / airTime);
                    Vector3 at = Vector3.Lerp(_from, _to, along);
                    at.y = _from.y;
                    transform.position = at;
                    if (visual != null) visual.localPosition = _visualHome + Vector3.up * (hopHeight * 4f * along * (1f - along));
                    if (_timer <= 0f) Land();
                    break;
            }
        }

        float RestTime => restTime * (Phase >= 2 ? shedRest : 1f);

        void BeginHop(Vector3 toTarget)
        {
            float distance = Mathf.Min(toTarget.magnitude, hopRange);
            Vector3 direction = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : transform.forward;
            _to = transform.position + direction * distance;

            // Somewhere it can stand: the nearest walkable spot to where it means to come down.
            if (UnityEngine.AI.NavMesh.SamplePosition(_to, out var hit, 6f, UnityEngine.AI.NavMesh.AllAreas) && hit.position.y < 0.5f)
            {
                _to = new Vector3(hit.position.x, transform.position.y, hit.position.z);
            }

            Current = State.Crouch;
            _timer = crouchTime;
            // The disc fills in for exactly as long as the hop takes, and goes off as it lands.
            GroundHazard.SpawnBlast(new Vector3(_to.x, 0f, _to.z), landRadius, crouchTime + airTime, telegraphMaterial);
        }

        void Land()
        {
            transform.position = new Vector3(_to.x, _from.y, _to.z);
            Ground();
            Current = State.Rest;
            _timer = RestTime;
            if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.4f);
            Fx.Burst(transform.position + Vector3.up * 0.3f, new Color(0.7f, 0.68f, 0.65f), 26, 7f, 0.3f, 0.6f);

            if (fluffPrefab != null)
            {
                int about = 0;
                foreach (var enemy in Active) if (enemy is LarvaEnemy && !enemy.Health.IsDead) about++;
                for (int i = 0; i < fluffPerLanding && about + i < maxFluff; i++)
                {
                    float angle = Random.Range(0f, 360f);
                    RequestMinion(fluffPrefab, transform.position + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (landRadius + 1f), false);
                    FluffShed++;
                }
            }

            if (Phase >= 2 && stickyMaterial != null)
            {
                float turn = Random.Range(0f, 360f);
                for (int i = 0; i < stickyPatches; i++)
                {
                    Vector3 direction = Quaternion.Euler(0f, turn + 360f * i / stickyPatches, 0f) * Vector3.forward;
                    GroundHazard.SpawnStickyPatch(transform.position + direction * (landRadius + 1.5f), stickyMaterial);
                }
            }
        }

        void Sneeze(Vector3 toTarget)
        {
            if (projectilePrefab == null) return;
            // Sat right on top of whoever it is sneezing at: straight ahead will do.
            if (toTarget.sqrMagnitude < 0.01f) toTarget = transform.forward;
            if (!Pools.TryGetValue(projectilePrefab, out var pool))
            {
                pool = new Pool<Bullet>(projectilePrefab);
                Pools.Add(projectilePrefab, pool);
            }

            Sneezes++;
            Vector3 origin = transform.position + Vector3.up * 0.8f + transform.forward * 2f;
            Vector3 forward = toTarget.normalized;
            for (int i = 0; i < sneezeShots; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, (i - (sneezeShots - 1) * 0.5f) * sneezeSpread, 0f) * forward;
                pool.Get().Launch(origin, direction, projectileSpeed, new Shot { damage = 1, owner = Health }, projectileRange, Team.Enemy, pool);
            }
            Fx.Burst(origin, new Color(0.7f, 0.68f, 0.65f), 18, 6f, 0.22f, 0.5f);
        }

        public override void DropRewards()
        {
            base.DropRewards();
            var assets = GameAssets.Instance;
            if (assets == null || assets.items == null) return;
            var picks = assets.items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), 1);
            if (picks.Count > 0) Pickup.Item(picks[0], transform.position, 0);
        }
    }
}
