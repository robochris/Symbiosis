using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    [Header("Spawning Settings")]
    public GameObject enemyPrefab;       // Enemy prefab to spawn
    public Vector3 spawnAreaCenter;      // Center of the spawn area
    public Vector3 spawnAreaSize = new Vector3(10, 0, 10); // Size of spawn area (y is ignored)

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SpawnEnemy()
    {
        // Determine a random spawn position within the defined area.
        Vector3 randomPos = spawnAreaCenter + new Vector3(
            Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
            0, // Keep y fixed (enemy will float at its set height)
            Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
        );
        Instantiate(enemyPrefab, randomPos, Quaternion.identity);
    }
}
