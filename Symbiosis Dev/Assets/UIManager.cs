using UnityEngine;
using TMPro; // if using TextMeshPro

public class UIManager : MonoBehaviour
{
    // Singleton instance (optional but common)
    public static UIManager Instance;

    // Assign in Inspector
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI healthText;

    private void Awake()
    {
        // Basic singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        // Make sure this object persists between scenes if desired
        // DontDestroyOnLoad(gameObject);
    }

    // Public methods to update UI
    public void UpdateScore(int newScore)
    {
        scoreText.text = "Score: " + newScore;
    }

    public void UpdateHealth(int newHealth)
    {
        healthText.text = "Health: " + newHealth;
    }
}
