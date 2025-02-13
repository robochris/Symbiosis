using UnityEngine;
using System.Collections.Generic;

public class RoomChainManager : MonoBehaviour
{
    public static RoomChainManager Instance;

    [Header("References")]
    [Tooltip("Drag your TileBasedRoomGenerator component here.")]
    public TileBasedRoomGenerator roomGenerator;
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

        roomGenerator.DestroyCurrentRoom();
        roomGenerator.GenerateRoomWithSeed(newSeed);

        // Position the player.
        // (For this example, we assume the player spawns at the center of the room.)
        Vector3 spawnPos = new Vector3((roomGenerator.roomWidth * roomGenerator.tileSize) / 2f, 0, (roomGenerator.roomHeight * roomGenerator.tileSize) / 2f);
        spawnPos.y = 0;
        if (player.parent != null)
            player.parent.position = spawnPos - player.localPosition;
        else
            player.position = spawnPos;

        player.rotation = Quaternion.identity;
        Debug.Log("GoForward -> new room, player spawned at: " + spawnPos);
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

        roomGenerator.DestroyCurrentRoom();
        roomGenerator.GenerateRoomWithSeed(oldSeed);

        // Position the player (for this example, at the room center).
        Vector3 spawnPos = new Vector3((roomGenerator.roomWidth * roomGenerator.tileSize) / 2f, 0, (roomGenerator.roomHeight * roomGenerator.tileSize) / 2f);
        spawnPos.y = 0;
        if (player.parent != null)
            player.parent.position = spawnPos - player.localPosition;
        else
            player.position = spawnPos;

        player.rotation = Quaternion.Euler(0, 180f, 0);
        Debug.Log("GoBack -> old room, player spawned at: " + spawnPos);
    }
}
