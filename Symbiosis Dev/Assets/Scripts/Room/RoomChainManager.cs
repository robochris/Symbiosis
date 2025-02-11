using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the sequence of rooms by storing seeds and allowing forward/back movement.
/// Attach this to a GameObject in your scene. Assign the 'roomGenerator' in the Inspector.
/// </summary>
public class RoomChainManager : MonoBehaviour
{
    public static RoomChainManager Instance;

    [Header("References")]
    public RoomGenerator roomGenerator; // Drag your RoomGenerator component here

    [Header("Room Sequence Settings")]
    [Tooltip("Maximum number of rooms you can go forward through before 'winning'.")]
    public int maxRooms = 5;

    // Store the random seeds for each room visited
    private List<int> roomSeeds = new List<int>();
    // Tracks which room index we're on
    private int currentRoomIndex = -1;

    private void Awake()
    {
        // Simple singleton pattern if you want to reference it from multiple places
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Generate the first room automatically at game start
        GoForward();
    }

    /// <summary>
    /// Move forward to the next room (create a new seed).
    /// </summary>
    public void GoForward()
    {
        // If we've hit the maxRooms limit, do something like show a "Win" screen
        if (currentRoomIndex + 1 >= maxRooms)
        {
            Debug.Log("Reached final room. You win!");
            // For example:
            // SceneManager.LoadScene("VictoryScene");
            return;
        }

        // 1) Create a new random seed
        int newSeed = Random.Range(0, 9999999);

        // 2) Increment room index
        currentRoomIndex++;

        // 3) If we've moved beyond the end of the stored seeds list, add the new seed
        //    Otherwise, if we had gone back before and now forward again, overwrite the old future path
        if (currentRoomIndex >= roomSeeds.Count)
        {
            roomSeeds.Add(newSeed);
        }
        else
        {
            roomSeeds[currentRoomIndex] = newSeed;
        }

        // 4) Destroy the existing room, then generate a new one with the new seed
        roomGenerator.DestroyCurrentRoom();
        roomGenerator.GenerateRoomWithSeed(newSeed);

        Debug.Log($"[RoomChainManager] Moved forward to room index {currentRoomIndex} with seed {newSeed}.");
    }

    /// <summary>
    /// Move back to the previous room (re-generate it from its stored seed).
    /// </summary>
    public void GoBack()
    {
        if (currentRoomIndex <= 0)
        {
            Debug.Log("No previous room to go back to!");
            return;
        }

        // 1) Decrement the index
        currentRoomIndex--;

        // 2) Retrieve that old seed from the list
        int oldSeed = roomSeeds[currentRoomIndex];

        // 3) Destroy current room, re-generate from oldSeed
        roomGenerator.DestroyCurrentRoom();
        roomGenerator.GenerateRoomWithSeed(oldSeed);

        Debug.Log($"[RoomChainManager] Moved back to room index {currentRoomIndex} with seed {oldSeed}.");
    }
}
