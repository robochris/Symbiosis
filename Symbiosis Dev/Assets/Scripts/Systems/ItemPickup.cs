// Assets/Scripts/Systems/ItemPickup.cs
using UnityEngine;
using System.Collections.Generic;

public class ItemPickup : MonoBehaviour
{
    [Header("Trait Settings")]
    [Tooltip("Assign one or more traits to this item.")]
    public List<Trait> traits = new List<Trait>(); // Assign via Inspector

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

            if (traits != null && traits.Count > 0)
            {
                foreach (var trait in traits)
                {
                    if (trait != null)
                    {
                        trait.Apply(PlayerManagement.Instance);
                        Debug.Log($"ItemPickup: Applied {trait.traitName} to player.");
                    }
                    else
                    {
                        Debug.LogWarning("ItemPickup: One of the traits is null.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("ItemPickup: No traits assigned to this item.");
            }

            gameObject.SetActive(false);

            RespawnManager.Instance.RespawnItem(gameObject, respawnTime);
        }
    }
}
