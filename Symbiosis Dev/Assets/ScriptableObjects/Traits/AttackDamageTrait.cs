// Assets/Scripts/Traits/IncreaseAttackDamageTrait.cs
using UnityEngine;

[CreateAssetMenu(fileName = "IncreaseAttackDamage", menuName = "Traits/IncreaseAttackDamage", order = 2)]
public class IncreaseAttackDamageTrait : Trait
{
    public int attackDamageIncrease = 5;

    public override void Apply(PlayerManagement player)
    {
        player.UpgradeAttackDamage(attackDamageIncrease);
    }
}
