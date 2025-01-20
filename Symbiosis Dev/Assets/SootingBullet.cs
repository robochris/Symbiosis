using UnityEngine;

public class ShootingBullet : MonoBehaviour
{
    public GameObject bulletPrefab;
    public float bulletSpeed = 50f;
    public Transform bulletSpawnPoint;

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
            rb.AddForce(bulletSpawnPoint.forward * bulletSpeed, ForceMode.Impulse);

            // Destroy after 3s
            Destroy(bullet, 3f);
        }
    }
}
