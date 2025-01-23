using UnityEngine;

public class HealthSystem : MonoBehaviour, IDamageable
{
    [SerializeField] private Stats stats;  // Assign via Inspector

    private int currentHealth;

    // Reference to FloatingDamageManager
    private FloatingDamageManager floatingDamageManager;

    // Events to notify other systems
    public event System.Action<int> OnHealthChanged;
    public event System.Action OnEntityDied;

    private void Awake()
    {
        if (stats == null)
        {
            Debug.LogError($"HealthSystem: Stats not assigned on {gameObject.name}.");
            return;
        }

        InitializeHealth();

        // Find the FloatingDamageManager in the scene
        floatingDamageManager = Object.FindFirstObjectByType<FloatingDamageManager>();
        if (floatingDamageManager == null)
        {
            Debug.LogError("HealthSystem: FloatingDamageManager not found in the scene.");
        }
    }

    // Initialize health based on assigned Stats
    public void InitializeHealth()
    {
        currentHealth = stats.maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

    // Implementing IDamageable
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        Debug.Log($"{gameObject.name} took {damage} damage. Current health: {currentHealth}");
        OnHealthChanged?.Invoke(currentHealth);

        // Trigger floating damage
        if (floatingDamageManager != null)
        {
            Vector3 damagePosition = transform.position + Vector3.up * 2f;  // Adjust as needed
            floatingDamageManager.SpawnFloatingDamage(damagePosition, damage);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, stats.maxHealth);
        Debug.Log($"{gameObject.name} healed by {amount}. Current health: {currentHealth}");
        OnHealthChanged?.Invoke(currentHealth);
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return stats.maxHealth;
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        OnEntityDied?.Invoke();
        Destroy(gameObject);  // Or handle death differently (animations, effects, etc.)
    }
}
