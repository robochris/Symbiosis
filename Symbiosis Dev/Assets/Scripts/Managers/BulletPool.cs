// Assets/Scripts/Managers/BulletPool.cs
using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int poolSize = 50;

    private Queue<GameObject> bullets = new Queue<GameObject>();

    private void Awake()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("BulletPool: Bullet Prefab is not assigned.");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab, transform);
            bullet.SetActive(false);

            // Initialize the bullet via IBullet interface
            IBullet bulletInterface = bullet.GetComponent<IBullet>();
            if (bulletInterface != null)
            {
                bulletInterface.SetPool(this);
            }
            else
            {
                Debug.LogWarning("BulletPool: Bullet prefab does not implement IBullet interface.");
            }

            bullets.Enqueue(bullet);
        }

        Debug.Log($"BulletPool: Initialized with {poolSize} bullets.");
    }

    /// <summary>
    /// Retrieves a bullet from the pool. If the pool is empty, a new bullet is instantiated.
    /// </summary>
    /// <returns>Active bullet GameObject.</returns>
    public GameObject GetBullet()
    {
        if (bullets.Count > 0)
        {
            GameObject bullet = bullets.Dequeue();
            bullet.SetActive(true);
            Debug.Log("BulletPool: Retrieved bullet from pool.");
            return bullet;
        }
        else
        {
            GameObject bullet = Instantiate(bulletPrefab, transform);
            bullet.SetActive(true);

            // Initialize the new bullet
            IBullet bulletInterface = bullet.GetComponent<IBullet>();
            if (bulletInterface != null)
            {
                bulletInterface.SetPool(this);
            }
            else
            {
                Debug.LogWarning("BulletPool: Bullet prefab does not implement IBullet interface.");
            }

            Debug.Log("BulletPool: Instantiated new bullet as pool was empty.");
            return bullet;
        }
    }

    /// <summary>
    /// Returns a bullet to the pool, deactivating it for future use.
    /// </summary>
    /// <param name="bullet">Bullet GameObject to return.</param>
    public void ReturnBullet(GameObject bullet)
    {
        bullet.SetActive(false);
        bullets.Enqueue(bullet);
        Debug.Log("BulletPool: Returned bullet to pool.");
    }
}
