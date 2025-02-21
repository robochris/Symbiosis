using UnityEngine;
using System.Collections.Generic;

public class RoomChainManager : MonoBehaviour
{
    public static RoomChainManager Instance;

    [Header("References")]
    [Tooltip("Drag your TileBasedRoomGenerator component here.")]
    public TileRoomGenerator roomGenerator;
    [Tooltip("Drag your Player transform (or parent) here.")]
    public Transform player;

    [Header("Room Sequence Settings")]
    [Tooltip("Maximum number of rooms before winning.")]
    public int maxRooms = 5;
    public bool lastDoorUsedIsForward;

    private List<int> roomSeeds = new List<int>();
    private int currentRoomIndex = -1;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // Generate the first room.
        GoForward();
    }

    public void GoForward()
    {
        if (currentRoomIndex + 1 >= maxRooms)
        {
            Debug.Log("Reached final room. You win!");
            return;
        }

        int newSeed = Random.Range(0, 9999999);
        currentRoomIndex++;
        if (currentRoomIndex >= roomSeeds.Count)
            roomSeeds.Add(newSeed);
        else
            roomSeeds[currentRoomIndex] = newSeed;

        // Clear old, then generate new
        roomGenerator.ClearRoom();
        roomGenerator.GenerateRoomWithSeed(newSeed);

        // Position the player in the center of the new room
        Vector3 spawnPos = new Vector3(
            (roomGenerator.totalWidth * roomGenerator.tileSize) / 2f,
            0f,
            (roomGenerator.totalHeight * roomGenerator.tileSize) / 2f
        );
        if (player.parent != null)
            player.parent.position = spawnPos - player.localPosition;
        else
            player.position = spawnPos;

        player.rotation = Quaternion.identity;
        Debug.Log("GoForward -> new room, seed = " + newSeed + ", player @ " + spawnPos);
    }

    public void GoBack()
    {
        if (currentRoomIndex <= 0)
        {
            Debug.Log("No previous room to go back to!");
            return;
        }

        currentRoomIndex--;
        int oldSeed = roomSeeds[currentRoomIndex];

        roomGenerator.ClearRoom();
        roomGenerator.GenerateRoomWithSeed(oldSeed);

        // Position the player center again
        Vector3 spawnPos = new Vector3(
            (roomGenerator.totalWidth * roomGenerator.tileSize) / 2f,
            0f,
            (roomGenerator.totalHeight * roomGenerator.tileSize) / 2f
        );
        if (player.parent != null)
            player.parent.position = spawnPos - player.localPosition;
        else
            player.position = spawnPos;

        // Example: face "backwards"
        player.rotation = Quaternion.Euler(0, 180f, 0);
        Debug.Log("GoBack -> old room, seed = " + oldSeed + ", player @ " + spawnPos);
    }
}
