// Assets/Scripts/Systems/HealthSystem.cs
using Unity.VisualScripting;
using UnityEngine;

public class HealthSystem : MonoBehaviour, IDamageable, IHealable, IHealthInfo, IDeathtable
{
    [SerializeField] private Stats playerStats;
    private int currentHealth;

    // Events to notify other systems
    public event System.Action<int> OnHealthChanged;
    public event System.Action OnDeath;

    private void Awake()
    {
        currentHealth = playerStats.maxHealth;
    }

    // Implementing IDamageable
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, playerStats.maxHealth);
        Debug.Log($"{gameObject.name} took {damage} damage. Current Health: {currentHealth}");
        OnHealthChanged?.Invoke(currentHealth);

        // Trigger floating damage
        if (FloatingDamageManager.Instance != null)
        {
            Vector3 damagePosition = transform.position + Vector3.up * 1.5f; // Adjust as needed
            FloatingDamageManager.Instance.SpawnFloatingDamage(damagePosition, damage);
        }
        else
        {
            Debug.LogError("HealthSystem: FloatingDamageManager instance not found.");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Implementing IHealable
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, playerStats.maxHealth);
        Debug.Log($"{gameObject.name} healed by {amount}. Current Health: {currentHealth}");
        OnHealthChanged?.Invoke(currentHealth);
    }

    // Implementing IHealthInfo
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return playerStats.maxHealth;
    }

    // Implementing IDeathtable
    public void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        OnDeath?.Invoke();
        // Handle death logic (e.g., play animation, drop loot)
        Destroy(gameObject);
    }

    // Method to set stats (useful for initializing with different stats)
    public void SetStats(int newMaxHealth)
    {
        playerStats.maxHealth = newMaxHealth;
        currentHealth = playerStats.maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }
}
