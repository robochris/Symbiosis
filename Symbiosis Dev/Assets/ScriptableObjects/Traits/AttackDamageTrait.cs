using UnityEngine;

[CreateAssetMenu(fileName = "New Attack Damage Trait", menuName = "Traits/AttackDamage")]
public class AttackDamageTrait : Trait
{
    [Header("Attack Damage Settings")]
    public int damageIncrease = 10;

    public override void Apply(PlayerManagement player)
    {
        player.UpgradeAttackDamage(damageIncrease);
    }
}
