using UnityEngine;

public class ShootingBullet : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint;
    public int damage = 20;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Spawn bullet at the spawn point's position & rotation
            GameObject bullet = Instantiate(
                bulletPrefab,
                bulletSpawnPoint.position,
                bulletSpawnPoint.rotation
            );

            // Apply force relative to the spawn point’s forward direction
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            rb.AddForce(bulletSpawnPoint.forward * PlayerManagement.Instance.bulletSpeed, ForceMode.Impulse);

            // Destroy after 3s
            Destroy(bullet, 3f);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy")) 
        {
            HealthSystem enemyHealth = other.GetComponent<HealthSystem>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
            Destroy(gameObject);  // Destroy the projectile after dealing damage
        }
        else if (other.CompareTag("Obstacle"))
        {
            Destroy(gameObject);  // Destroy if it hits an obstacle
        }
    }
}
