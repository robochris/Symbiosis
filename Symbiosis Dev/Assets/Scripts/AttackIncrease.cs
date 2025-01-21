using UnityEngine;
using System.Collections;

public class ItemPickup : MonoBehaviour
{
    public float respawnTime = 3f;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (PlayerManagement.Instance == null)
                Debug.LogError("PlayerManagement.Instance is null!");
            if (RespawnManager.Instance == null)
                Debug.LogError("RespawnManager.Instance is null!");

            if (PlayerManagement.Instance != null)
                PlayerManagement.Instance.UpdateAttackDamage(10);

            gameObject.SetActive(false);

            if (RespawnManager.Instance != null)
                RespawnManager.Instance.RespawnItem(gameObject, respawnTime);
        }
    }

}
