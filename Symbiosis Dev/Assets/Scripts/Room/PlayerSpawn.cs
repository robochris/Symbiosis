using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;  // Assign your player prefab in the Inspector
    public Vector3 spawnPosition = new Vector3(35f, 1f, 35f); // Adjust Y as needed

    void Start()
    {
        Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
    }
}
