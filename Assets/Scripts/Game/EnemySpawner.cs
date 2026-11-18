using System.Collections;
using UnityEngine;

namespace Shrunk
{
    /// <summary>Keeps the test arena stocked: whenever an ant dies, a new one arrives a moment later, away from the player.</summary>
    public class EnemySpawner : MonoBehaviour
    {
        public Enemy prefab;
        [Tooltip("How many ants are alive at once.")]
        public int maxAlive = 1;
        public float respawnDelay = 1.2f;
        [Tooltip("Half the width of the square area ants can appear in.")]
        public float arenaHalfSize = 12f;
        public float minDistanceFromPlayer = 8f;

        Pool<Enemy> _pool;

        void Awake()
        {
            _pool = new Pool<Enemy>(prefab, transform);
        }

        void OnEnable()
        {
            Enemy.Killed += OnKilled;
        }

        void OnDisable()
        {
            Enemy.Killed -= OnKilled;
        }

        void Start()
        {
            for (int i = 0; i < maxAlive; i++) Spawn();
        }

        void OnKilled(Enemy enemy)
        {
            StartCoroutine(Respawn(enemy));
        }

        IEnumerator Respawn(Enemy dead)
        {
            yield return new WaitForSeconds(respawnDelay);
            _pool.Release(dead);
            if (Enemy.Active.Count < maxAlive) Spawn();
        }

        void Spawn()
        {
            Vector3 playerPosition = PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero;
            Vector3 position = Vector3.zero;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                position = new Vector3(Random.Range(-arenaHalfSize, arenaHalfSize), 0.05f, Random.Range(-arenaHalfSize, arenaHalfSize));
                if ((position - playerPosition).sqrMagnitude >= minDistanceFromPlayer * minDistanceFromPlayer) break;
            }

            var enemy = _pool.Get();
            Vector3 toPlayer = playerPosition - position;
            toPlayer.y = 0f;
            enemy.transform.SetPositionAndRotation(position, toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity);
            enemy.gameObject.SetActive(true);
        }
    }
}
