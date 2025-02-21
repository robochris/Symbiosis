using UnityEngine;
using System.Collections.Generic;

public class TileRoomGenerator : MonoBehaviour
{
    [Header("Overall Grid Size")]
    public int totalWidth = 30;   // The overall grid we can place a room in
    public int totalHeight = 30;
    public float tileSize = 1f;

    [Header("Random Room Bounds")]
    public int minRoomWidth = 5;
    public int maxRoomWidth = 20;
    public int minRoomHeight = 5;
    public int maxRoomHeight = 20;

    [Header("Room Pieces (ScriptableObjects)")]
    public List<RoomPiece> floorPieces;
    public List<RoomPiece> wallPieces;
    // Add door/window pieces if you like

    // Data structures
    private GameObject roomParent;
    private RoomLayout layout;    // holds tile types
    private bool[,] occupancy;    // tracks footprints

    /// <summary>
    /// Clear the existing room from the scene.
    /// </summary>
    public void ClearRoom()
    {
        if (roomParent != null)
        {
            Destroy(roomParent);
            roomParent = null;
        }
    }

    /// <summary>
    /// Generate a random rectangular room using a seed, so we can reproduce it.
    /// </summary>
    public void GenerateRoomWithSeed(int seed)
    {
        Random.InitState(seed);
        ClearRoom();

        roomParent = new GameObject("GeneratedRoom (Seed " + seed + ")");

        // 1) Create a random rectangle for the "room" inside the total grid
        int roomW = Random.Range(minRoomWidth, Mathf.Min(maxRoomWidth, totalWidth));
        int roomH = Random.Range(minRoomHeight, Mathf.Min(maxRoomHeight, totalHeight));

        // 2) Create layout for the entire grid, default None
        layout = new RoomLayout(totalWidth, totalHeight);

        // 3) Fill [0..roomW-1, 0..roomH-1] as floor, except boundary = wall
        //    (You can also place it randomly within the total grid, e.g. offsetX, offsetY)
        for (int x = 0; x < roomW; x++)
        {
            for (int y = 0; y < roomH; y++)
            {
                bool isEdge = (x == 0 || x == roomW - 1 || y == 0 || y == roomH - 1);
                layout.grid[x, y] = isEdge ? RoomTileType.Wall : RoomTileType.Floor;
            }
        }

        // 4) Instantiate tiles
        occupancy = new bool[totalWidth, totalHeight];
        InstantiateTilesFromLayout();
    }

    /// <summary>
    /// Actually spawns the tile prefabs based on layout.grid.
    /// </summary>
    private void InstantiateTilesFromLayout()
    {
        for (int x = 0; x < layout.width; x++)
        {
            for (int y = 0; y < layout.height; y++)
            {
                RoomTileType tileType = layout.grid[x, y];
                if (tileType == RoomTileType.None) continue;

                // pick a scriptable object
                RoomPiece piece = GetRandomPiece(tileType);
                if (piece == null || piece.prefab == null) continue;

                int w = piece.Size.x;
                int h = piece.Size.y;

                // check bounds
                if (x + w > layout.width || y + h > layout.height)
                    continue;

                // check occupancy
                bool spaceFree = true;
                for (int ix = 0; ix < w; ix++)
                {
                    for (int iy = 0; iy < h; iy++)
                    {
                        if (occupancy[x + ix, y + iy])
                        {
                            spaceFree = false;
                            break;
                        }
                    }
                    if (!spaceFree) break;
                }
                if (!spaceFree) continue;

                // mark as occupied
                for (int ix = 0; ix < w; ix++)
                {
                    for (int iy = 0; iy < h; iy++)
                    {
                        occupancy[x + ix, y + iy] = true;
                    }
                }

                // spawn
                float worldX = (x + w / 2f) * tileSize;
                float worldZ = (y + h / 2f) * tileSize;
                Vector3 pos = new Vector3(worldX, 0f, worldZ);
                Instantiate(piece.prefab, pos, Quaternion.identity, roomParent.transform);
            }
        }
    }

    private RoomPiece GetRandomPiece(RoomTileType tileType)
    {
        List<RoomPiece> pool = null;
        switch (tileType)
        {
            case RoomTileType.Floor:
                pool = floorPieces;
                break;
            case RoomTileType.Wall:
                pool = wallPieces;
                break;
        }
        if (pool == null || pool.Count == 0) return null;

        // Weighted random
        float total = 0f;
        foreach (var p in pool) total += p.weight;
        float rand = Random.Range(0f, total);
        foreach (var p in pool)
        {
            rand -= p.weight;
            if (rand <= 0f)
                return p;
        }
        return pool[pool.Count - 1];
    }
}
