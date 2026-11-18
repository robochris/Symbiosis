using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>Patches left behind by an ant with the Pheromone Trail mutation. Any other ant standing on one moves faster.</summary>
    public class PheromoneTrail : MonoBehaviour
    {
        const float Radius = 1.3f;
        const float Lifetime = 4f;

        static readonly List<PheromoneTrail> All = new List<PheromoneTrail>();

        Enemy _owner;
        float _age;

        public static void Drop(Vector3 position, Enemy owner, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Pheromone";
            var collider = go.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            go.transform.position = new Vector3(position.x, 0.02f, position.z);
            go.transform.localScale = new Vector3(Radius * 2f, 0.01f, Radius * 2f);
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            go.AddComponent<PheromoneTrail>()._owner = owner;
        }

        /// <summary>True when the position is on a patch left by a different ant.</summary>
        public static bool Inside(Vector3 position, Enemy asking)
        {
            foreach (var patch in All)
            {
                if (patch._owner == asking) continue;
                Vector3 to = position - patch.transform.position;
                to.y = 0f;
                if (to.sqrMagnitude <= Radius * Radius) return true;
            }
            return false;
        }

        public static void ClearAll()
        {
            foreach (var patch in All.ToArray()) if (patch != null) Destroy(patch.gameObject);
            All.Clear();
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            _age += Time.deltaTime;

            // Shrinks as it fades, so an old trail reads as weaker.
            float size = Radius * 2f * Mathf.Lerp(1f, 0.4f, _age / Lifetime);
            transform.localScale = new Vector3(size, 0.01f, size);
            if (_age >= Lifetime) Destroy(gameObject);
        }
    }
}
