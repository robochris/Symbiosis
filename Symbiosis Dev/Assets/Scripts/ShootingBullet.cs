// Assets/Scripts/ShootingBullet.cs
using UnityEngine;

public class ShootingBullet : MonoBehaviour
{

    [Header("Bullet Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeedMultiplier = 1f; // Allows traits to modify bullet speed
    [SerializeField] private int bulletDamageMultiplier = 1;    // Allows traits to modify bullet damage
    [SerializeField] private BulletPool bulletPool; // Reference to the BulletPool
    [SerializeField] private Stats playerStats;
    // Cached player stats
    private float currentBulletSpeed;
    private int currentBulletDamage;

    private void Awake()
    {
        if (PlayerManagement.Instance == null)
        {
            Debug.LogError("ShootingBullet: PlayerManagement instance not found in the scene.");
            return;
        }

        // Initialize bullet stats based on player stats
        UpdateBulletStats();

        // Subscribe to stats changed event
        PlayerManagement.Instance.OnStatsChanged += UpdateBulletStats;
    }

    private void OnDestroy()
    {
        if (PlayerManagement.Instance != null)
        {
            PlayerManagement.Instance.OnStatsChanged -= UpdateBulletStats;
        }
    }

    private void UpdateBulletStats()
    {
        // Fetch updated stats from PlayerManagement
        currentBulletSpeed = playerStats.bulletSpeed * bulletSpeedMultiplier;
        currentBulletDamage = playerStats.attackDamage * bulletDamageMultiplier;

        Debug.Log($"ShootingBullet: Updated bullet speed to {currentBulletSpeed} and damage to {currentBulletDamage}.");
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (bulletPool == null)
        {
            Debug.LogError("ShootingBullet: BulletPool is not assigned.");
            return;
        }

        GameObject bulletObj = bulletPool.GetBullet();
        if (bulletObj == null)
        {
            Debug.LogError("ShootingBullet: BulletPool returned null.");
            return;
        }

        bulletObj.transform.position = firePoint.position;
        bulletObj.transform.rotation = firePoint.rotation;

        IBullet bullet = bulletObj.GetComponent<IBullet>();
        if (bullet != null)
        {
            bullet.Initialize(currentBulletDamage, currentBulletSpeed);
            bullet.SetPool(bulletPool); // Ensure the bullet knows which pool to return to
        }
        else
        {
            Debug.LogWarning("ShootingBullet: Bullet does not implement IBullet interface.");
        }

        Debug.Log("ShootingBullet: Fired a bullet.");
    }
}
