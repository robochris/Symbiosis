// Assets/Scripts/Player/AttackSystem.cs
using UnityEngine;

public class AttackSystem : MonoBehaviour
{
    public static AttackSystem Instance { get; private set; }

    [Header("Attack Settings")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private BulletPool bulletPool; // Reference to the BulletPool
    [SerializeField] private Stats playerStats;

    private float nextFireTime = 0f;

    private PlayerManagement playerManagement;
    private GameObject bulletPrefab;

    private void Start()
    {
        if (playerStats.defaultBulletPrefab == null)
        {
            Debug.LogError("AttackSystem: BulletPrefab is not assigned.");
        }
        else
        {
            // Store the default bullet
            bulletPrefab = playerStats.defaultBulletPrefab;
        }
    }
    private void Awake()
    {
        // Get reference to PlayerManagement
        playerManagement = PlayerManagement.Instance;
        if (playerManagement == null)
        {
            Debug.LogError("AttackSystem: PlayerManagement instance not found.");
        }

        // Validate bulletPool
        if (bulletPool == null)
        {
            Debug.LogError("AttackSystem: BulletPool is not assigned.");
        }

        // Validate firePoint
        if (firePoint == null)
        {
            Debug.LogError("AttackSystem: FirePoint is not assigned.");
        }
    }

    private void Update()
    {
        // Example: Fire on left mouse button click
        if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + playerManagement.GetFireRateSpeed();
        }
    }

    public void Shoot()
    {
        if (bulletPool == null || firePoint == null || bulletPrefab == null || playerManagement == null)
        {
            Debug.LogError("AttackSystem: Missing references. Cannot shoot.");
            return;
        }

        // Get a bullet from the pool
        GameObject bulletObj = bulletPool.GetBullet(bulletPrefab);
        if (bulletObj != null)
        {
            bulletObj.transform.position = firePoint.position;
            bulletObj.transform.rotation = firePoint.rotation;

            // Initialize bullet with current attack damage and bullet speed
            IBullet bullet = bulletObj.GetComponent<IBullet>();
            if (bullet != null)
            {
                int attackDamage = playerManagement.GetAttackDamage();
                float bulletSpeed = playerManagement.GetBulletSpeed();
                bullet.Initialize(attackDamage, bulletSpeed);
                bullet.SetPool(bulletPool); // Ensure the bullet knows which pool to return to
            }
            else
            {
                Debug.LogWarning("AttackSystem: Bullet does not implement IBullet interface.");
            }
        }
        else
        {
            Debug.LogWarning("AttackSystem: BulletPool returned null.");
        }

        Debug.Log("AttackSystem: Fired a bullet.");

    }

    public void SetBulletPrefab(GameObject newPrefab)
    {
        bulletPrefab = newPrefab;
    }

    public GameObject GetCurrentBulletPrefab()
    {
        return bulletPrefab;
    }
}
