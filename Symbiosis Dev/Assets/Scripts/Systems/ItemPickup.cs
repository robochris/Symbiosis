// Assets/Scripts/Systems/ItemPickup.cs
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Trait Settings")]
    public Trait trait; // Assign via Inspector

    [Header("Respawn Settings")]
    public float respawnTime = 3f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (PlayerManagement.Instance == null)
            {
                Debug.LogError("ItemPickup: PlayerManagement.Instance is null!");
                return;
            }

            if (RespawnManager.Instance == null)
            {
                Debug.LogError("ItemPickup: RespawnManager.Instance is null!");
                return;
            }

            if (trait != null)
            {
                trait.Apply(PlayerManagement.Instance);
                Debug.Log($"ItemPickup: Applied {trait.traitName} to player.");
            }
            else
            {
                Debug.LogWarning("ItemPickup: No trait assigned to this item.");
            }

            gameObject.SetActive(false);

            RespawnManager.Instance.RespawnItem(gameObject, respawnTime);
        }
    }
}
