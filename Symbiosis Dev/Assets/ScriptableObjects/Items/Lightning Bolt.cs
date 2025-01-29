using UnityEngine;

[CreateAssetMenu(fileName = "LightningBulletItem", menuName = "Items/LightningBulletItem", order = 5)]
public class LightningBulletItem : ItemData
{
    [Header("Lightning Bullet Settings")]
    public GameObject lightningBulletPrefab;
    public int extraDamage = 5;

    public override void Apply(PlayerManagement player)
    {
        var attackSystem = player.GetAttackSystem();
        if (attackSystem != null)
        {
            // Swap to the lightning bullet
            attackSystem.SetBulletPrefab(lightningBulletPrefab);
        } else
        {
            Debug.Log("attackSystem is null");
        }
        // Increase player's attack damage
        player.UpgradeAttackDamage(extraDamage);

        Debug.Log("LightningBulletItem: Applied lightning bullets + extra damage.");
    }
}
