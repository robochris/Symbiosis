// Assets/Scripts/Items/StatUpgradeItem.cs
using UnityEngine;

public class StatUpgradeItem : MonoBehaviour
{
    [Header("Upgrade Settings")]
    [SerializeField] private int attackDamageIncrease = 5;
    [SerializeField] private float bulletSpeedIncrease = 2f;
    [SerializeField] private float fireRateDecrease = 0.1f; // Optional: Decrease fire rate

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Access PlayerManagement singleton and apply upgrades
            if (PlayerManagement.Instance != null)
            {
                PlayerManagement.Instance.UpgradeAttackDamage(attackDamageIncrease);
                PlayerManagement.Instance.UpgradeBulletSpeed(bulletSpeedIncrease);
                PlayerManagement.Instance.UpgradeFireRate(fireRateDecrease); // Optional
                Destroy(gameObject); // Remove the item after collection
            }
            else
            {
                Debug.LogError("StatUpgradeItem: PlayerManagement instance not found.");
            }
        }
    }
}
