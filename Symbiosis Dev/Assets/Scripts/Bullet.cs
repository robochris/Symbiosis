// Assets/Scripts/Bullet.cs
using UnityEngine;

public class Bullet : MonoBehaviour, IBullet
{
    private int damage;
    private BulletPool bulletPool;

    public void Initialize(int damageAmount, float speed)
    {
        damage = damageAmount;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed; // Use velocity instead of linearVelocity for consistency
        }
        else
        {
            Debug.LogWarning("Bullet: Rigidbody component missing.");
        }
    }

    public void SetPool(BulletPool pool)
    {
        bulletPool = pool;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Apply damage to enemies
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            Debug.Log($"Bullet: Applied {damage} damage to {collision.gameObject.name}.");
        }

        // Return the bullet to the pool for reuse
        if (bulletPool != null)
        {
            bulletPool.ReturnBullet(gameObject);
        }
        else
        {
            // If no pool is assigned, destroy the bullet to prevent clutter
            Destroy(gameObject);
        }

        Debug.Log("Bullet: Collided and returned to pool.");
    }

    // Optional: Add additional behavior (e.g., lifespan, effects) as needed
}
