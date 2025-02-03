using UnityEngine;

public class AttackSystem : MonoBehaviour
{
    public static AttackSystem Instance { get; private set; }

    [Header("Attack Settings")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private BulletPool bulletPool;
    [SerializeField] private Stats playerStats;

    private float nextFireTime = 0f;
    private PlayerManagement playerManagement;
    private GameObject bulletPrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Find PlayerManagement
        playerManagement = PlayerManagement.Instance;
        if (!playerManagement)
            Debug.LogError("AttackSystem: PlayerManagement instance not found.");

        // Validate references
        if (!bulletPool) Debug.LogError("AttackSystem: BulletPool is not assigned.");
        if (!firePoint) Debug.LogError("AttackSystem: FirePoint is not assigned.");
    }

    private void Start()
    {
        // Load default bullet from Stats
        if (!playerStats || !playerStats.defaultBulletPrefab)
        {
            Debug.LogError("AttackSystem: BulletPrefab is not assigned in Stats.");
        }
        else
        {
            bulletPrefab = playerStats.defaultBulletPrefab;
        }
    }

    private void Update()
    {
        // Fire on left mouse and respect fire-rate cooldown
        if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + playerManagement.GetFireRateSpeed();
        }
    }

    public void Shoot()
    {
        // Basic validation
        if (!bulletPool || !firePoint || !bulletPrefab || !playerManagement)
        {
            Debug.LogError("AttackSystem: Missing references, cannot shoot.");
            return;
        }

        // Get a bullet from the pool
        GameObject bulletObj = bulletPool.GetBullet(bulletPrefab);
        if (bulletObj == null)
        {
            Debug.LogWarning("AttackSystem: BulletPool returned null.");
            return;
        }

        // Spawn the bullet at the firePoint
        bulletObj.transform.position = firePoint.position;
        bulletObj.transform.rotation = firePoint.rotation;

        // Initialize bullet stats
        IBullet bullet = bulletObj.GetComponent<IBullet>();
        if (bullet != null)
        {
            int attackDamage = playerManagement.GetAttackDamage();
            float bulletSpeed = playerManagement.GetBulletSpeed();
            bullet.Initialize(attackDamage, bulletSpeed);
            bullet.SetPool(bulletPool);
        }
        else
        {
            Debug.LogWarning("AttackSystem: Bullet does not implement IBullet interface.");
        }

        Debug.Log("AttackSystem: Fired a bullet from the player's facing direction.");
    }

    public void SetBulletPrefab(GameObject newPrefab) => bulletPrefab = newPrefab;
    public GameObject GetCurrentBulletPrefab() => bulletPrefab;
}
