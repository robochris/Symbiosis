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
        /// <summary>
        /// An enemy wants another enemy put into the fight: (who asked, which prefab or null for a random one from
        /// the biome, where, whether it should roll mutations).
        /// </summary>
        public static event Action<Enemy, Enemy, Vector3, bool> MinionRequested;

        [Tooltip("The body; scaled for telegraphs and the death pop.")]
        public Transform visual;
        public float knockbackOnHit = 5f;
        public float knockbackDecay = 14f;
        [Tooltip("Stands still and never attacks, but can still be hit, burned and killed. For target practice and tests.")]
        public bool frozen;
        [Tooltip("Off for things that would be farmed otherwise: larvae and eggs.")]
        public bool dropsLoot = true;

        public Health Health { get; private set; }
        public EnemyMutations Mutations { get; private set; }
        /// <summary>Lasting speed change from mutations (Fast = 1.5).</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        /// <summary>Lasting size change from mutations.</summary>
        public float BaseScale { get; set; } = 1f;
        /// <summary>False while it can't be shot at all (a burrowed boss). Aim assist and companions skip it.</summary>
        public bool Targetable { get; protected set; } = true;
        public bool IsBuffed => Time.time < _buffUntil;
        public bool IsBurning => Time.time < _burnUntil;
        /// <summary>Confused enemies attack the nearest other enemy instead of the player.</summary>
        public bool IsConfused => Time.time < _confusedUntil;
        /// <summary>Airborne enemies are not hurt by puddles on the floor.</summary>
        public virtual bool IgnoresGroundHazards => false;
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
        float _burnUntil;
        float _nextBurnTick;
        float _confusedUntil;
        GameObject _fireTell;
        GameObject _confusedTell;

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
            _burnUntil = 0f;
            _confusedUntil = 0f;
            if (_fireTell != null) _fireTell.SetActive(false);
            if (_confusedTell != null) _confusedTell.SetActive(false);
            SpeedMultiplier = 1f;
            BaseScale = 1f;
            Targetable = true;
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

            // Burning: a point of damage every half second until it goes out.
            if (IsBurning && Time.time >= _nextBurnTick)
            {
                _nextBurnTick = Time.time + 0.5f;
                Health.TakeDamage(1, Vector3.zero);
                if (Health.IsDead) return;
            }
            ShowTell(ref _fireTell, IsBurning, 0f);
            ShowTell(ref _confusedTell, IsConfused, 0.5f);

            if (frozen)
            {
                Move(Vector3.zero);
                return;
            }
            Think();
        }

        /// <summary>Sets this enemy on fire. Higher levels burn longer.</summary>
        public void Ignite(int level)
        {
            if (!IsBurning) _nextBurnTick = Time.time + 0.5f;
            _burnUntil = Mathf.Max(_burnUntil, Time.time + 1.6f + level);
        }

        public void Confuse(float seconds)
        {
            _confusedUntil = Mathf.Max(_confusedUntil, Time.time + seconds);
        }

        /// <summary>
        /// Who this enemy is after right now: normally the player, but the nearest other enemy while confused.
        /// Returns false when there is nobody to go for.
        /// </summary>
        protected bool TryGetTarget(out Vector3 position, out Health health)
        {
            if (IsConfused)
            {
                Enemy nearest = null;
                float best = float.MaxValue;
                foreach (var other in Active)
                {
                    if (other == this || other.Health.IsDead) continue;
                    float distance = (other.transform.position - transform.position).sqrMagnitude;
                    if (distance < best) { best = distance; nearest = other; }
                }
                if (nearest != null)
                {
                    position = nearest.transform.position;
                    health = nearest.Health;
                    return true;
                }
            }

            var player = PlayerController.Instance;
            if (player == null || player.Health.IsDead)
            {
                position = default;
                health = null;
                return false;
            }
            position = player.transform.position;
            health = player.Health;
            return true;
        }

        Health _reachTarget;
        float _reachBonus;

        /// <summary>
        /// Extra reach against a target fatter than the player. Bite ranges are tuned for the player's size;
        /// without this a confused ant could never get close enough to bite a big Soldier.
        /// </summary>
        protected float ReachBonus(Health target)
        {
            if (target != _reachTarget)
            {
                _reachTarget = target;
                var body = target != null ? target.GetComponent<CharacterController>() : null;
                _reachBonus = body != null ? Mathf.Max(0f, body.radius - 0.4f) : 0f;
            }
            return _reachBonus;
        }

        // A small coloured ball over the enemy while a status lasts: orange for fire, pink for confused.
        void ShowTell(ref GameObject tell, bool show, float sideways)
        {
            if (tell == null)
            {
                if (!show || GameAssets.Instance == null) return;
                tell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tell.name = sideways > 0f ? "Confused" : "On fire";
                var collider = tell.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
                tell.transform.SetParent(transform, false);
                tell.transform.localPosition = new Vector3(sideways, 1.6f, 0f);
                tell.transform.localScale = Vector3.one * 0.45f;
                tell.GetComponent<Renderer>().sharedMaterial = sideways > 0f ? GameAssets.Instance.confused : GameAssets.Instance.fire;
            }
            if (tell.activeSelf != show) tell.SetActive(show);
        }

        /// <summary>The behaviour's own logic, run every frame while alive.</summary>
        protected abstract void Think();

        public void Buff(float seconds)
        {
            _buffUntil = Mathf.Max(_buffUntil, Time.time + seconds);
        }

        protected void RequestMinion(Enemy prefab, Vector3 position, bool mutate)
        {
            MinionRequested?.Invoke(this, prefab, position, mutate);
        }

        /// <summary>Leaves the fight without dying: no loot, no kill counted.</summary>
        public void Vanish()
        {
            gameObject.SetActive(false);
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
