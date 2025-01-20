using UnityEngine;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc; // if using TextMeshPro

public class UIManager : MonoBehaviour
{
    // Singleton instance (optional but common)
    public static UIManager Instance;

    // Assign in Inspector
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI Stats;

    private void Awake()
    {
        // Basic singleton pattern
        if (Instance == null)
        {
            Debug.LogError("UIManager.Instance is null!");
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


    void Update()
    {
        Stats.text = "Stats: \nAttack Damage: " + PlayerManagement.Instance.attackDamage+"\n Bullet Speed: "+PlayerManagement.Instance.bulletSpeed;
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
