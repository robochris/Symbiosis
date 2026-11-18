using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Something on the floor that hurts whoever stands in it: a puddle that lingers, or a blast that goes off after a short fuse.
    /// Enemy hazards hurt only the player; the player's own puddles hurt only enemies. Anything airborne is untouched.
    /// </summary>
    public class GroundHazard : MonoBehaviour
    {
        public static readonly List<GroundHazard> All = new List<GroundHazard>();

        [Tooltip("Who it hurts.")]
        public Team hurts = Team.Player;
        public float radius = 1.5f;
        public int damage = 1;
        [Tooltip("Seconds it stays. For a blast, how long the flash lingers after it goes off.")]
        public float lifetime = 5f;
        [Tooltip("Blasts only: seconds before it goes off.")]
        public float fuse = 0f;
        public bool isBlast;
        [Tooltip("Blasts only: a disc on the floor instead of a ball.")]
        public bool flat;
        [Tooltip("Also sets what it hurts on fire.")]
        public bool burning;
        [Tooltip("Seconds between hits on enemies standing in it.")]
        public float tick = 0.5f;

        float _age;
        float _nextTick;
        bool _wentOff;

        /// <summary>An enemy's acid: a flat pool that hurts the player while they stand in it.</summary>
        public static GroundHazard SpawnAcidPool(Vector3 position, Material material)
        {
            var hazard = Create(PrimitiveType.Cylinder, "Acid Pool", Flat(position, 0.03f), material);
            hazard.radius = 1.6f;
            hazard.lifetime = 5f;
            hazard.transform.localScale = new Vector3(hazard.radius * 2f, 0.02f, hazard.radius * 2f);
            return hazard;
        }

        /// <summary>A ball that swells for a moment, then hurts the player if they are still inside it.</summary>
        public static GroundHazard SpawnExplosion(Vector3 position, Material material)
        {
            var hazard = Create(PrimitiveType.Sphere, "Explosion", Flat(position, 0.6f), material);
            hazard.isBlast = true;
            hazard.radius = 3f;
            hazard.fuse = 0.7f;
            hazard.lifetime = 0.15f;
            hazard.transform.localScale = Vector3.one * 0.6f;
            return hazard;
        }

        /// <summary>A flat warning disc that fills in, then hurts the player if they are still on it. For boss attacks.</summary>
        public static GroundHazard SpawnBlast(Vector3 position, float radius, float fuse, Material material)
        {
            var hazard = Create(PrimitiveType.Cylinder, "Blast", Flat(position, 0.05f), material);
            hazard.isBlast = true;
            hazard.flat = true;
            hazard.radius = radius;
            hazard.fuse = fuse;
            hazard.lifetime = 0.2f;
            hazard.transform.localScale = new Vector3(0.5f, 0.02f, 0.5f);
            return hazard;
        }

        /// <summary>The player's own puddle (Acid Gland): hurts enemies standing in it. Burning puddles also set them alight.</summary>
        public static GroundHazard SpawnFriendlyPuddle(Vector3 position, int level, bool burning, Material material)
        {
            var hazard = Create(PrimitiveType.Cylinder, burning ? "Burning Puddle" : "Acid Puddle", Flat(position, 0.04f), material);
            hazard.hurts = Team.Enemy;
            hazard.burning = burning;
            hazard.radius = 0.9f + 0.3f * level;
            hazard.lifetime = 3f;
            hazard.transform.localScale = new Vector3(hazard.radius * 2f, 0.02f, hazard.radius * 2f);
            return hazard;
        }

        static Vector3 Flat(Vector3 position, float height) => new Vector3(position.x, height, position.z);

        static GroundHazard Create(PrimitiveType shape, string name, Vector3 position, Material material)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            go.transform.position = position;
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            return go.AddComponent<GroundHazard>();
        }

        /// <summary>Removes every hazard, e.g. when a new floor is built.</summary>
        public static void ClearAll()
        {
            foreach (var hazard in All.ToArray()) if (hazard != null) Destroy(hazard.gameObject);
            All.Clear();
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            _age += Time.deltaTime;

            if (isBlast)
            {
                if (!_wentOff)
                {
                    // Swell toward full size so the danger area is readable before it lands.
                    float size = Mathf.Lerp(0.6f, radius * 2f, _age / fuse);
                    transform.localScale = flat ? new Vector3(size, 0.02f, size) : Vector3.one * size;
                    if (_age >= fuse)
                    {
                        _wentOff = true;
                        Sfx.Play(SfxId.Blast);
                        Hurt();
                    }
                }
                else if (_age >= fuse + lifetime)
                {
                    Destroy(gameObject);
                }
                return;
            }

            if (hurts == Team.Player)
            {
                // The player's own post-hit invulnerability sets how often a pool can hurt them.
                Hurt();
            }
            else if (Time.time >= _nextTick)
            {
                _nextTick = Time.time + tick;
                Hurt();
            }

            if (_age >= lifetime) Destroy(gameObject);
        }

        bool Inside(Vector3 position)
        {
            Vector3 to = position - transform.position;
            to.y = 0f;
            return to.sqrMagnitude <= radius * radius;
        }

        void Hurt()
        {
            if (hurts == Team.Player)
            {
                var player = PlayerController.Instance;
                if (player == null || player.Health.IsDead || player.IsHovering) return;
                if (Inside(player.transform.position)) player.Health.TakeDamage(damage, Vector3.zero);
                return;
            }

            // Copy: a death can change the list while we walk it.
            foreach (var enemy in Enemy.Active.ToArray())
            {
                if (enemy.Health.IsDead || enemy.IgnoresGroundHazards || !Inside(enemy.transform.position)) continue;
                enemy.Health.TakeDamage(damage, Vector3.zero);
                if (burning) enemy.Ignite(1);
            }
        }
    }
}
