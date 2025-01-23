using UnityEngine;

public class PlayerManagement : MonoBehaviour
{
    // Singleton instance
    public static PlayerManagement Instance { get; private set; }

    // Player stats
    [Header("Player Stats")]
    public int attackDamage = 10;
    public float bulletSpeed = 20f;
    public int health = 100;

    private int score;
    private HealthSystem healthSystem;

    // Events for UI and other systems
    public event System.Action<int> OnScoreChanged;
    public event System.Action<int> OnHealthChanged;
    public event System.Action OnStatsChanged;
    public event System.Action OnPlayerDied;

    private void Awake()
    {
        // Implement Singleton Pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Optionally persist across scenes
        // DontDestroyOnLoad(gameObject);

        // Initialize HealthSystem
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            Debug.LogError("PlayerManagement: HealthSystem component missing.");
        }
        else
        {
            healthSystem.OnHealthChanged += HandleHealthChanged;
            healthSystem.OnEntityDied += HandlePlayerDied;
        }

        // Initialize score
        score = 0;
    }

    private void HandleHealthChanged(int newHealth)
    {
        OnHealthChanged?.Invoke(newHealth);
    }

    private void HandlePlayerDied()
    {
        OnPlayerDied?.Invoke();
        // Additional death handling (e.g., show game over screen)
    }

    // Methods to modify player stats
    public void AddScore(int amount)
    {
        score += amount;
        OnScoreChanged?.Invoke(score);
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

    public void UpgradeAttackDamage(int amount)
    {
        attackDamage += amount;
        Debug.Log($"PlayerManagement: Attack Damage increased by {amount}. New Attack Damage: {attackDamage}");
        // Update UI or other dependent systems here
    }

    public void UpgradeBulletSpeed(float amount)
    {
        bulletSpeed += amount;
        Debug.Log($"PlayerManagement: Bullet Speed increased by {amount}. New Bullet Speed: {bulletSpeed}");
        // Update UI or other dependent systems here
    }

    // Getter for current health
    public int GetCurrentHealth()
    {
        return healthSystem != null ? healthSystem.GetCurrentHealth() : 0;
    }

    // Getter for score
    public int GetScore()
    {
        return score;
    }
}
