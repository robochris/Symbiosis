using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Something on the floor that hurts the player: an acid pool that lingers, or a blast that goes off after a short fuse.
    /// Only the player is affected; enemies walk (or fly) through.
    /// </summary>
    public class GroundHazard : MonoBehaviour
    {
        public static readonly List<GroundHazard> All = new List<GroundHazard>();

        public float radius = 1.5f;
        public int damage = 1;
        [Tooltip("Seconds it stays. For a blast, how long the flash lingers after it goes off.")]
        public float lifetime = 5f;
        [Tooltip("Blasts only: seconds before it goes off.")]
        public float fuse = 0f;
        public bool isBlast;

        float _age;
        bool _wentOff;

        /// <summary>A flat pool that hurts the player while they stand in it.</summary>
        public static GroundHazard SpawnAcidPool(Vector3 position, Material material)
        {
            var hazard = Create(PrimitiveType.Cylinder, "Acid Pool", position + Vector3.up * 0.03f, material);
            hazard.radius = 1.6f;
            hazard.lifetime = 5f;
            hazard.transform.localScale = new Vector3(hazard.radius * 2f, 0.02f, hazard.radius * 2f);
            return hazard;
        }

        /// <summary>A ball that swells for a moment, then hurts the player if they are still inside it.</summary>
        public static GroundHazard SpawnExplosion(Vector3 position, Material material)
        {
            var hazard = Create(PrimitiveType.Sphere, "Explosion", position + Vector3.up * 0.6f, material);
            hazard.isBlast = true;
            hazard.radius = 3f;
            hazard.fuse = 0.7f;
            hazard.lifetime = 0.15f;
            hazard.transform.localScale = Vector3.one * 0.6f;
            return hazard;
        }

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
                    transform.localScale = Vector3.one * size;
                    if (_age >= fuse)
                    {
                        _wentOff = true;
                        HurtPlayerInside();
                    }
                }
                else if (_age >= fuse + lifetime)
                {
                    Destroy(gameObject);
                }
                return;
            }

            // The player's own post-hit invulnerability sets how often a pool can hurt them.
            HurtPlayerInside();
            if (_age >= lifetime) Destroy(gameObject);
        }

        void HurtPlayerInside()
        {
            var player = PlayerController.Instance;
            if (player == null || player.Health.IsDead) return;
            Vector3 to = player.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude <= radius * radius) player.Health.TakeDamage(damage, to.normalized);
        }
    }
}
