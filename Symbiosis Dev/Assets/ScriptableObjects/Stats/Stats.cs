using UnityEngine;

[CreateAssetMenu(fileName = "Stats", menuName = "ScriptableObjects/Stats/BaseStats", order = 1)]
public class Stats : ScriptableObject
{
    public int maxHealth = 100;
    public int attackDamage = 0;
    public float bulletSpeed = 0f;
    public int maxScore = 9999;
    public float fireRate = 0f;
    // Add other common stats as needed
}
