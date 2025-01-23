// Assets/Scripts/Systems/RespawnManager.cs
using UnityEngine;
using System.Collections; // Required for IEnumerator

public class RespawnManager : MonoBehaviour
{
    // Singleton instance (optional)
    public static RespawnManager Instance { get; private set; }

    private void Awake()
    {
        // Implement Singleton Pattern (optional)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Optionally persist across scenes
        // DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Respawns the given item after a specified delay.
    /// </summary>
    /// <param name="item">The GameObject to respawn.</param>
    /// <param name="delay">Time in seconds before respawning.</param>
    public void RespawnItem(GameObject item, float delay)
    {
        if (item == null)
        {
            Debug.LogError("RespawnManager: Cannot respawn a null item.");
            return;
        }

        StartCoroutine(RespawnCoroutine(item, delay));
    }

    /// <summary>
    /// Coroutine that handles the respawning of an item.
    /// </summary>
    /// <param name="item">The GameObject to respawn.</param>
    /// <param name="delay">Time in seconds before respawning.</param>
    /// <returns></returns>
    private IEnumerator RespawnCoroutine(GameObject item, float delay)
    {
        yield return new WaitForSeconds(delay);
        item.SetActive(true);
        Debug.Log($"RespawnManager: {item.name} has respawned after {delay} seconds.");
    }
}
