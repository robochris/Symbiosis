// Assets/Scripts/Traits/BulletSpeedTrait.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New Bullet Speed Trait", menuName = "Traits/BulletSpeed")]
public class BulletSpeedTrait : Trait
{
    [Header("Bullet Speed Settings")]
    public float speedIncrease = 5f;

    public override void Apply(PlayerManagement player)
    {
        player.UpgradeBulletSpeed(speedIncrease);
    }
}
