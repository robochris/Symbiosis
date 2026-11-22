using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Birthday Cake's boss, with a fight of its own. A candle does not chase anybody: it slides slowly across
    /// the cake on its plate and makes the floor a worse and worse place to stand. It drips hot wax where the player
    /// is going (a warning disc, then a pool that stays), lights its sparkler boxes one at a time so that something
    /// climbs out, and below 60% it spins, throwing wax out in a spiral that has to be walked round with it.
    /// Its trick: the first time it is nearly out, the flame goes out, it stands helpless for a moment, and then it
    /// relights itself with a burst of fire and a good share of its health back. The moment it is out is the best
    /// chance to hurt it; being next to it when it relights is the worst place to be.
    /// </summary>
    public class TrickCandle : BossEnemy
    {
        public enum State { Burn, SpiralWindup, Spiral }

        [Header("Sliding")]
        public float slideSpeed = 1.6f;
        public float turnSpeed = 140f;

        [Header("Wax drips")]
        public float waxInterval = 4.4f;
        public int waxDrops = 2;
        public float waxRadius = 2f;
        [Tooltip("Seconds between the warning disc and the wax landing.")]
        public float waxFuse = 1.2f;
        [Tooltip("Seconds a pool of wax stays hot.")]
        public float waxPoolTime = 2.2f;
        [Tooltip("What the pool left behind is made of.")]
        public Material waxMaterial;

        [Header("Sparkler boxes")]
        public EggEnemy sparklerPrefab;
        public int sparklerCount = 4;
        [Tooltip("Seconds between it lighting one.")]
        public float lightInterval = 13f;
        public float sparklerRing = 11f;

        [Header("Spiral (below 60%)")]
        [Range(0f, 1f)] public float spiralAt = 0.6f;
        public Bullet projectilePrefab;
        public float spiralEvery = 9f;
        public float spiralWindup = 0.8f;
        public float spiralTime = 3.2f;
        public int spiralArms = 3;
        [Tooltip("Seconds between shots down each arm.")]
        public float spiralGap = 0.22f;
        [Tooltip("Degrees a second the spiral turns.")]
        public float spiralTurn = 75f;
        public float projectileSpeed = 7f;
        public float projectileRange = 26f;

        [Header("The trick")]
        [Range(0f, 1f), Tooltip("It goes out the first time its health drops to this share.")]
        public float goesOutAt = 0.35f;
        [Tooltip("Seconds it stands unlit before it relights.")]
        public float outTime = 2.4f;
        [Range(0f, 1f), Tooltip("Relighting brings its health back up to this share.")]
        public float relightsTo = 0.6f;
        [Tooltip("The fire that bursts out round it when it relights.")]
        public float relightRadius = 6.5f;
        [Tooltip("How much quicker everything is once it has relit.")]
        public float relitSpeed = 1.3f;
        public Transform flame;

        public State Current { get; private set; }
        /// <summary>True from the moment it has gone out once. It only has the one trick.</summary>
        public bool TrickUsed { get; private set; }
        public bool IsOut => _out > 0f;
        public int WaxDropped { get; private set; }
        public int Spirals { get; private set; }
        public int SparklersLit { get; private set; }

        static readonly Dictionary<Bullet, Pool<Bullet>> Pools = new Dictionary<Bullet, Pool<Bullet>>();

        // Wax in the air: where it will land, and when.
        readonly List<Vector3> _fallingAt = new List<Vector3>();
        readonly List<float> _fallingWhen = new List<float>();

        float _out, _timer, _nextWax, _nextLight, _nextSpiral, _nextShot, _angle;
        bool _started;
        float _holdUntil;
        Vector3 _lastTargetPosition, _targetVelocity, _flameSize;
        bool _flameKnown;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = State.Burn;
            TrickUsed = false;
            _out = 0f;
            _holdUntil = 0f;
            _started = false;
            WaxDropped = Spirals = SparklersLit = 0;
            _fallingAt.Clear();
            _fallingWhen.Clear();
            if (flame != null)
            {
                if (!_flameKnown)
                {
                    _flameKnown = true;
                    _flameSize = flame.localScale;
                }
                flame.localScale = _flameSize;
                flame.gameObject.SetActive(true);
            }
        }

        protected override void Think()
        {
            LandWax();

            if (_out > 0f)
            {
                Move(Vector3.zero);
                _out -= Time.deltaTime;
                if (_out <= 0f) Relight();
                return;
            }

            if (!TrickUsed && !Health.IsDead && Health01 <= goesOutAt)
            {
                GoOut();
                return;
            }

            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                Move(Vector3.zero);
                return;
            }

            if (!_started) Begin(targetPosition);

            if (Time.deltaTime > 0f)
            {
                Vector3 velocity = (targetPosition - _lastTargetPosition) / Time.deltaTime;
                velocity.y = 0f;
                _targetVelocity = Vector3.Lerp(_targetVelocity, velocity, 0.15f);
                _lastTargetPosition = targetPosition;
            }

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            if (Phase == 1 && Health01 <= spiralAt)
            {
                Phase = 2;
                _nextSpiral = Time.time + 1f;
            }

            // The boxes are lit on a clock, whatever else it is doing.
            if (Time.time >= _nextLight)
            {
                _nextLight = Time.time + lightInterval / SpeedScale;
                if (LightOne()) SparklersLit++;
            }

            switch (Current)
            {
                case State.SpiralWindup:
                    // The flame swells: the warning.
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (flame != null) flame.localScale = _flameSize * Mathf.Lerp(2.2f, 1f, Mathf.Clamp01(_timer / spiralWindup));
                    if (_timer <= 0f)
                    {
                        Spirals++;
                        Current = State.Spiral;
                        _timer = spiralTime;
                        _nextShot = Time.time;
                        _angle = transform.eulerAngles.y;
                    }
                    break;

                case State.Spiral:
                    Move(Vector3.zero);
                    _angle += spiralTurn * SpeedScale * Time.deltaTime;
                    if (Time.time >= _nextShot)
                    {
                        _nextShot = Time.time + spiralGap;
                        FireArms();
                    }
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f)
                    {
                        if (flame != null) flame.localScale = _flameSize;
                        Current = State.Burn;
                        _nextSpiral = Time.time + spiralEvery / SpeedScale;
                        _nextWax = Time.time + 1f;
                    }
                    break;

                default:
                    Face(toTarget, turnSpeed);
                    Move(toTarget.magnitude > 5f && Time.time >= _holdUntil ? transform.forward * (slideSpeed * SpeedScale) : Vector3.zero);

                    if (Time.time >= _nextWax)
                    {
                        _nextWax = Time.time + waxInterval / SpeedScale;
                        DripWax(targetPosition);
                    }

                    if (Phase >= 2 && projectilePrefab != null && Time.time >= _nextSpiral)
                    {
                        Current = State.SpiralWindup;
                        _timer = spiralWindup;
                    }
                    break;
            }
        }

        void Begin(Vector3 targetPosition)
        {
            _started = true;
            _lastTargetPosition = targetPosition;
            _nextWax = Time.time + 1.5f;
            _nextLight = Time.time + lightInterval;
            _nextSpiral = Time.time + spiralEvery;

            // Sparkler boxes go against the walls, evenly round the cake.
            if (sparklerPrefab == null) return;
            Vector3 origin = transform.position + Vector3.up * 0.7f;
            for (int i = 0; i < sparklerCount; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, 360f * i / sparklerCount + 45f, 0f) * Vector3.forward;
                float reach = sparklerRing;
                if (Physics.Raycast(origin, direction, out var hit, sparklerRing + 2f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<Health>() == null)
                {
                    reach = Mathf.Max(3f, hit.distance - 1.6f);
                }
                RequestMinion(sparklerPrefab, transform.position + direction * reach, false);
            }
        }

        bool LightOne()
        {
            if (EggEnemy.All.Count == 0) return false;
            EggEnemy box = EggEnemy.All[Random.Range(0, EggEnemy.All.Count)];
            int before = EggEnemy.All.Count;
            box.Hatch();
            return EggEnemy.All.Count < before;
        }

        // One where the player is going, the rest scattered close by, so standing still and running straight both lose.
        void DripWax(Vector3 targetPosition)
        {
            for (int i = 0; i < waxDrops; i++)
            {
                Vector3 at = i == 0
                    ? targetPosition + _targetVelocity * (waxFuse * 0.8f)
                    : targetPosition + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(2.5f, 5.5f);
                at.y = 0f;
                GroundHazard.SpawnBlast(at, waxRadius, waxFuse, telegraphMaterial);
                _fallingAt.Add(at);
                _fallingWhen.Add(Time.time + waxFuse);
                WaxDropped++;
            }
        }

        void LandWax()
        {
            for (int i = _fallingAt.Count - 1; i >= 0; i--)
            {
                if (Time.time < _fallingWhen[i]) continue;
                GroundHazard.SpawnAcidPool(_fallingAt[i], waxMaterial).lifetime = waxPoolTime;
                _fallingAt.RemoveAt(i);
                _fallingWhen.RemoveAt(i);
            }
        }

        void FireArms()
        {
            if (!Pools.TryGetValue(projectilePrefab, out var pool))
            {
                pool = new Pool<Bullet>(projectilePrefab);
                Pools.Add(projectilePrefab, pool);
            }
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            for (int i = 0; i < spiralArms; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, _angle + 360f * i / spiralArms, 0f) * Vector3.forward;
                pool.Get().Launch(origin + direction * 2f, direction, projectileSpeed, new Shot { damage = 1, owner = Health }, projectileRange, Team.Enemy, pool);
            }
        }

        void GoOut()
        {
            TrickUsed = true;
            _out = outTime;
            Current = State.Burn;
            if (flame != null)
            {
                flame.localScale = _flameSize;
                flame.gameObject.SetActive(false);
            }
            HideChargeLine();
            SetVisualScale(1f);
            Move(Vector3.zero);
            // The warning for the relight: a disc that fills in round it for exactly as long as it is out.
            GroundHazard.SpawnBlast(transform.position, relightRadius, outTime, telegraphMaterial);
        }

        void Relight()
        {
            if (flame != null) flame.gameObject.SetActive(true);
            int back = Mathf.RoundToInt(Health.maxHealth * relightsTo) - Health.Current;
            if (back > 0) Health.Heal(back);
            Phase = 3;
            SpeedMultiplier *= relitSpeed;
            _nextSpiral = Time.time + 2f;
            _nextWax = Time.time + 1f;
            // It stands where it relit for a moment before it sets off again.
            _holdUntil = Time.time + 1.2f;
            Fx.Burst(transform.position + Vector3.up * 3f, new Color(1f, 0.6f, 0.15f), 40, 9f, 0.3f, 0.6f);
            Sfx.Play(SfxId.BossAppear);
        }
    }
}
