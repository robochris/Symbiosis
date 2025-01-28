using UnityEngine;

public class PlayerManagement : MonoBehaviour
{
    // Singleton instance
    public static PlayerManagement Instance { get; private set; }

    [Header("Player Configuration")]
    [SerializeField] private Stats playerStats; // Reference to the Stats ScriptableObject

    private HealthSystem healthSystem;
    private AttackSystem attackSystem; // Reference to AttackSystem

    // Player dynamic stats (modifiers)
    private int attackDamageModifier = 0;
    private float bulletSpeedModifier = 0f;
    private float fireRateModifier = 0f;

    // Player dynamic stats
    private int currentScore;

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
            // Assign the playerStats to HealthSystem's stats
            healthSystem.SetStats(playerStats.maxHealth);

            healthSystem.OnHealthChanged += HandleHealthChanged;
            healthSystem.OnDeath += HandlePlayerDied;
        }

        // Initialize AttackSystem
        attackSystem = GetComponent<AttackSystem>();
        if (attackSystem == null)
        {
            Debug.LogError("PlayerManagement: AttackSystem component missing.");
        }

        // Initialize score
        currentScore = 0;
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
        currentScore += amount;
        currentScore = Mathf.Clamp(currentScore, 0, playerStats.maxScore);
        OnScoreChanged?.Invoke(currentScore);
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
        attackDamageModifier += amount;
        Debug.Log($"PlayerManagement: Attack Damage increased by {amount}. New Attack Damage Modifier: {attackDamageModifier}");
        OnStatsChanged?.Invoke();
        // Update UI or other dependent systems here
    }

    public void UpgradeBulletSpeed(float amount)
    {
        bulletSpeedModifier += amount;
        Debug.Log($"PlayerManagement: Bullet Speed increased by {amount}. New Bullet Speed Modifier: {bulletSpeedModifier}");
        OnStatsChanged?.Invoke();
        // Update UI or other dependent systems here
    }

    public void UpgradeFireRate(float amount)
    {
        fireRateModifier += amount;
    }

    // Getters for stats, considering modifiers
    public int GetAttackDamage()
    {
        return playerStats.attackDamage + attackDamageModifier;
    }

    public float GetBulletSpeed()
    {
        return playerStats.bulletSpeed + bulletSpeedModifier;
    }

    public float GetFireRateSpeed()
    {
        return playerStats.fireRate + fireRateModifier;
    }

    // Getter for current health
    public int GetCurrentHealth()
    {
        return healthSystem != null ? healthSystem.GetCurrentHealth() : 0;
    }

    // Getter for score
    public int GetScore()
    {
        return currentScore;
    }
}
