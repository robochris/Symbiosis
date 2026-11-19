using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Under the Sofa's boss. It does what a robot vacuum does: drives in straight lines, turns at the walls, and runs
    /// over whatever is in the way, flinging out rings of sparks as it goes. Every so often it stops and sucks, pulling
    /// the player in; then its bin is full and it has to stop and empty it, lid open, which is when it takes double
    /// damage. Emptying leaves lint piles behind, and the piles it left last time hatch. Below 55% it goes to turbo
    /// and starts charging straight at the player. Once, at a quarter health, its battery runs low and it stops to
    /// recharge: hurt it enough while it does and the recharge fails and leaves it stunned.
    /// </summary>
    public class RobotVacuum : BossEnemy
    {
        public enum State { Drive, SuckWindup, Suck, Empty, ChargeWindup, Charge, Dock }

        [Header("Driving")]
        public float driveSpeed = 6f;
        public float turnSpeed = 300f;
        [Tooltip("Seconds of driving between stops.")]
        public float driveTime = 5.5f;
        [Tooltip("Being this close to its middle while it moves is being run over.")]
        public float hitReach = 3.1f;
        public float hitCooldown = 1f;

        [Header("Sparks")]
        public Bullet projectilePrefab;
        public float sprayInterval = 3.2f;
        public int sprayShots = 8;
        public float projectileSpeed = 7.5f;
        public float projectileRange = 24f;

        [Header("Suction")]
        public float suckWindup = 0.9f;
        public float suckTime = 2.8f;
        [Tooltip("Metres a second the player is dragged, at point-blank range. It fades to nothing at the edge.")]
        public float pullSpeed = 6.5f;
        public float suckRange = 18f;
        [Tooltip("Dragged this close, the player is bitten.")]
        public float mouthReach = 3.3f;

        [Header("Emptying the bin (the weak moment)")]
        public float emptyTime = 2.4f;
        public int weakMultiplier = 2;
        public EggEnemy lintPrefab;
        public int startLint = 3;
        public int lintPerEmpty = 2;
        [Tooltip("The lid, lifted while the bin is open.")]
        public Transform lid;

        [Header("Turbo (phase 2)")]
        [Range(0f, 1f)] public float turboAt = 0.55f;
        public float turboSpeed = 1.3f;
        public float chargeWindup = 0.8f;
        public float chargeSpeed = 16f;
        public float chargeTime = 1.5f;
        public float trailSpacing = 2.6f;
        public Material sparkMaterial;

        [Header("Low battery (once)")]
        [Range(0f, 1f)] public float dockAt = 0.25f;
        public float dockTime = 4f;
        [Range(0f, 1f), Tooltip("A full recharge brings it back up to this share of its health.")]
        public float dockHealTo = 0.5f;
        [Tooltip("This much damage during the recharge makes it fail.")]
        public int dockInterrupt = 25;
        public float stunTime = 3.5f;

        public State Current { get; private set; }
        public bool BinOpen => Current == State.Empty;
        public int Sprays { get; private set; }
        public int Sucks { get; private set; }
        public int Empties { get; private set; }
        public int Charges { get; private set; }
        public int LintDropped { get; private set; }
        public bool Docked { get; private set; }
        public bool DockFailed { get; private set; }

        static readonly Dictionary<Bullet, Pool<Bullet>> Pools = new Dictionary<Bullet, Pool<Bullet>>();

        float _timer, _nextSpray, _nextHit, _nextBounce, _sinceTrail, _emptyFor, _dockHurt, _healCarry, _puff;
        int _chargesLeft, _stops;
        Vector3 _heading;
        Vector3 _lidHome;
        bool _started, _lidKnown;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = State.Drive;
            Sprays = Sucks = Empties = Charges = LintDropped = 0;
            Docked = DockFailed = false;
            _started = false;
            _chargesLeft = _stops = 0;
            if (lid != null && !_lidKnown)
            {
                _lidKnown = true;
                _lidHome = lid.localPosition;
            }
            SetLid(false);
            Health.Damaged += OnHurt;
        }

        protected override void OnDisable()
        {
            Health.Damaged -= OnHurt;
            base.OnDisable();
        }

        void SetLid(bool open)
        {
            if (lid == null) return;
            lid.localPosition = _lidHome + (open ? new Vector3(0f, 0.7f, -0.6f) : Vector3.zero);
            lid.localRotation = open ? Quaternion.Euler(-35f, 0f, 0f) : Quaternion.identity;
        }

        int Weak(Vector3 direction) => Current == State.Empty ? weakMultiplier : 1;

        void OnHurt(int amount, Vector3 direction)
        {
            if (Current != State.Dock || amount <= 0) return;
            _dockHurt += amount;
            if (_dockHurt < dockInterrupt) return;

            // The recharge fails: bin open, lights out, sitting there.
            DockFailed = true;
            Fx.Burst(transform.position + Vector3.up * 2f, new Color(1f, 0.95f, 0.4f), 40, 9f, 0.25f, 0.7f);
            Say("RECHARGE FAILED.");
            BeginEmpty(stunTime, false);
        }

        static void Say(string words)
        {
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast(words, 2.5f);
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                Move(Vector3.zero);
                return;
            }

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            Vector3 toward = distance > 0.01f ? toTarget / distance : transform.forward;

            if (!_started)
            {
                _started = true;
                Health.DamageScaler = Weak;
                _heading = toward;
                _timer = driveTime;
                _nextSpray = Time.time + 1.5f;
                for (int i = 0; i < startLint; i++)
                {
                    Vector3 direction = Quaternion.Euler(0f, 360f * i / Mathf.Max(1, startLint) + 60f, 0f) * Vector3.forward;
                    DropLint(transform.position + direction * 9f);
                }
            }
            if (Health.DamageScaler == null) Health.DamageScaler = Weak;

            float health = Health01;
            if (Phase == 1 && health <= turboAt)
            {
                Phase = 2;
                SpeedMultiplier *= turboSpeed;
                Say("TURBO MODE.");
            }
            if (!Docked && !Health.IsDead && health <= dockAt && Current != State.Charge)
            {
                Docked = true;
                Phase = 3;
                Current = State.Dock;
                _timer = dockTime;
                _dockHurt = 0f;
                _healCarry = 0f;
                HideChargeLine();
                SetVisualScale(1f);
                SetLid(false);
                Say("LOW BATTERY. RECHARGING.");
            }

            switch (Current)
            {
                case State.Drive:
                    Drive(toward, distance, targetHealth);
                    break;

                case State.SuckWindup:
                    Face(toward, turnSpeed);
                    SetVisualScale(1f + 0.12f * Mathf.Sin((1f - _timer / suckWindup) * Mathf.PI * 3f));
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer <= 0f)
                    {
                        SetVisualScale(1f);
                        Sucks++;
                        Current = State.Suck;
                        _timer = suckTime;
                    }
                    break;

                case State.Suck:
                    Face(toward, turnSpeed * 0.3f);
                    Move(Vector3.zero);
                    Pull(targetPosition, toward, distance, targetHealth);
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f) BeginEmpty(emptyTime, true);
                    break;

                case State.Empty:
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f)
                    {
                        SetLid(false);
                        StartDriving(toward);
                    }
                    break;

                case State.ChargeWindup:
                    Face(toward, turnSpeed);
                    SetVisualScale(Mathf.Lerp(1.15f, 1f, _timer / chargeWindup));
                    ShowChargeLine(1f - _timer / chargeWindup, chargeSpeed * SpeedScale * chargeTime, 4f, telegraphMaterial);
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer <= 0f)
                    {
                        HideChargeLine();
                        SetVisualScale(1f);
                        Charges++;
                        _heading = transform.forward;
                        _sinceTrail = trailSpacing;
                        Current = State.Charge;
                        _timer = chargeTime;
                    }
                    break;

                case State.Charge:
                    float step = chargeSpeed * SpeedScale * Time.deltaTime;
                    Move(_heading * (chargeSpeed * SpeedScale));
                    _sinceTrail += step;
                    if (_sinceTrail >= trailSpacing)
                    {
                        _sinceTrail = 0f;
                        GroundHazard.SpawnAcidPool(transform.position, sparkMaterial);
                    }
                    RunOver(distance, targetHealth);
                    _timer -= Time.deltaTime;
                    bool crashed = _timer < chargeTime - 0.15f && (Controller.collisionFlags & CollisionFlags.Sides) != 0;
                    if (crashed || _timer <= 0f)
                    {
                        if (--_chargesLeft > 0)
                        {
                            Current = State.ChargeWindup;
                            _timer = chargeWindup;
                        }
                        else
                        {
                            StartDriving(toward);
                        }
                    }
                    break;

                case State.Dock:
                    Move(Vector3.zero);
                    // Blinks while it recharges.
                    SetVisualScale(1f + 0.04f * Mathf.Sin(Time.time * 12f));
                    _healCarry += Time.deltaTime * Mathf.Max(0f, dockHealTo - dockAt) * Health.maxHealth / dockTime;
                    if (_healCarry >= 1f)
                    {
                        int whole = Mathf.FloorToInt(_healCarry);
                        _healCarry -= whole;
                        if (Health01 < dockHealTo) Health.Heal(whole);
                    }
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f)
                    {
                        SetVisualScale(1f);
                        Say("RECHARGED.");
                        StartDriving(toward);
                    }
                    break;
            }
        }

        void StartDriving(Vector3 toward)
        {
            Current = State.Drive;
            _timer = driveTime / SpeedScale;
            _heading = toward;
        }

        void Drive(Vector3 toward, float distance, Health targetHealth)
        {
            // Turns at a wall, and always back towards the player, give or take.
            bool blocked = (Controller.collisionFlags & CollisionFlags.Sides) != 0;
            if (blocked && Time.time >= _nextBounce)
            {
                _nextBounce = Time.time + 0.4f;
                _heading = Quaternion.Euler(0f, Random.Range(-50f, 50f), 0f) * toward;
            }

            Face(_heading, turnSpeed);
            Move(_heading * (driveSpeed * SpeedScale));
            RunOver(distance, targetHealth);

            if (Time.time >= _nextSpray)
            {
                _nextSpray = Time.time + sprayInterval / SpeedScale;
                Spray();
            }

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            // In turbo every other stop is a pair of charges instead of a go at sucking.
            _stops++;
            if (Phase >= 2 && _stops % 2 == 1)
            {
                _chargesLeft = 2;
                Current = State.ChargeWindup;
                _timer = chargeWindup;
            }
            else
            {
                Current = State.SuckWindup;
                _timer = suckWindup;
            }
        }

        void RunOver(float distance, Health targetHealth)
        {
            if (Time.time < _nextHit || distance > hitReach + ReachBonus(targetHealth)) return;
            if (targetHealth.TakeDamage(1, _heading)) _nextHit = Time.time + hitCooldown;
        }

        void Pull(Vector3 targetPosition, Vector3 toward, float distance, Health targetHealth)
        {
            var player = PlayerController.Instance;
            if (player == null || targetHealth != player.Health) return;

            float strength = Mathf.Clamp01(1f - distance / suckRange);
            if (strength > 0f && !player.IsHovering) player.Push(-toward * (pullSpeed * (0.35f + 0.65f * strength)));

            // Dust streaming in, so the pull can be seen.
            _puff -= Time.deltaTime;
            if (_puff <= 0f)
            {
                _puff = 0.12f;
                Vector3 from = transform.position + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(5f, 11f);
                Fx.Burst(from + Vector3.up * 0.4f, new Color(0.7f, 0.68f, 0.65f), 3, 2f, 0.16f, 0.35f);
            }

            if (distance <= mouthReach + ReachBonus(targetHealth) && Time.time >= _nextHit)
            {
                if (targetHealth.TakeDamage(1, toward)) _nextHit = Time.time + hitCooldown;
            }
        }

        // Bin open: double damage until it shuts. The piles left last time hatch, and new ones are left.
        void BeginEmpty(float seconds, bool dropLint)
        {
            Empties++;
            Current = State.Empty;
            _timer = seconds;
            HideChargeLine();
            SetVisualScale(1f);
            SetLid(true);
            if (!dropLint) return;

            foreach (var pile in new List<EggEnemy>(EggEnemy.All)) pile.Hatch();
            for (int i = 0; i < lintPerEmpty; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                DropLint(transform.position - transform.forward * 4.5f + transform.right * (side * 2.5f));
            }
        }

        void DropLint(Vector3 position)
        {
            if (lintPrefab == null) return;
            LintDropped++;
            RequestMinion(lintPrefab, position, false);
        }

        void Spray()
        {
            if (projectilePrefab == null) return;
            if (!Pools.TryGetValue(projectilePrefab, out var pool))
            {
                pool = new Pool<Bullet>(projectilePrefab);
                Pools.Add(projectilePrefab, pool);
            }

            Sprays++;
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            // Every other ring is turned half a step, so standing still in a gap only works once.
            float offset = Sprays % 2 == 0 ? 180f / sprayShots : 0f;
            for (int i = 0; i < sprayShots; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, 360f * i / sprayShots + offset, 0f) * transform.forward;
                pool.Get().Launch(origin + direction * 2.2f, direction, projectileSpeed, new Shot { damage = 1, owner = Health }, projectileRange, Team.Enemy, pool);
            }
        }
    }
}
