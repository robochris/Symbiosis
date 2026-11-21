using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Dr. Finch, at the bottom. The one who went in first fights the way the player does: keeps moving, shoots in
    /// short aimed bursts and sidesteps when hit. Twice in the fight (at two thirds and one third health) they stop to
    /// take another dose: they stand still while they shrink, which is the best moment to hurt them, and come out
    /// smaller, quicker, throwing flasks and with a couple of ants for company. When they are beaten they do not die
    /// so much as get too small to see.
    /// </summary>
    public class FinchBoss : BossEnemy
    {
        public enum State { Circle, Aim, Dash, Dose }

        /// <summary>Finch has just taken another dose. The radio has something to say about it.</summary>
        public static event Action<FinchBoss> Dosed;

        [Header("Moving")]
        public float moveSpeed = 3.5f;
        public float turnSpeed = 540f;
        [Tooltip("The distance from the player they try to keep.")]
        public float keepRange = 8f;

        [Header("Bursts")]
        public Bullet projectilePrefab;
        public Transform muzzle;
        public float burstInterval = 2.8f;
        public int burstShots = 3;
        public float shotGap = 0.16f;
        [Tooltip("Swells for this long before a burst: the warning.")]
        public float aimTime = 0.55f;
        public float projectileSpeed = 11f;
        public float projectileRange = 28f;
        [Range(0f, 1f), Tooltip("How much a burst aims ahead of a moving target.")]
        public float lead = 0.5f;

        [Header("Sidestep")]
        public float dashSpeed = 15f;
        public float dashTime = 0.22f;
        [Tooltip("Seconds before they can sidestep another hit.")]
        public float dashCooldown = 3.6f;

        [Header("Doses")]
        [Range(0f, 1f)] public float phase2At = 0.66f;
        [Range(0f, 1f)] public float phase3At = 0.33f;
        [Tooltip("Seconds they stand still, shrinking. The time to hurt them.")]
        public float doseTime = 1.6f;
        [Tooltip("Body size in each phase.")]
        public float[] phaseSize = { 1f, 0.7f, 0.48f };
        [Tooltip("Speed in each phase.")]
        public float[] phaseSpeed = { 1f, 1.12f, 1.25f };

        [Header("Flasks (after the first dose)")]
        public float flaskInterval = 5.5f;
        public float flaskRadius = 2.5f;
        public float flaskFuse = 1.3f;

        [Header("Company")]
        [Tooltip("What turns up after each dose. They have an understanding.")]
        public Enemy[] friends;
        public int friendsPerDose = 2;

        public State Current { get; private set; }
        public int Bursts { get; private set; }
        public int Doses { get; private set; }
        public int Sidesteps { get; private set; }
        public int FlasksThrown { get; private set; }

        static readonly Dictionary<Bullet, Pool<Bullet>> Pools = new Dictionary<Bullet, Pool<Bullet>>();

        float _timer, _nextBurst, _nextShot, _nextDash, _nextFlask, _strafe = 1f, _nextTurn;
        float _fromSize, _toSize;
        int _shotsLeft;
        Vector3 _dashDirection, _lastTargetPosition, _targetVelocity;
        bool _started;
        float _radius, _height;

        protected override void Awake()
        {
            base.Awake();
            _radius = Controller.radius;
            _height = Controller.height;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = State.Circle;
            Bursts = Doses = Sidesteps = FlasksThrown = 0;
            _shotsLeft = 0;
            _started = false;
            SetSize(Size(1));
            Health.Damaged += OnHurt;
        }

        protected override void OnDisable()
        {
            Health.Damaged -= OnHurt;
            base.OnDisable();
        }

        float Size(int phase) => phaseSize != null && phaseSize.Length >= phase ? phaseSize[phase - 1] : 1f;
        float Speed(int phase) => phaseSpeed != null && phaseSpeed.Length >= phase ? phaseSpeed[phase - 1] : 1f;

        // The body shrinks all the way; the part that can be shot shrinks a third as much, so the fight stays fair.
        void SetSize(float size)
        {
            BaseScale = size;
            SetVisualScale(1f);
            float body = (2f + size) / 3f;
            Controller.radius = _radius * body;
            Controller.height = _height * body;
            Controller.center = new Vector3(0f, Controller.height * 0.5f, 0f);
        }

        // A hit while they are free to move: step out of the line of fire, like the player would.
        void OnHurt(int amount, Vector3 direction)
        {
            if (Current != State.Circle || Time.time < _nextDash || Health.IsDead || frozen) return;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
            float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            _dashDirection = Quaternion.Euler(0f, 90f * side, 0f) * direction.normalized;
            _nextDash = Time.time + dashCooldown / SpeedScale;
            _timer = dashTime;
            Current = State.Dash;
            Sidesteps++;
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                Move(Vector3.zero);
                return;
            }

            if (!_started)
            {
                _started = true;
                _nextBurst = Time.time + 1.4f;
                _nextFlask = Time.time + flaskInterval;
                _lastTargetPosition = targetPosition;
            }

            if (Time.deltaTime > 0f)
            {
                Vector3 velocity = (targetPosition - _lastTargetPosition) / Time.deltaTime;
                velocity.y = 0f;
                _targetVelocity = Vector3.Lerp(_targetVelocity, velocity, 0.15f);
                _lastTargetPosition = targetPosition;
            }

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            float health = Health01;
            if (Current != State.Dose && ((Phase == 1 && health <= phase2At) || (Phase == 2 && health <= phase3At))) StartDose();

            // A burst already started carries on while they move.
            if (_shotsLeft > 0 && Current != State.Dose && Time.time >= _nextShot)
            {
                _shotsLeft--;
                _nextShot = Time.time + shotGap;
                Fire(toTarget);
            }

            switch (Current)
            {
                case State.Dose:
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime;
                    SetSize(Mathf.Lerp(_toSize, _fromSize, Mathf.Clamp01(_timer / doseTime)));
                    if (_timer <= 0f) EndDose();
                    break;

                case State.Dash:
                    Move(_dashDirection * dashSpeed);
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f) Current = State.Circle;
                    break;

                case State.Aim:
                    Face(toTarget, turnSpeed);
                    SetVisualScale(Mathf.Lerp(1.3f, 1f, _timer / aimTime));
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer <= 0f)
                    {
                        SetVisualScale(1f);
                        Bursts++;
                        _shotsLeft = burstShots;
                        _nextShot = Time.time;
                        _nextBurst = Time.time + burstInterval / SpeedScale;
                        Current = State.Circle;
                    }
                    break;

                default:
                    Circle(targetPosition, toTarget, distance);
                    break;
            }
        }

        void Circle(Vector3 targetPosition, Vector3 toTarget, float distance)
        {
            bool sees = CanSee(targetPosition);
            Vector3 heading;
            if (!sees)
            {
                heading = SteerToward(targetPosition) - transform.position;
                heading.y = 0f;
            }
            else
            {
                // Round the player at arm's length: sideways, with a push in or out to hold the distance.
                Vector3 radial = distance > 0.01f ? toTarget / distance : transform.forward;
                Vector3 tangent = Quaternion.Euler(0f, 90f * _strafe, 0f) * radial;
                float push = Mathf.Clamp((distance - keepRange) / 3f, -1f, 1f);
                heading = tangent + radial * push;
            }

            // Turn round at a wall, and now and then anyway, so the circle can't be read.
            bool blocked = (Controller.collisionFlags & CollisionFlags.Sides) != 0;
            if (Time.time >= _nextTurn && (blocked || UnityEngine.Random.value < Time.deltaTime * 0.25f))
            {
                _strafe = -_strafe;
                _nextTurn = Time.time + 0.8f;
            }

            Face(sees ? toTarget : heading, turnSpeed);
            Move(heading.sqrMagnitude > 0.01f ? heading.normalized * (moveSpeed * SpeedScale) : Vector3.zero);

            if (sees && _shotsLeft == 0 && Time.time >= _nextBurst)
            {
                Current = State.Aim;
                _timer = aimTime;
                return;
            }

            if (Phase >= 2 && Time.time >= _nextFlask)
            {
                _nextFlask = Time.time + flaskInterval / SpeedScale;
                ThrowFlasks(targetPosition);
            }
        }

        void StartDose()
        {
            Phase++;
            Doses++;
            Current = State.Dose;
            _timer = doseTime;
            _fromSize = BaseScale;
            _toSize = Size(Phase);
            _shotsLeft = 0;
            Fx.Burst(transform.position + Vector3.up, new Color(0.45f, 0.95f, 1f), 24, 6f, 0.2f, 0.5f);
            Sfx.Play(SfxId.BossAppear);
            Dosed?.Invoke(this);
        }

        void EndDose()
        {
            SetSize(_toSize);
            SpeedMultiplier = Speed(Phase);
            _nextBurst = Time.time + 0.8f;
            _nextFlask = Time.time + 1.5f;
            Current = State.Circle;

            for (int i = 0; friends != null && friends.Length > 0 && i < friendsPerDose; i++)
            {
                Enemy friend = friends[UnityEngine.Random.Range(0, friends.Length)];
                float side = i % 2 == 0 ? -1f : 1f;
                if (friend != null) RequestMinion(friend, transform.position + transform.right * (side * 3.5f), false);
            }
        }

        // One where the player is going; after the second dose, one where they are standing too.
        void ThrowFlasks(Vector3 targetPosition)
        {
            Vector3 ahead = _targetVelocity.sqrMagnitude > 1f ? _targetVelocity.normalized : transform.right;
            Spill(targetPosition + ahead * (flaskRadius * 1.6f));
            if (Phase >= 3) Spill(targetPosition);
        }

        void Spill(Vector3 position)
        {
            position.y = 0f;
            GroundHazard.SpawnBlast(position, flaskRadius, flaskFuse, telegraphMaterial);
            FlasksThrown++;
        }

        // What is left in the flask.
        // The item is always the dose.
        protected override ItemDefinition RewardItem(ItemLibrary items)
        {
            var dose = items.Get(ItemType.FinchsDose);
            return dose != null ? dose : base.RewardItem(items);
        }

        void Fire(Vector3 toTarget)
        {
            if (projectilePrefab == null || toTarget.sqrMagnitude < 0.01f) return;
            if (!Pools.TryGetValue(projectilePrefab, out var pool))
            {
                pool = new Pool<Bullet>(projectilePrefab);
                Pools.Add(projectilePrefab, pool);
            }

            Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * 0.7f;
            Vector3 aimAt = toTarget + _targetVelocity * (toTarget.magnitude / projectileSpeed * lead);
            Vector3 direction = aimAt.sqrMagnitude > 0.01f ? aimAt.normalized : toTarget.normalized;
            pool.Get().Launch(origin, direction, projectileSpeed, new Shot { damage = 1, owner = Health }, projectileRange, IsConfused ? Team.Player : Team.Enemy, pool);
        }
    }
}
