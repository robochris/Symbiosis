using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Fridge's mini boss: a tub of something that has been in there since March. With the lid on, nothing gets
    /// in: every shot bounces off, and it slides after the player and shoves. Every few seconds the lid rattles and
    /// comes up, and for a moment it can be hurt, while it lets out a ring of mould and a couple of things that used
    /// to be fruit. The whole fight is waiting for the lid. Below half health it stays open for less time and lets
    /// out more.
    /// </summary>
    public class Leftovers : BossEnemy
    {
        public enum State { Shut, Rattle, Open }

        [Header("Lid on")]
        public float slideSpeed = 2.6f;
        public float turnSpeed = 170f;
        [Tooltip("Seconds it keeps the lid on between openings.")]
        public float shutTime = 4.2f;
        [Tooltip("Shoves the player when it slides into them.")]
        public float shoveReach = 2.5f;
        public float shoveCooldown = 1.3f;

        [Header("Lid off")]
        [Tooltip("Seconds the lid rattles before it comes up: the warning.")]
        public float rattleTime = 0.8f;
        public float openTime = 3.4f;
        public Transform lid;

        [Header("What comes out")]
        public Bullet projectilePrefab;
        public int sprayShots = 8;
        public float projectileSpeed = 6.5f;
        public float projectileRange = 22f;
        public Enemy sporePrefab;
        public int sporesPerOpening = 2;
        [Tooltip("No more are let out while this many others are still about.")]
        public int maxCompany = 6;

        [Header("Below half health")]
        [Range(0f, 1f)] public float worseAt = 0.5f;
        public float worseOpenTime = 2.5f;
        public int worseSprayShots = 12;

        public State Current { get; private set; }
        public bool IsOpen => Current == State.Open;
        public int Openings { get; private set; }
        public int ShotsBlocked { get; private set; }
        public int Released { get; private set; }

        static readonly Dictionary<Bullet, Pool<Bullet>> Pools = new Dictionary<Bullet, Pool<Bullet>>();

        float _timer, _nextShove;
        Vector3 _lidHome;
        bool _lidKnown, _guarded;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = State.Shut;
            _timer = shutTime * 0.6f;
            _guarded = false;
            Openings = ShotsBlocked = Released = 0;
            if (lid != null && !_lidKnown)
            {
                _lidKnown = true;
                _lidHome = lid.localPosition;
            }
            SetLid(false);
            Health.Blocked += OnBlocked;
        }

        protected override void OnDisable()
        {
            Health.Blocked -= OnBlocked;
            base.OnDisable();
        }

        void OnBlocked(Vector3 direction)
        {
            ShotsBlocked++;
            Sfx.Play(SfxId.Blocked, 0.6f);
        }

        void SetLid(bool open)
        {
            if (lid == null) return;
            lid.localPosition = _lidHome + (open ? new Vector3(0f, 0.55f, -0.45f) : Vector3.zero);
            lid.localRotation = open ? Quaternion.Euler(-40f, 0f, 0f) : Quaternion.identity;
        }

        protected override void Think()
        {
            // Nothing gets in while the lid is on.
            if (!_guarded)
            {
                _guarded = true;
                Health.ShouldBlock = direction => Current != State.Open;
            }

            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                Move(Vector3.zero);
                return;
            }

            Phase = Health01 <= worseAt ? 2 : 1;
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            switch (Current)
            {
                case State.Shut:
                    Face(SteerToward(targetPosition) - transform.position, turnSpeed);
                    Move(distance > 1.5f ? transform.forward * (slideSpeed * SpeedScale) : Vector3.zero);
                    if (distance <= shoveReach + ReachBonus(targetHealth) && Time.time >= _nextShove)
                    {
                        if (targetHealth.TakeDamage(1, transform.forward)) _nextShove = Time.time + shoveCooldown;
                    }
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer <= 0f)
                    {
                        Current = State.Rattle;
                        _timer = rattleTime;
                    }
                    break;

                case State.Rattle:
                    Move(Vector3.zero);
                    // The lid jumps about.
                    if (lid != null) lid.localPosition = _lidHome + Vector3.up * (0.08f * Mathf.Abs(Mathf.Sin(Time.time * 30f)));
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f) Open(toTarget);
                    break;

                case State.Open:
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f)
                    {
                        SetLid(false);
                        Current = State.Shut;
                        _timer = shutTime;
                    }
                    break;
            }
        }

        void Open(Vector3 toTarget)
        {
            Openings++;
            Current = State.Open;
            _timer = Phase >= 2 ? worseOpenTime : openTime;
            SetLid(true);
            Fx.Burst(transform.position + Vector3.up * 1.6f, new Color(0.5f, 0.75f, 0.3f), 24, 6f, 0.25f, 0.6f);
            Spray(Phase >= 2 ? worseSprayShots : sprayShots);

            int company = 0;
            foreach (var enemy in Active) if (enemy != this && !enemy.Health.IsDead) company++;
            for (int i = 0; sporePrefab != null && i < sporesPerOpening && company + i < maxCompany; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                RequestMinion(sporePrefab, transform.position + transform.right * (side * 3f) + transform.forward * 1.5f, Phase >= 2);
                Released++;
            }
        }

        void Spray(int shots)
        {
            if (projectilePrefab == null || shots <= 0) return;
            if (!Pools.TryGetValue(projectilePrefab, out var pool))
            {
                pool = new Pool<Bullet>(projectilePrefab);
                Pools.Add(projectilePrefab, pool);
            }
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            float turn = Random.Range(0f, 360f);
            for (int i = 0; i < shots; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, turn + 360f * i / shots, 0f) * Vector3.forward;
                pool.Get().Launch(origin + direction * 1.8f, direction, projectileSpeed, new Shot { damage = 1, owner = Health }, projectileRange, Team.Enemy, pool);
            }
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
