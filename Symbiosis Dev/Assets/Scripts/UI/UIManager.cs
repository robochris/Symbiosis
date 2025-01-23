using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    // Singleton instance
    public static UIManager Instance { get; private set; }

    // UI elements assigned via the Inspector
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI statsText;

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

        // Initialize UI elements
        InitializeUI();
    }

    private void InitializeUI()
    {
        if (scoreText == null || healthText == null || statsText == null)
        {
            Debug.LogError("UIManager: One or more UI elements are not assigned in the Inspector.");
            return;
        }

        // Initialize UI with current player stats
        UpdateScore(PlayerManagement.Instance != null ? PlayerManagement.Instance.GetScore() : 0);
        UpdateHealth(PlayerManagement.Instance != null ? PlayerManagement.Instance.GetCurrentHealth() : 0);
        UpdateStats();
    }

    // Public method to update the score UI
    public void UpdateScore(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {newScore}";
        }
        else
        {
            Debug.LogWarning("UIManager: scoreText is not assigned.");
        }
    }

    // Public method to update the health UI
    public void UpdateHealth(int newHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"Health: {newHealth}";
        }
        else
        {
            Debug.LogWarning("UIManager: healthText is not assigned.");
        }
    }

    // Public method to update the stats UI
    public void UpdateStats()
    {
        if (statsText != null && PlayerManagement.Instance != null)
        {
            statsText.text = $"Stats:\nAttack Damage: {PlayerManagement.Instance.attackDamage}\nBullet Speed: {PlayerManagement.Instance.bulletSpeed}";
        }
        else
        {
            Debug.LogWarning("UIManager: statsText is not assigned or PlayerManagement.Instance is null.");
        }
    }

    // Subscribe to PlayerManagement events when UIManager is enabled
    private void OnEnable()
    {
        if (PlayerManagement.Instance != null)
        {
            PlayerManagement.Instance.OnScoreChanged += UpdateScore;
            PlayerManagement.Instance.OnHealthChanged += UpdateHealth;
            PlayerManagement.Instance.OnStatsChanged += UpdateStats;
        }
    }

    // Unsubscribe from PlayerManagement events when UIManager is disabled
    private void OnDisable()
    {
        if (PlayerManagement.Instance != null)
        {
            PlayerManagement.Instance.OnScoreChanged -= UpdateScore;
            PlayerManagement.Instance.OnHealthChanged -= UpdateHealth;
            PlayerManagement.Instance.OnStatsChanged -= UpdateStats;
        }
    }
}
