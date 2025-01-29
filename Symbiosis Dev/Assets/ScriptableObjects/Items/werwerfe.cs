using UnityEngine;

[CreateAssetMenu(fileName = "LightningBulletItem", menuName = "Items/LightningBulletItem", order = 5)]
public class asdasd : ItemData
{
        public int poisonDamageincrease = 5;

        public override void Apply(PlayerManagement player)
        {
            player.UpgradeAttackDamage(poisonDamageincrease);
        }
}
