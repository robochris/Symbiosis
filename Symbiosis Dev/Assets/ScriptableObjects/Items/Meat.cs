using UnityEngine;

[CreateAssetMenu(fileName = "Meat", menuName = "Traits/Meat", order = 4)]
public class MeatTrait : ItemData
{
    public float Healing = 5f;

    public override void Apply(PlayerManagement player)
    {
        Debug.Log("Meat Collected");

        // Heal the player by the amount specified in "Healing"
        player.Heal(Mathf.RoundToInt(Healing));
    }
}