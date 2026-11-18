using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Shrunk
{
    /// <summary>
    /// Shared by every enemy: the list of live enemies, movement, pathing, knockback, speed changes and dying.
    /// A behaviour (Chaser, Charger, Shooter, Flyer) extends this and fills in Think().
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public abstract class Enemy : MonoBehaviour
    {
        /// <summary>Every enemy currently in play. Used by aim assist and the spawner.</summary>
        public static readonly List<Enemy> Active = new List<Enemy>();
        public static event Action<Enemy> Killed;
        /// <summary>An enemy is calling for help: spawn this many of the biome's swarm enemy next to it.</summary>
        public static event Action<Enemy, int> SummonRequested;

        [Tooltip("The body; scaled for telegraphs and the death pop.")]
        public Transform visual;
        public float knockbackOnHit = 5f;
        public float knockbackDecay = 14f;

        public Health Health { get; private set; }
        public EnemyMutations Mutations { get; private set; }
        /// <summary>Lasting speed change from mutations (Fast = 1.5).</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        /// <summary>Lasting size change from mutations.</summary>
        public float BaseScale { get; set; } = 1f;
        public bool IsBuffed => Time.time < _buffUntil;
        /// <summary>Everything that changes how fast this enemy moves and attacks, multiplied together.</summary>
        public float SpeedScale => SpeedMultiplier * (IsBuffed ? 1.4f : 1f);

        protected CharacterController Controller { get; private set; }
        protected Vector3 Knockback;

        NavMeshPath _path;
        Vector3 _corner;
        bool _hasCorner;
        float _nextPathTime;
        float _buffUntil;
        float _nextTrailCheck;

        protected virtual void Awake()
        {
            Health = GetComponent<Health>();
            Controller = GetComponent<CharacterController>();
            Mutations = GetComponent<EnemyMutations>();
        }

        protected virtual void OnEnable()
        {
            Active.Add(this);
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
            Knockback = Vector3.zero;
            _hasCorner = false;
            _nextPathTime = 0f;
            _buffUntil = 0f;
            SpeedMultiplier = 1f;
            BaseScale = 1f;
            SetVisualScale(1f);
        }

        protected virtual void OnDisable()
        {
            Active.Remove(this);
            Health.Damaged -= OnDamaged;
            Health.Died -= OnDied;
        }

        void Update()
        {
            if (Health.IsDead) return;

            // Crossing another ant's pheromone trail gives a short burst of speed.
            if (Time.time >= _nextTrailCheck)
            {
                _nextTrailCheck = Time.time + 0.2f;
                if (PheromoneTrail.Inside(transform.position, this)) Buff(1f);
            }

            Think();
        }

        /// <summary>The behaviour's own logic, run every frame while alive.</summary>
        protected abstract void Think();

        public void Buff(float seconds)
        {
            _buffUntil = Mathf.Max(_buffUntil, Time.time + seconds);
        }

        public void RequestSummon(int count)
        {
            SummonRequested?.Invoke(this, count);
        }

        void OnDamaged(int amount, Vector3 direction)
        {
            direction.y = 0f;
            Knockback = direction.normalized * knockbackOnHit;
        }

        void OnDied()
        {
            Killed?.Invoke(this);
            StartCoroutine(DeathPop());
        }

        IEnumerator DeathPop()
        {
            const float duration = 0.15f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                SetVisualScale(Mathf.Lerp(1.2f, 0f, t / duration));
                yield return null;
            }
            gameObject.SetActive(false);
        }

        protected void SetVisualScale(float scale)
        {
            if (visual != null) visual.localScale = Vector3.one * (scale * BaseScale);
        }

        /// <summary>Moves by the behaviour's own velocity plus any knockback, keeping the enemy on the floor.</summary>
        protected void Move(Vector3 velocity)
        {
            Knockback = Vector3.MoveTowards(Knockback, Vector3.zero, knockbackDecay * Time.deltaTime);
            velocity += Knockback;
            velocity.y = -5f;
            Controller.Move(velocity * Time.deltaTime);
        }

        /// <summary>
        /// Where to head right now to reach the target: the next corner of a path around obstacles when the floor
        /// has a navmesh, or the target itself when it doesn't (the open test arena).
        /// </summary>
        protected Vector3 SteerToward(Vector3 target)
        {
            bool reachedCorner = _hasCorner && (transform.position - _corner).sqrMagnitude < 0.36f;
            if (Time.time >= _nextPathTime || reachedCorner)
            {
                _nextPathTime = Time.time + 0.25f;
                if (_path == null) _path = new NavMeshPath();
                _hasCorner = NavMesh.CalculatePath(transform.position, target, NavMesh.AllAreas, _path)
                             && _path.status != NavMeshPathStatus.PathInvalid
                             && _path.corners.Length > 1;
                if (_hasCorner) _corner = _path.corners[1];
            }
            return _hasCorner ? _corner : target;
        }

        protected void Face(Vector3 direction, float degreesPerSecond)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            var target = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * Time.deltaTime);
        }

        /// <summary>True when no wall stands between this enemy and the point. Other characters don't count as walls.</summary>
        protected bool CanSee(Vector3 point)
        {
            Vector3 from = transform.position + Vector3.up * 0.7f;
            Vector3 to = point + Vector3.up * 0.7f;
            if (!Physics.Linecast(from, to, out var hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.collider.GetComponentInParent<Health>() != null;
        }
    }
}
