// Assets/Scripts/Managers/RespawnManager.cs
using UnityEngine;
using System.Collections;

public class RespawnManager : MonoBehaviour
{
    // Singleton instance
    public static RespawnManager Instance { get; private set; }

    private void Awake()
    {
        // Implement Singleton Pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Optionally persist across scenes
        // DontDestroyOnLoad(gameObject);
    }

    // Method to respawn an item after a delay
    public void RespawnItem(GameObject item, float delay)
    {
        StartCoroutine(RespawnCoroutine(item, delay));
    }

    private IEnumerator RespawnCoroutine(GameObject item, float delay)
    {
        yield return new WaitForSeconds(delay);
        item.SetActive(true);
    }
}
