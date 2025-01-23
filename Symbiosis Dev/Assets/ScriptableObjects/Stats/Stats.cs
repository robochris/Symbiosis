using UnityEngine;

[CreateAssetMenu(fileName = "Stats", menuName = "ScriptableObjects/Stats/BaseStats", order = 1)]
public class Stats : ScriptableObject
{
    public int maxHealth = 100;
    public int maxMana = 50;
    public int experience = 0;
    // Add other common stats as needed
}
