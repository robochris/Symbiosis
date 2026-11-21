using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// A ring along the floor that spreads out from where it started. Its edge hurts the player as it passes; inside
    /// and outside it are safe. A dodge carries straight through the edge. The Rubber Duck's squeak.
    /// </summary>
    public class RingWave : MonoBehaviour
    {
        public static readonly List<RingWave> All = new List<RingWave>();
        /// <summary>Raised when a ring's edge catches the player, for whoever made it to keep count.</summary>
        public static event System.Action<RingWave> Caught;

        public float speed = 6f;
        public float maxRadius = 16f;
        public float width = 0.8f;
        public int damage = 1;
        public Object source;

        public float Radius { get; private set; }
        /// <summary>True once its edge has caught the player.</summary>
        public bool Hit { get; private set; }

        LineRenderer _line;
        const int Segments = 48;

        public static RingWave Spawn(Vector3 centre, float speed, float maxRadius, Material material, Object source)
        {
            var go = new GameObject("Ring Wave");
            go.transform.position = new Vector3(centre.x, 0.08f, centre.z);
            var ring = go.AddComponent<RingWave>();
            ring.speed = speed;
            ring.maxRadius = maxRadius;
            ring.source = source;
            ring._line = go.AddComponent<LineRenderer>();
            ring._line.loop = true;
            ring._line.positionCount = Segments;
            ring._line.useWorldSpace = false;
            ring._line.widthMultiplier = ring.width;
            ring._line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (material != null) ring._line.sharedMaterial = material;
            ring.Draw();
            return ring;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            Radius += speed * Time.deltaTime * Enemy.WorldSlow;
            Draw();
            if (!Hit) Catch();
            if (Radius >= maxRadius) Destroy(gameObject);
        }

        void Draw()
        {
            if (_line == null) return;
            for (int i = 0; i < Segments; i++)
            {
                float a = i * Mathf.PI * 2f / Segments;
                _line.SetPosition(i, new Vector3(Mathf.Cos(a) * Radius, 0f, Mathf.Sin(a) * Radius));
            }
        }

        void Catch()
        {
            var player = PlayerController.Instance;
            if (player == null || player.Health.IsDead || player.IsDodging) return;
            Vector3 to = player.transform.position - transform.position;
            to.y = 0f;
            if (Mathf.Abs(to.magnitude - Radius) > width * 0.5f + 0.35f) return;
            if (player.Health.TakeDamage(damage, to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward))
            {
                Hit = true;
                Caught?.Invoke(this);
            }
        }

        public static void ClearAll()
        {
            foreach (var ring in All.ToArray()) if (ring != null) Destroy(ring.gameObject);
            All.Clear();
        }
    }
}
