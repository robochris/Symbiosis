using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Gives one enemy its mutations: changes its stats, reacts to damage and death, and shows the tells.
    /// Every mutation is read the same way: a coloured badge floating above the enemy and a body tint in that colour,
    /// plus its own look in that colour: speed lines, armour plates, a pulse on the ground, a swollen flashing body,
    /// drips, glowing antennae, a face plate, a trail.
    /// </summary>
    [RequireComponent(typeof(Enemy), typeof(Health))]
    public class EnemyMutations : MonoBehaviour
    {
        [Tooltip("Height of the row of badges above the enemy.")]
        public float badgeHeight = 2.3f;
        [Tooltip("How far in front of the body the Hardened Shell plate sits.")]
        public float shellOffset = 1.1f;
        [Tooltip("How big the body is next to a worker ant (1). The looks are sized and placed by it.")]
        public float bodySize = 1f;
        [Tooltip("Hardened Shell: shots from the front it stops before it shatters. Getting behind is still the quick way, but an enemy that always faces you (a spitter backing off) can never be untouchable.")]
        public int shellStrength = 8;

        /// <summary>Frontal shots the shell can still stop. 0 once it has shattered, or if there never was one.</summary>
        public int ShellLeft { get; private set; }
        GameObject _shellPlate;

        public IReadOnlyList<MutationDefinition> Active => _active;
        public const float SugarRushSpeed = 1.8f;
        public const float AcidBloodTime = 3.2f;
        public const int SequelCount = 2;
        public const float StaticInterval = 4f, StaticRadius = 2.7f, StaticFuse = 0.9f;
        /// <summary>How many times a Static enemy has gone off.</summary>
        public int Discharges { get; private set; }
        float _nextDischarge;
        /// <summary>A hardened shell has just been shot to bits.</summary>
        public static event System.Action ShellShattered;
        /// <summary>True once a Sugar Rush ant has dropped to half health and sped up.</summary>
        public bool IsRushing => _rushing;

        readonly List<MutationDefinition> _active = new List<MutationDefinition>();
        readonly List<GameObject> _tells = new List<GameObject>();
        readonly List<MovingTell> _moving = new List<MovingTell>();

        enum Motion { SpeedLine, Pulse, Drip, Glow }

        struct MovingTell
        {
            public Transform transform;
            public Motion motion;
            public Vector3 position, scale;
            public float phase;
        }
        Enemy _enemy;
        Health _health;
        HitFlash _flash;
        Color _tint;
        bool _summoned;
        bool _rushing;
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
            _health.Blocked += OnBlocked;
        }

        // The shell took one for the ant. Enough of those and it breaks for good.
        void OnBlocked(Vector3 direction)
        {
            if (ShellLeft <= 0) return;
            ShellLeft--;
            Sfx.Play(SfxId.Blocked, 0.5f);
            var shell = Find(MutationType.HardenedShell);
            Color colour = shell != null ? shell.color : Color.white;
            Vector3 face = transform.position + transform.forward * shellOffset + Vector3.up * 0.75f;
            if (ShellLeft > 0)
            {
                Fx.Burst(face, colour, 3, 3f, 0.1f, 0.25f);
                // It visibly wears down: thinner with every hit.
                if (_shellPlate != null) _shellPlate.transform.localScale = new Vector3(1.3f, 1.1f * (0.4f + 0.6f * ShellLeft / shellStrength), 0.15f);
                return;
            }

            _health.ShouldBlock = null;
            ShellShattered?.Invoke();
            if (_shellPlate != null) _shellPlate.SetActive(false);
            Fx.Burst(face, colour, 18, 6f, 0.2f, 0.5f);
        }

        void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
            _health.Blocked -= OnBlocked;
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
            _moving.Clear();
            _active.Clear();
            _summoned = false;
            _rushing = false;
            Discharges = 0;
            // Not the moment it walks in: the first one comes after a couple of seconds.
            _nextDischarge = Time.time + 2f;
            ShellLeft = 0;
            _shellPlate = null;
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
                        // Speed lines streaming out behind.
                        Moving(Motion.SpeedLine, BodyTell(PrimitiveType.Cube, "Speed line", mutation, new Vector3(-0.32f, 0.5f, -1.5f), new Vector3(0.05f, 0.05f, 0.7f)), 0f);
                        Moving(Motion.SpeedLine, BodyTell(PrimitiveType.Cube, "Speed line", mutation, new Vector3(0f, 0.85f, -1.6f), new Vector3(0.05f, 0.05f, 0.7f)), 0.33f);
                        Moving(Motion.SpeedLine, BodyTell(PrimitiveType.Cube, "Speed line", mutation, new Vector3(0.32f, 0.45f, -1.5f), new Vector3(0.05f, 0.05f, 0.7f)), 0.66f);
                        break;
                    case MutationType.Armored:
                        _health.damageMultiplier = 0.5f;
                        scale *= 1.1f;
                        // Plates over the back, the middle and the head.
                        BodyTell(PrimitiveType.Cube, "Armour plate", mutation, new Vector3(0f, 0.95f, -0.65f), new Vector3(0.62f, 0.12f, 0.75f));
                        BodyTell(PrimitiveType.Cube, "Armour plate", mutation, new Vector3(0f, 0.8f, 0f), new Vector3(0.44f, 0.1f, 0.46f));
                        BodyTell(PrimitiveType.Cube, "Armour plate", mutation, new Vector3(0f, 0.9f, 0.5f), new Vector3(0.44f, 0.1f, 0.4f));
                        break;
                    case MutationType.Regenerates:
                        // A ring on the ground that keeps spreading out from under it.
                        var pulse = MakeTell(PrimitiveType.Cylinder, "Regeneration pulse", mutation.effectMaterial);
                        pulse.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                        Moving(Motion.Pulse, pulse, 0f);
                        break;
                    case MutationType.AcidBlood:
                        Moving(Motion.Drip, BodyTell(PrimitiveType.Sphere, "Acid drip", mutation, new Vector3(-0.2f, 0.4f, -0.7f), Vector3.one * 0.13f), 0f);
                        Moving(Motion.Drip, BodyTell(PrimitiveType.Sphere, "Acid drip", mutation, new Vector3(0.15f, 0.4f, -0.2f), Vector3.one * 0.11f), 0.4f);
                        Moving(Motion.Drip, BodyTell(PrimitiveType.Sphere, "Acid drip", mutation, new Vector3(0.05f, 0.45f, 0.45f), Vector3.one * 0.12f), 0.75f);
                        break;
                    case MutationType.Sticky:
                        BodyTell(PrimitiveType.Sphere, "Sticky glob", mutation, new Vector3(-0.25f, 0.22f, -0.3f), new Vector3(0.45f, 0.3f, 0.45f));
                        BodyTell(PrimitiveType.Sphere, "Sticky glob", mutation, new Vector3(0.22f, 0.18f, 0.25f), new Vector3(0.38f, 0.26f, 0.38f));
                        break;
                    case MutationType.Static:
                        // Sparks that never stop.
                        Moving(Motion.Glow, BodyTell(PrimitiveType.Sphere, "Static spark", mutation, new Vector3(0.3f, 1.1f, 0.1f), Vector3.one * 0.14f), 0f);
                        Moving(Motion.Glow, BodyTell(PrimitiveType.Sphere, "Static spark", mutation, new Vector3(-0.28f, 0.9f, -0.25f), Vector3.one * 0.12f), 0.4f);
                        Moving(Motion.Glow, BodyTell(PrimitiveType.Sphere, "Static spark", mutation, new Vector3(0f, 1.25f, -0.1f), Vector3.one * 0.1f), 0.8f);
                        break;
                    case MutationType.SugarRush:
                        // Sparks that sit quietly until the rush starts, then never stop.
                        Moving(Motion.Glow, BodyTell(PrimitiveType.Sphere, "Sugar spark", mutation, new Vector3(-0.3f, 1.15f, 0f), Vector3.one * 0.16f), 0f);
                        Moving(Motion.Glow, BodyTell(PrimitiveType.Sphere, "Sugar spark", mutation, new Vector3(0.3f, 1.2f, -0.3f), Vector3.one * 0.14f), 0.5f);
                        break;
                    case MutationType.SwarmCall:
                        // Antennae with glowing tips.
                        for (int side = -1; side <= 1; side += 2)
                        {
                            var stalk = BodyTell(PrimitiveType.Cube, "Antenna", mutation, new Vector3(0.14f * side, 1.02f, 0.72f), new Vector3(0.04f, 0.4f, 0.04f));
                            stalk.transform.localRotation = Quaternion.Euler(35f, 0f, -18f * side);
                            Moving(Motion.Glow, BodyTell(PrimitiveType.Sphere, "Antenna glow", mutation, new Vector3(0.2f * side, 1.2f, 0.85f), Vector3.one * 0.2f), side * 0.25f);
                        }
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
                        ShellLeft = shellStrength;
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
            _shellPlate = plate;
            plate.transform.localPosition = new Vector3(0f, 0.75f, shellOffset);
            plate.transform.localScale = new Vector3(1.3f, 1.1f, 0.15f);
        }

        // A tell fixed to the body itself, placed and sized as if on a worker ant: it hovers, swells and shrinks with the body.
        GameObject BodyTell(PrimitiveType shape, string name, MutationDefinition mutation, Vector3 position, Vector3 size)
        {
            var go = MakeTell(shape, name, mutation.effectMaterial);
            if (_enemy.visual != null) go.transform.SetParent(_enemy.visual, false);
            go.transform.localPosition = position * bodySize;
            go.transform.localScale = size * bodySize;
            return go;
        }

        void Moving(Motion motion, GameObject tell, float phase)
        {
            _moving.Add(new MovingTell
            {
                transform = tell.transform, motion = motion, phase = phase,
                position = tell.transform.localPosition, scale = tell.transform.localScale
            });
        }

        void Animate()
        {
            foreach (var tell in _moving)
            {
                if (tell.transform == null) continue;
                float rate = tell.motion == Motion.SpeedLine ? 2.5f : tell.motion == Motion.Glow ? 1.5f : 0.9f;
                float t = Mathf.Repeat(Time.time * rate + tell.phase, 1f);
                switch (tell.motion)
                {
                    case Motion.SpeedLine:
                        // Shoots backward and thins out, over and over.
                        tell.transform.localPosition = tell.position + Vector3.back * (t * 0.6f * bodySize);
                        tell.transform.localScale = new Vector3(tell.scale.x, tell.scale.y, tell.scale.z * (1.2f - t));
                        break;
                    case Motion.Pulse:
                        float width = Mathf.Lerp(0.8f, 3.2f, t) * bodySize;
                        tell.transform.localScale = new Vector3(width, Mathf.Lerp(0.02f, 0.002f, t), width);
                        break;
                    case Motion.Drip:
                        // Hangs, falls to the floor, starts again.
                        tell.transform.localPosition = new Vector3(tell.position.x, Mathf.Lerp(tell.position.y, 0.05f, t * t), tell.position.z);
                        break;
                    case Motion.Glow:
                        tell.transform.localScale = tell.scale * (1f + 0.35f * Mathf.Sin(t * Mathf.PI * 2f));
                        break;
                }
            }
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

            // Static: every few seconds a disc fills in round it and then goes off. Keep your distance, or time it.
            var charge = Find(MutationType.Static);
            if (charge != null && Time.time >= _nextDischarge)
            {
                _nextDischarge = Time.time + StaticInterval;
                Discharges++;
                GroundHazard.SpawnBlast(transform.position, StaticRadius, StaticFuse, charge.effectMaterial);
            }

            Animate();

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
            // Sugar rush: once it is down to half health, it speeds right up for the rest of its life.
            if (!_rushing && !_health.IsDead && Has(MutationType.SugarRush) && _health.Current <= _health.maxHealth / 2)
            {
                _rushing = true;
                _enemy.SpeedMultiplier *= SugarRushSpeed;
                Fx.Burst(transform.position + Vector3.up, Find(MutationType.SugarRush).color, 12, 5f, 0.18f);
            }

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
            // A shorter-lived pool than a boss leaves: measured with a bot, these were most of what made the ants'
            // late floors twice as punishing as any other biome's.
            if (acid != null) GroundHazard.SpawnAcidPool(transform.position, acid.effectMaterial).lifetime = AcidBloodTime;

            // A sequel: two more of the world's swarm, where this one fell. They are plain, so it ends there.
            if (Has(MutationType.Sequel)) _enemy.RequestSummon(SequelCount);

            var sticky = Find(MutationType.Sticky);
            if (sticky != null) GroundHazard.SpawnStickyPatch(transform.position, sticky.effectMaterial);
        }
    }
}
