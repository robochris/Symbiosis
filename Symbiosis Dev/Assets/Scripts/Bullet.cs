using UnityEngine;

public class Bullet : MonoBehaviour, IBullet
{
    private int damage;
    private float speed;
    private BulletPool bulletPool;

    // This tracks which prefab the bullet was instantiated from
    private GameObject originalPrefab;

    public void Initialize(int damageAmount, float speedAmount)
    {
        damage = damageAmount;
        speed = speedAmount;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity= transform.forward * speed;
        }
    }

    public void SetPool(BulletPool pool)
    {
        bulletPool = pool;
    }

    // Called by the pool to store the prefab reference
    public void SetOriginalPrefab(GameObject prefab)
    {
        originalPrefab = prefab;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Example damage logic...
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }

        // Return the bullet to the pool
        if (bulletPool != null && originalPrefab != null)
        {
            bulletPool.ReturnBullet(gameObject, originalPrefab);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
