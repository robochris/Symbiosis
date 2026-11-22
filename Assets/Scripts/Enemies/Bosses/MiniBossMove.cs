using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// A mini-boss's own move, on top of what its kind does, so it is a creature of its own and not a smaller copy of
    /// the boss at the end of its world. Trail: it leaves sticky (or burning) patches where it goes. Burst: a ring of
    /// shots all round, every so often. Shockwave: marks in a ring round itself that go off a moment later. Hem: a ring of
    /// marks round the player, with one gap to step out through.
    /// </summary>
    public class MiniBossMove : MonoBehaviour
    {
        public enum Kind { Trail, Burst, Shockwave, Hem }

        public Kind kind;
        public float every = 6f;
        public Material material;
        public Bullet projectilePrefab;
        public int shots = 10;
        public float shotSpeed = 7f;
        public bool trailHurts;

        public int Uses { get; private set; }

        static readonly Dictionary<Bullet, Pool<Bullet>> Pools = new Dictionary<Bullet, Pool<Bullet>>();
        Enemy _enemy;
        float _next, _nextDrop;

        void Awake() => _enemy = GetComponent<Enemy>();

        void OnEnable()
        {
            _next = Time.time + every * 0.6f;
            Uses = 0;
        }

        void Update()
        {
            if (_enemy == null || _enemy.Health.IsDead || _enemy.Asleep) return;
            var player = PlayerController.Instance;
            if (player == null) return;

            if (kind == Kind.Trail)
            {
                if (Time.time < _nextDrop) return;
                _nextDrop = Time.time + 0.45f;
                Vector3 at = transform.position;
                if (trailHurts) GroundHazard.SpawnAcidPool(new Vector3(at.x, 0f, at.z), material);
                else GroundHazard.SpawnStickyPatch(new Vector3(at.x, 0f, at.z), material);
                Uses++;
                return;
            }

            if (Time.time < _next) return;
            _next = Time.time + every;
            Uses++;
            switch (kind)
            {
                case Kind.Burst:
                    if (projectilePrefab == null) break;
                    if (!Pools.TryGetValue(projectilePrefab, out var pool)) { pool = new Pool<Bullet>(projectilePrefab); Pools.Add(projectilePrefab, pool); }
                    float offset = Random.Range(0f, 360f / shots);
                    for (int i = 0; i < shots; i++)
                    {
                        Vector3 direction = Quaternion.Euler(0f, offset + i * 360f / shots, 0f) * Vector3.forward;
                        pool.Get().Launch(transform.position + Vector3.up * 0.8f + direction, direction, shotSpeed, new Shot { damage = 1, owner = _enemy.Health }, 16f, Team.Enemy, pool);
                    }
                    Sfx.Play(SfxId.Shoot, 0.6f);
                    break;

                case Kind.Shockwave:
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 at = transform.position + Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * 3.2f;
                        GroundHazard.SpawnBlast(new Vector3(at.x, 0f, at.z), 1.4f, 0.9f, material);
                    }
                    break;

                case Kind.Hem:
                {
                    Vector3 centre = player.transform.position;
                    int gap = Random.Range(0, 10);
                    for (int i = 0; i < 10; i++)
                    {
                        if (i == gap) continue;
                        Vector3 at = centre + Quaternion.Euler(0f, i * 36f, 0f) * Vector3.forward * 3.5f;
                        GroundHazard.SpawnBlast(new Vector3(at.x, 0f, at.z), 1.2f, 1.4f, material);
                    }
                    break;
                }
            }
        }
    }
}
