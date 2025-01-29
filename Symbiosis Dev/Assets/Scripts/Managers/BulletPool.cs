using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single BulletPool component that can manage multiple bullet prefabs simultaneously.
/// </summary>
public class BulletPool : MonoBehaviour
{
    [Header("Default Pool Settings")]
    [SerializeField] private int defaultPoolSize = 50;

    // Dictionary to track a separate queue for each bullet prefab
    // Key: the bullet prefab
    // Value: a queue of bullet GameObjects spawned from that prefab
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary
        = new Dictionary<GameObject, Queue<GameObject>>();

    /// <summary>
    /// Pre-initialize a queue for a given bullet prefab with 'size' bullets.
    /// Call this for each bullet prefab you know you'll need to pool.
    /// </summary>
    public void InitializePool(GameObject bulletPrefab, int size = -1)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("BulletPool: bulletPrefab is null!");
            return;
        }
        if (size < 0) size = defaultPoolSize;

        // If we haven't yet created a queue for this prefab, create one
        if (!poolDictionary.ContainsKey(bulletPrefab))
        {
            poolDictionary[bulletPrefab] = new Queue<GameObject>();
        }

        // Instantiate 'size' bullets, deactivate them, and enqueue
        for (int i = 0; i < size; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab, transform);
            bullet.SetActive(false);

            // Let the bullet know which pool it belongs to (so it can return itself)
            IBullet bulletInterface = bullet.GetComponent<IBullet>();
            if (bulletInterface != null)
            {
                bulletInterface.SetPool(this);
            }
            else
            {
                Debug.LogWarning(
                    $"BulletPool: Bullet prefab {bulletPrefab.name} doesn't implement IBullet."
                );
            }

            // We can also store "original prefab" on each bullet if needed for returning
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.SetOriginalPrefab(bulletPrefab);
            }

            poolDictionary[bulletPrefab].Enqueue(bullet);
        }

        Debug.Log(
            $"BulletPool: Initialized {size} bullets for prefab '{bulletPrefab.name}'."
        );
    }

    /// <summary>
    /// Retrieves a bullet of the specified prefab type.
    /// If none remain in the queue, a new instance is created.
    /// </summary>
    public GameObject GetBullet(GameObject bulletPrefab)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("BulletPool: Provided bulletPrefab is null in GetBullet!");
            return null;
        }

        // If we haven't yet made a queue for this prefab, create one empty queue
        if (!poolDictionary.ContainsKey(bulletPrefab))
        {
            // Initialize with zero pre-spawned bullets
            poolDictionary[bulletPrefab] = new Queue<GameObject>();
        }

        // If the queue is empty, instantiate a new bullet
        if (poolDictionary[bulletPrefab].Count == 0)
        {
            GameObject newBullet = Instantiate(bulletPrefab, transform);
            newBullet.SetActive(true);

            IBullet bulletInterface = newBullet.GetComponent<IBullet>();
            if (bulletInterface != null)
            {
                bulletInterface.SetPool(this);
            }

            Bullet bulletScript = newBullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.SetOriginalPrefab(bulletPrefab);
            }

            Debug.Log(
                $"BulletPool: Instantiated new bullet of prefab '{bulletPrefab.name}' as pool was empty."
            );
            return newBullet;
        }
        else
        {
            // Reuse an existing bullet from the queue
            GameObject reusedBullet = poolDictionary[bulletPrefab].Dequeue();
            reusedBullet.SetActive(true);

            Debug.Log(
                $"BulletPool: Reused bullet of prefab '{bulletPrefab.name}' from pool."
            );
            return reusedBullet;
        }
    }

    /// <summary>
    /// Return a bullet to the pool. The bullet script should know its original prefab,
    /// so we know which queue to return it to.
    /// </summary>
    public void ReturnBullet(GameObject bullet, GameObject prefab)
    {
        if (bullet == null || prefab == null)
        {
            Debug.LogWarning("BulletPool: bullet or prefab is null in ReturnBullet!");
            Destroy(bullet); // can't safely re-queue
            return;
        }

        bullet.SetActive(false);

        // Make sure the dictionary has a queue for this prefab
        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary[prefab] = new Queue<GameObject>();
        }

        poolDictionary[prefab].Enqueue(bullet);
        Debug.Log($"BulletPool: Returned bullet of prefab '{prefab.name}' to pool.");
    }
}
