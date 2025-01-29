using UnityEngine;
using System.Collections.Generic;

public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] private Stats enemyStats; // Reference to a Stats ScriptableObject for enemies

    [Header("Health System")]
    private HealthSystem healthSystem;

    [Header("Enemy Loot Settings")]
    public LootTable lootTable;
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
        List<GameObject> drops = lootTable.GetDrops();

        // 2. Spawn each drop at this position
        foreach (GameObject drop in drops)
        {
            Instantiate(drop, transform.position, Quaternion.identity);
        }

        // 3. Destroy or disable the enemy
        Debug.Log($"{gameObject.name} has died");
        Destroy(gameObject);
    }
}
