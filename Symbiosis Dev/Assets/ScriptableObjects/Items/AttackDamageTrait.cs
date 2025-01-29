// Assets/Scripts/Traits/IncreaseAttackDamageTrait.cs
using NUnit.Framework.Interfaces;
using UnityEngine;

[CreateAssetMenu(fileName = "IncreaseAttackDamage", menuName = "Traits/IncreaseAttackDamage", order = 2)]
public class IncreaseAttackDamageTrait : ItemData
{
    public int attackDamageIncrease = 5;

    public override void Apply(PlayerManagement player)
    {
        player.UpgradeAttackDamage(attackDamageIncrease);
    }
}
