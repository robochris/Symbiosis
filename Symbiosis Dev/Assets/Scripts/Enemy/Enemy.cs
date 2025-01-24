// Assets/Scripts/Enemies/Enemy.cs
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] private Stats enemyStats; // Reference to a Stats ScriptableObject for enemies

    private HealthSystem healthSystem;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            Debug.LogError("Enemy: HealthSystem component missing.");
        }
        else
        {
            healthSystem.SetStats(enemyStats.maxHealth);
            healthSystem.OnHealthChanged += HandleHealthChanged;
            healthSystem.OnDeath += HandleEnemyDied;
        }
    }

    public void TakeDamage(int damage)
    {
        if (healthSystem != null)
        {
            healthSystem.TakeDamage(damage);
        }
    }

    public void Heal(int amount)
    {
        if (healthSystem != null)
        {
            healthSystem.Heal(amount);
        }
    }

    public int GetCurrentHealth()
    {
        return healthSystem != null ? healthSystem.GetCurrentHealth() : 0;
    }

    public int GetMaxHealth()
    {
        return healthSystem != null ? healthSystem.GetMaxHealth() : 0;
    }

    private void HandleHealthChanged(int newHealth)
    {
        // Optional: Implement enemy-specific health change logic
    }

    private void HandleEnemyDied()
    {
        Debug.Log($"{gameObject.name} has died.");
        // Handle death (e.g., drop loot, play animation, etc.)
        Destroy(gameObject);
    }
}
