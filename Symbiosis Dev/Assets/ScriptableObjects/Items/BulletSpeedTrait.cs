// Assets/Scripts/Traits/IncreaseBulletSpeedTrait.cs
using UnityEngine;

[CreateAssetMenu(fileName = "IncreaseBulletSpeed", menuName = "Traits/IncreaseBulletSpeed", order = 3)]
public class IncreaseBulletSpeedTrait : ItemData
{
    public float bulletSpeedIncrease = 2f;

    public override void Apply(PlayerManagement player)
    {
        player.UpgradeBulletSpeed(bulletSpeedIncrease);
    }
}
