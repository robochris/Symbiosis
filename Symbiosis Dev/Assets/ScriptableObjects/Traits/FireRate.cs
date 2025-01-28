// Assets/Scripts/Traits/IncreaseIncreaseFireRate.cs
using UnityEngine;

[CreateAssetMenu(fileName = "IncreaseFireRate", menuName = "Traits/IncreaseFireRate", order = 3)]
public class IncreaseIncreaseFireRate : Trait
{
    public float fireRateIncrease = 2f;

    public override void Apply(PlayerManagement player)
    {
        player.UpgradeFireRate(fireRateIncrease);
    }
}
