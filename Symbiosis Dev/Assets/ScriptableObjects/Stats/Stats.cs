using UnityEngine;

[CreateAssetMenu(fileName = "BasePlayerStats", menuName = "Stats/BasePlayerStats", order = 1)]
public class Stats : ScriptableObject
{
    [Header("Health")]
    public int maxHealth = 100;

    [Header("Attack & Weapon")]
    public GameObject defaultBulletPrefab;
    public int attackDamage = 10;
    public float fireRate = 1f;
    public float bulletSpeed = 5f;

    [Header("Misc")]
    public int maxScore = 999999;
}
