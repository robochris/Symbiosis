using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class PlayerManagement : MonoBehaviour
{
    // Singleton instance
    public static PlayerManagement Instance { get; private set; }

    private List<ItemData> activeItems = new List<ItemData>();

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

    private void Update()
    {
        // TEST: Press 'K' to damage the player by 5
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(5);
            Debug.Log("Test Damage: Player took 5 damage from key press.");
        }
    }


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
        ResetToBaseStats();
    }

    private void HandleHealthChanged(int newHealth)
    {
        OnHealthChanged?.Invoke(newHealth);
    }

    public AttackSystem GetAttackSystem()
    {
        return attackSystem;
    }

    private void HandlePlayerDied()
    {
        OnPlayerDied?.Invoke();
        // Additional death handling (e.g., show game over screen)
        // Now that the player is dead, do you restart immediately?
        // Or do you show a game over screen, then call ResetToBaseStats later?

        // For example:
        // ResetToBaseStats(); 
        // or handle it from a GameManager, etc.
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
        fireRateModifier -= amount;
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

    public void AddItem(ItemData newItem)
    {
        if (newItem == null) return;

        activeItems.Add(newItem);
        newItem.Apply(this);
    }

    public void RemoveItem(ItemData itemToRemove)
    {
        if (itemToRemove == null) return;

        if (activeItems.Contains(itemToRemove))
        {
            itemToRemove.Remove(this);
            activeItems.Remove(itemToRemove);
        }
    }

    public void RemoveAllItems()
    {
        foreach (var item in activeItems)
        {
            item.Remove(this);
        }
        activeItems.Clear();
    }

    // Directly handle numeric modifiers
    public void ResetToBaseStats()
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerManagement: BasePlayerStats is not assigned!");
            return;
        }

        // 1. Reset dynamic modifiers
        attackDamageModifier = 0;
        bulletSpeedModifier = 0f;
        fireRateModifier = 0f;

        // 2. Reset HealthSystem
        if (healthSystem != null)
        {
            // SetStats will set currentHealth to basePlayerStats.maxHealth
            healthSystem.SetStats(playerStats.maxHealth);
            // Subscribe to HealthSystem events
            healthSystem.OnHealthChanged -= HandleHealthChanged;
            healthSystem.OnHealthChanged += HandleHealthChanged;
            healthSystem.OnDeath -= HandlePlayerDied;
            healthSystem.OnDeath += HandlePlayerDied;
        }

        // 3. Reset AttackSystem
        if (attackSystem != null && playerStats.defaultBulletPrefab != null)
        {
            // Force AttackSystem to use the default bullet from base stats
            attackSystem.SetBulletPrefab(playerStats.defaultBulletPrefab);
        }

        // 4. Reset score (if desired)
        currentScore = 0;
        OnScoreChanged?.Invoke(currentScore);

        // 5. Remove all active items/traits (if you have them)
        RemoveAllItems();

        Debug.Log("PlayerManagement: Reset to base stats complete.");
    }
    // Getter for score
    public int GetScore()
    {
        return currentScore;
    }
}
