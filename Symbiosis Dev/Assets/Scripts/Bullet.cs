// Assets/Scripts/Bullet.cs
using UnityEngine;

public class Bullet : MonoBehaviour, IBullet
{
    private int damage;
    private BulletPool bulletPool;

    /// <summary>
    /// Initializes the bullet with damage and speed.
    /// </summary>
    /// <param name="damageAmount">Damage value of the bullet.</param>
    /// <param name="speed">Speed at which the bullet moves.</param>
    public void Initialize(int damageAmount, float speed)
    {
        damage = damageAmount;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }
        else
        {
            Debug.LogWarning("Bullet: Rigidbody component missing.");
        }
    }

    /// <summary>
    /// Assigns the BulletPool to return this bullet to after use.
    /// </summary>
    /// <param name="pool">The BulletPool managing this bullet.</param>
    public void SetPool(BulletPool pool)
    {
        bulletPool = pool;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Example collision handling (e.g., applying damage to enemies)
        // Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        // if (enemy != null)
        // {
        //     enemy.TakeDamage(damage);
        // }

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
