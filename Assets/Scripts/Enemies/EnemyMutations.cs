using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Gives one enemy its mutations: changes its stats, reacts to damage and death, and shows the tells.
    /// Every mutation is read the same way: a coloured badge floating above the enemy and a body tint in that colour,
    /// plus a shape or motion for the ones that need it (a face plate, flashing, pulsing, a trail).
    /// </summary>
    [RequireComponent(typeof(Enemy), typeof(Health))]
    public class EnemyMutations : MonoBehaviour
    {
        [Tooltip("Height of the row of badges above the enemy.")]
        public float badgeHeight = 2.3f;
        [Tooltip("How far in front of the body the Hardened Shell plate sits.")]
        public float shellOffset = 1.1f;

        public IReadOnlyList<MutationDefinition> Active => _active;

        readonly List<MutationDefinition> _active = new List<MutationDefinition>();
        readonly List<GameObject> _tells = new List<GameObject>();
        Enemy _enemy;
        Health _health;
        HitFlash _flash;
        Color _tint;
        bool _summoned;
        float _nextRegen;
        float _nextTrail;

        void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _health = GetComponent<Health>();
            _flash = GetComponent<HitFlash>();
        }

        void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
            Clear();
        }

        public bool Has(MutationType type) => Find(type) != null;

        MutationDefinition Find(MutationType type)
        {
            foreach (var mutation in _active) if (mutation.type == type) return mutation;
            return null;
        }

        public int TotalCost
        {
            get
            {
                int cost = 0;
                foreach (var mutation in _active) cost += mutation.cost;
                return cost;
            }
        }

        void Clear()
        {
            foreach (var tell in _tells) if (tell != null) Destroy(tell);
            _tells.Clear();
            _active.Clear();
            _summoned = false;
        }

        /// <summary>Call right after the enemy is switched on. Replaces any mutations it had.</summary>
        public void Apply(IList<MutationDefinition> mutations)
        {
            Clear();
            if (mutations == null || mutations.Count == 0) return;

            Vector3 colourSum = Vector3.zero;
            float scale = 1f;

            foreach (var mutation in mutations)
            {
                if (mutation == null || Has(mutation.type)) continue;
                _active.Add(mutation);
                colourSum += new Vector3(mutation.color.r, mutation.color.g, mutation.color.b);

                switch (mutation.type)
                {
                    case MutationType.Fast:
                        _enemy.SpeedMultiplier = 1.5f;
                        break;
                    case MutationType.Armored:
                        _health.damageMultiplier = 0.5f;
                        scale *= 1.1f;
                        break;
                    case MutationType.ExplodesOnDeath:
                        scale *= 1.2f;
                        break;
                    case MutationType.HardenedShell:
                        // A shot is blocked when it is travelling against the way the enemy faces: it came from the front.
                        _health.ShouldBlock = direction =>
                        {
                            direction.y = 0f;
                            return Vector3.Dot(direction.normalized, transform.forward) < -0.35f;
                        };
                        AddShell(mutation);
                        break;
                }
            }

            if (_active.Count == 0) return;
            _enemy.BaseScale = scale;
            _enemy.visual.localScale = Vector3.one * scale;

            Vector3 average = colourSum / _active.Count;
            _tint = new Color(average.x, average.y, average.z);
            if (_flash != null) _flash.SetTint(_tint, 0.55f);

            AddBadges();
            _nextRegen = Time.time + 0.5f;
            _nextTrail = Time.time;
        }

        // One small coloured ball per mutation, in a row above the enemy.
        void AddBadges()
        {
            const float spacing = 0.42f;
            float start = -(_active.Count - 1) * spacing * 0.5f;
            for (int i = 0; i < _active.Count; i++)
            {
                var badge = MakeTell(PrimitiveType.Sphere, _active[i].displayName + " badge", _active[i].effectMaterial);
                badge.transform.localPosition = new Vector3(start + i * spacing, badgeHeight, 0f);
                badge.transform.localScale = Vector3.one * 0.3f;
            }
        }

        void AddShell(MutationDefinition mutation)
        {
            var plate = MakeTell(PrimitiveType.Cube, "Shell plate", mutation.effectMaterial);
            plate.transform.localPosition = new Vector3(0f, 0.75f, shellOffset);
            plate.transform.localScale = new Vector3(1.3f, 1.1f, 0.15f);
        }

        GameObject MakeTell(PrimitiveType shape, string name, Material material)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            go.transform.SetParent(transform, false);
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            _tells.Add(go);
            return go;
        }

        void Update()
        {
            if (_active.Count == 0 || _health.IsDead) return;

            // Regenerates: heals once it has gone 2 seconds without being hit.
            if (Has(MutationType.Regenerates) && Time.time >= _nextRegen)
            {
                _nextRegen = Time.time + 0.5f;
                if (Time.time - _health.LastDamageTime >= 2f) _health.Heal(1);
            }

            // Pheromone trail: leaves a patch behind it that speeds up any other ant that crosses it.
            var pheromone = Find(MutationType.PheromoneTrail);
            if (pheromone != null && Time.time >= _nextTrail)
            {
                _nextTrail = Time.time + 0.4f;
                PheromoneTrail.Drop(transform.position, _enemy, pheromone.effectMaterial);
            }

            // Moving tells: about-to-explode flashes red, regenerating pulses.
            if (_flash == null) return;
            if (Has(MutationType.ExplodesOnDeath))
            {
                _flash.SetTint(Color.Lerp(_tint, Color.red, Mathf.PingPong(Time.time * 5f, 1f)), 0.7f);
            }
            else if (Has(MutationType.Regenerates))
            {
                _flash.SetTint(_tint, 0.3f + 0.45f * Mathf.PingPong(Time.time * 2f, 1f));
            }
        }

        void OnDamaged(int amount, Vector3 direction)
        {
            // Swarm call: once, at half health, call in three workers.
            if (!_summoned && !_health.IsDead && Has(MutationType.SwarmCall) && _health.Current <= _health.maxHealth / 2)
            {
                _summoned = true;
                _enemy.RequestSummon(3);
            }
        }

        void OnDied()
        {
            var explode = Find(MutationType.ExplodesOnDeath);
            if (explode != null) GroundHazard.SpawnExplosion(transform.position, explode.effectMaterial);

            var acid = Find(MutationType.AcidBlood);
            if (acid != null) GroundHazard.SpawnAcidPool(transform.position, acid.effectMaterial);
        }
    }
}
