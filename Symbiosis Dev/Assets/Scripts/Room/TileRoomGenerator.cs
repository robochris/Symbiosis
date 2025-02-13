using UnityEngine;
using System.Collections.Generic;

public class TileBasedRoomGenerator : MonoBehaviour
{
    [Header("Room Layout Settings")]
    public int roomWidth = 10;         // Number of grid cells in X
    public int roomHeight = 10;        // Number of grid cells in Z
    public float tileSize = 1f;        // 1 unit = 1 meter

    [Header("Room Pieces")]
    public List<RoomPiece> floorPieces;
    public List<RoomPiece> wallPieces;
    public List<RoomPiece> doorPieces;
    public List<RoomPiece> windowPieces;

    private GameObject roomParent;
    private bool[,] occupancy;         // Keeps track of occupied grid cells

    /// <summary>
    /// Generates the room using the current random state.
    /// </summary>
    public void GenerateRoom()
    {
        // Initialize occupancy grid
        occupancy = new bool[roomWidth, roomHeight];

        // Destroy any previous room
        if (roomParent != null)
            Destroy(roomParent);
        roomParent = new GameObject("GeneratedRoom");

        // Create a basic layout (walls on borders, floors inside, door at bottom center)
        RoomLayout layout = new RoomLayout(roomWidth, roomHeight);
        layout.GenerateBasicRoom();

        // Loop through each cell in the grid
        for (int x = 0; x < roomWidth; x++)
        {
            for (int y = 0; y < roomHeight; y++)
            {
                // Skip if cell is already occupied by a multi-tile prefab
                if (occupancy[x, y])
                    continue;

                // Get the tile type from the layout
                RoomTileType tileType = layout.grid[x, y];

                // Get a random piece for this tile type
                RoomPiece selectedPiece = GetRandomPieceForTileType(tileType);
                if (selectedPiece == null || selectedPiece.prefab == null)
                    continue;

                // Determine the footprint size of the selected piece
                float pieceWidth = selectedPiece.tileWidth;
                float pieceHeight = selectedPiece.tileHeight;

                // Ensure the piece fits within the grid boundaries
                if (x + pieceWidth > roomWidth || y + pieceHeight > roomHeight)
                    continue;

                // Check that all cells in the footprint are available
                bool spaceAvailable = true;
                for (int i = 0; i < pieceWidth; i++)
                {
                    for (int j = 0; j < pieceHeight; j++)
                    {
                        if (occupancy[x + i, y + j])
                        {
                            spaceAvailable = false;
                            break;
                        }
                    }
                    if (!spaceAvailable)
                        break;
                }
                if (!spaceAvailable)
                    continue;

                // Mark the cells as occupied
                for (int i = 0; i < pieceWidth; i++)
                {
                    for (int j = 0; j < pieceHeight; j++)
                    {
                        occupancy[x + i, y + j] = true;
                    }
                }

                // Calculate the placement position
                // (Center the prefab over its footprint)
                Vector3 pos = new Vector3(
                    (x + pieceWidth / 2f) * tileSize,
                    0,
                    (y + pieceHeight / 2f) * tileSize
                );

                // Instantiate the prefab
                Instantiate(selectedPiece.prefab, pos, Quaternion.identity, roomParent.transform);
            }
        }
    }

    /// <summary>
    /// Initializes the random seed then generates the room.
    /// </summary>
    /// <param name="seed">Random seed</param>
    public void GenerateRoomWithSeed(int seed)
    {
        Random.InitState(seed);
        GenerateRoom();
    }

    /// <summary>
    /// Destroys the current room.
    /// </summary>
    public void DestroyCurrentRoom()
    {
        if (roomParent != null)
        {
            Destroy(roomParent);
            roomParent = null;
        }
    }

    /// <summary>
    /// Chooses a random RoomPiece for a given tile type using weighted selection.
    /// </summary>
    /// <param name="tileType">The type of tile for which to select a piece</param>
    /// <returns>A RoomPiece matching the tile type</returns>
    private RoomPiece GetRandomPieceForTileType(RoomTileType tileType)
    {
        List<RoomPiece> piecesList = null;
        switch (tileType)
        {
            case RoomTileType.Floor:
                piecesList = floorPieces;
                break;
            case RoomTileType.Wall:
                piecesList = wallPieces;
                break;
            case RoomTileType.Door:
                piecesList = doorPieces;
                break;
            case RoomTileType.Window:
                piecesList = windowPieces;
                break;
        }

        if (piecesList == null || piecesList.Count == 0)
            return null;

        // Calculate total weight.
        float totalWeight = 0f;
        foreach (var piece in piecesList)
            totalWeight += piece.weight;

        // Select a random value and pick based on weight.
        float randomValue = Random.Range(0f, totalWeight);
        foreach (var piece in piecesList)
        {
            randomValue -= piece.weight;
            if (randomValue <= 0f)
                return piece;
        }
        return piecesList[piecesList.Count - 1];
    }
}
