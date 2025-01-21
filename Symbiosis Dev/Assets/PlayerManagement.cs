using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class PlayerManagement : MonoBehaviour
{
    public static PlayerManagement Instance;
    public int health = 100;
    public int attackDamage = 10;
    public float bulletSpeed = 10f;
    private void Awake()
    {
        if (Instance == null)
        {
            Debug.LogError("PlayerManagement.Instance is null!");
            Instance = this;
        }
    }
    void Start()
    {
        UIManager.Instance.UpdateHealth(health);
    }

    public void UpdateAttackDamage(int attackStat)
    {
        attackDamage += attackStat;
    }

    public void UpdateBulletSpeed(int bulletStat)
    {
        bulletSpeed += bulletStat;
    }
}
