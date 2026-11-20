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
        /// <summary>An enemy has lost health: (which one, how much).</summary>
        public static event Action<Enemy, int> Hurt;
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
        [Tooltip("When this enemy drops an item, it is usually one of these: hunting a kind of enemy is a way to fish for a kind of item.")]
        public ItemType[] favouredDrops;

        public Health Health { get; private set; }
        public EnemyMutations Mutations { get; private set; }
        /// <summary>Lasting speed change from mutations (Fast = 1.5).</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        /// <summary>Lasting size change from mutations.</summary>
        public float BaseScale { get; set; } = 1f;
        /// <summary>False while it can't be shot at all (a burrowed boss). Aim assist and companions skip it.</summary>
        public bool Targetable { get; protected set; } = true;
        /// <summary>The pick of the room: bigger, much tougher, carrying a full load of mutations, and worth more.</summary>
        public bool IsElite { get; private set; }
        public bool IsBuffed => Time.time < _buffUntil;
        public bool IsBurning => Time.time < _burnUntil;
        /// <summary>Confused enemies attack the nearest other enemy instead of the player.</summary>
        public bool IsConfused => Time.time < _confusedUntil;
        /// <summary>Airborne enemies are not hurt by puddles on the floor.</summary>
        public virtual bool IgnoresGroundHazards => false;
        /// <summary>Everything that changes how fast this enemy moves and attacks, multiplied together.</summary>
        public float SpeedScale => SpeedMultiplier * (IsBuffed ? 1.4f : 1f) * (IsChilled ? ChilledSpeed : 1f) * WorldSlow;
        /// <summary>Chilled by an Ice Cube shot: slower in everything it does until it thaws.</summary>
        public bool IsChilled => Time.time < _chilledUntil;
        public const float ChilledSpeed = 0.6f;
        float _chilledUntil;
        GameObject _chilledTell;

        public void Chill(float seconds)
        {
            _chilledUntil = Mathf.Max(_chilledUntil, Time.time + seconds);
        }
        /// <summary>
        /// Slows every enemy and every enemy shot at once: 1 is normal, 0.3 is a crawl. Freeze Frame sets it and
        /// puts it back.
        /// </summary>
        public static float WorldSlow = 1f;

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
        GameObject _crown;
        int _plainMaxHealth;

        public const float EliteHealth = 2.5f;
        public const float EliteSize = 1.3f;

        /// <summary>Turns this enemy into the room's elite. Call after its mutations are applied. It wears a crown so it can be picked out.</summary>
        public void MakeElite(Material crownMaterial)
        {
            if (IsElite) return;
            IsElite = true;
            _plainMaxHealth = Health.maxHealth;
            Health.SetMaxHealth(Mathf.CeilToInt(Health.maxHealth * EliteHealth), true);
            BaseScale *= EliteSize;
            SetVisualScale(1f);

            float height = Mutations != null ? Mutations.badgeHeight + 0.55f : 2.8f;
            _crown = new GameObject("Elite crown");
            _crown.transform.SetParent(transform, false);
            _crown.transform.localPosition = new Vector3(0f, height, 0f);
            CrownPart(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.7f, 0.07f, 0.7f), crownMaterial);
            for (int i = 0; i < 5; i++)
            {
                Vector3 at = Quaternion.Euler(0f, i * 72f, 0f) * new Vector3(0.3f, 0.17f, 0f);
                CrownPart(PrimitiveType.Cube, at, new Vector3(0.12f, 0.24f, 0.12f), crownMaterial);
            }
        }

        void CrownPart(PrimitiveType shape, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(shape);
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            part.transform.SetParent(_crown.transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
        }

        protected virtual void Awake()
        {
            Health = GetComponent<Health>();
            Controller = GetComponent<CharacterController>();
            // Every floor is flat, so nothing needs to climb. Left at the defaults, an enemy backing away from the
            // player could walk up the side of a pebble and stand on top of it, where no shot reaches.
            Controller.slopeLimit = 5f;
            Controller.stepOffset = Mathf.Min(Controller.stepOffset, 0.08f);
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
            _chilledUntil = 0f;
            if (_chilledTell != null) _chilledTell.SetActive(false);
            _burnUntil = 0f;
            _confusedUntil = 0f;
            if (_fireTell != null) _fireTell.SetActive(false);
            if (_confusedTell != null) _confusedTell.SetActive(false);
            SpeedMultiplier = 1f;
            BaseScale = 1f;
            Targetable = true;
            SetVisualScale(1f);
            HideChargeLine();
        }

        protected virtual void OnDisable()
        {
            // Back to a plain ant before it goes into the pool.
            if (IsElite)
            {
                IsElite = false;
                Health.maxHealth = _plainMaxHealth;
                if (_crown != null) Destroy(_crown);
            }
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
            if (_crown != null) _crown.transform.Rotate(0f, 90f * Time.deltaTime, 0f);
            ShowTell(ref _fireTell, IsBurning, 0f);
            ShowTell(ref _confusedTell, IsConfused, 0.5f);
            ShowTell(ref _chilledTell, IsChilled, -0.5f);

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

        // A small coloured ball over the enemy while a status lasts: orange for fire, pink for confused, pale blue for chilled.
        void ShowTell(ref GameObject tell, bool show, float sideways)
        {
            if (tell == null)
            {
                if (!show || GameAssets.Instance == null) return;
                tell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tell.name = sideways > 0f ? "Confused" : sideways < 0f ? "Chilled" : "On fire";
                var collider = tell.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
                tell.transform.SetParent(transform, false);
                tell.transform.localPosition = new Vector3(sideways, 1.6f, 0f);
                tell.transform.localScale = Vector3.one * 0.45f;
                tell.GetComponent<Renderer>().sharedMaterial = sideways > 0f ? GameAssets.Instance.confused : sideways < 0f ? GameAssets.Instance.chilled : GameAssets.Instance.fire;
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
            Sfx.Play(SfxId.Hit, 0.7f);
            if (amount > 0) Hurt?.Invoke(this, amount);
        }

        void OnDied()
        {
            Killed?.Invoke(this);
            OnDeath();
        }

        /// <summary>What it looks and sounds like when this enemy is beaten. Most burst and are gone.</summary>
        protected virtual void OnDeath()
        {
            Sfx.Play(SfxId.EnemyDie);
            float size = Controller != null ? Controller.radius / 0.55f : 1f;
            Fx.Burst(transform.position + Vector3.up * (0.6f * size), Fx.ColourOf(visual), Mathf.Min(40, Mathf.RoundToInt(12 * size)), 5f + size, 0.22f * Mathf.Sqrt(size), 0.55f);
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

        GameObject _chargeLine;

        /// <summary>True while a warning strip is showing on the floor in front of this enemy.</summary>
        public bool ChargeLineShowing => _chargeLine != null && _chargeLine.activeSelf;
        /// <summary>How long the strip is right now, in metres.</summary>
        public float ChargeLineLength => ChargeLineShowing ? _chargeLine.transform.localScale.z : 0f;

        /// <summary>
        /// The warning for a charge: a strip on the floor that grows out in front, along the line the charge will take,
        /// stopping at the first wall. Call every frame of the wind-up with how far through it is (0 to 1).
        /// </summary>
        protected void ShowChargeLine(float progress, float length, float width, Material material)
        {
            if (_chargeLine == null)
            {
                _chargeLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _chargeLine.name = "Charge line";
                var collider = _chargeLine.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
                _chargeLine.transform.SetParent(transform, false);
                var renderer = _chargeLine.GetComponent<Renderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (material != null) renderer.sharedMaterial = material;
            }

            Vector3 from = transform.position + Vector3.up * 0.5f;
            int count = Physics.RaycastNonAlloc(from, transform.forward, LineHits, length, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (LineHits[i].collider.GetComponentInParent<Health>() == null) length = Mathf.Min(length, LineHits[i].distance);
            }

            float shown = Mathf.Max(0.1f, length * Mathf.Clamp01(progress));
            _chargeLine.transform.localPosition = new Vector3(0f, 0.04f - transform.position.y, shown * 0.5f);
            _chargeLine.transform.localScale = new Vector3(width, 0.02f, shown);
            if (!_chargeLine.activeSelf) _chargeLine.SetActive(true);
        }

        protected void HideChargeLine()
        {
            if (_chargeLine != null && _chargeLine.activeSelf) _chargeLine.SetActive(false);
        }

        static readonly RaycastHit[] LineHits = new RaycastHit[12];

        protected void SetVisualScale(float scale)
        {
            if (visual != null) visual.localScale = Vector3.one * (scale * BaseScale);
        }

        /// <summary>Moves by the behaviour's own velocity plus any knockback, keeping the enemy on the floor.</summary>
        protected void Move(Vector3 velocity)
        {
            Knockback = Vector3.MoveTowards(Knockback, Vector3.zero, knockbackDecay * Time.deltaTime);
            // Freeze Frame: whatever it meant to do, it does it at a crawl. Being knocked back is not slowed.
            velocity *= WorldSlow;
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
