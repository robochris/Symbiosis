using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Keeps the test arena stocked: whenever an ant dies, a new one arrives a moment later, away from the player.
    /// Set Prefab to any ant and tick mutations to try them out one at a time.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Tooltip("Where the enemy and mutation lists come from.")]
        public BiomeDefinition biome;
        [Tooltip("Which ant to keep spawning. Empty = the biome's swarm enemy (the worker).")]
        public Enemy prefab;
        [Tooltip("Mutations every spawned ant gets, for trying them out.")]
        public MutationDefinition[] forcedMutations;
        [Tooltip("How many ants are alive at once.")]
        public int maxAlive = 1;
        public float respawnDelay = 1.2f;
        [Tooltip("Half the width of the square area ants can appear in.")]
        public float arenaHalfSize = 12f;
        public float minDistanceFromPlayer = 8f;

        void OnEnable()
        {
            Enemy.Killed += OnKilled;
            Enemy.SummonRequested += OnSummon;
        }

        void OnDisable()
        {
            Enemy.Killed -= OnKilled;
            Enemy.SummonRequested -= OnSummon;
        }

        void Start()
        {
            for (int i = 0; i < maxAlive; i++) SpawnRandom();
        }

        void OnKilled(Enemy enemy)
        {
            StartCoroutine(Respawn(enemy));
        }

        IEnumerator Respawn(Enemy dead)
        {
            yield return new WaitForSeconds(respawnDelay);
            if (dead != null) Destroy(dead.gameObject);
            if (Enemy.Active.Count < maxAlive) SpawnRandom();
        }

        void OnSummon(Enemy source, int count)
        {
            if (biome == null || biome.swarmEnemy == null) return;
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, 360f * i / count, 0f) * Vector3.forward * 2f;
                SpawnAt(biome.swarmEnemy, source.transform.position + offset, null);
            }
        }

        void SpawnRandom()
        {
            Vector3 playerPosition = PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero;
            Vector3 position = Vector3.zero;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                position = new Vector3(Random.Range(-arenaHalfSize, arenaHalfSize), 0f, Random.Range(-arenaHalfSize, arenaHalfSize));
                if ((position - playerPosition).sqrMagnitude >= minDistanceFromPlayer * minDistanceFromPlayer) break;
            }

            Enemy which = prefab != null ? prefab : biome != null ? biome.swarmEnemy : null;
            if (which != null) SpawnAt(which, position, forcedMutations);
        }

        /// <summary>Puts one enemy in the arena, facing the player, with the given mutations.</summary>
        public Enemy SpawnAt(Enemy enemyPrefab, Vector3 position, IList<MutationDefinition> mutations)
        {
            Vector3 playerPosition = PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero;
            Vector3 toPlayer = playerPosition - position;
            toPlayer.y = 0f;
            var rotation = toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity;

            position.y = 0.05f;
            Enemy enemy = Instantiate(enemyPrefab, position, rotation, transform);
            if (enemy.Mutations != null) enemy.Mutations.Apply(mutations);
            return enemy;
        }
    }
}
