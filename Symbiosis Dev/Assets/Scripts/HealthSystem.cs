using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public int maxHealth = 100;  // Maximum health value
    private int currentHealth;   // Current health

    void Start()
    {
        currentHealth = maxHealth;  // Initialize health
    }

    // Function to apply damage
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log(gameObject.name + " took " + damage + " damage. Current health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }


    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        Debug.Log(gameObject.name + " healed. Current health: " + currentHealth);
    }
  
    void Die()
    {
        Debug.Log(gameObject.name + " cheecks have deceased.");
        Destroy(gameObject);  // Destroy the object or trigger a death animation
    }
}