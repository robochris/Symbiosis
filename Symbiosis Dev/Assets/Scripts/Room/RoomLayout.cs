using UnityEngine;

public class RoomLayout
{
    public RoomTileType[,] grid;
    public int width;
    public int height;

    public RoomLayout(int width, int height)
    {
        this.width = width;
        this.height = height;
        grid = new RoomTileType[width, height];
    }

    /// <summary>
    /// Generates a basic room:
    /// - Walls on the border,
    /// - Floors inside,
    /// - Door in the center of the bottom wall.
    /// </summary>
    public void GenerateBasicRoom()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                    grid[x, y] = RoomTileType.Wall;
                else
                    grid[x, y] = RoomTileType.Floor;
            }
        }
        // Place a door at the middle of the bottom wall.
        int doorX = width / 2;
        grid[doorX, 0] = RoomTileType.Door;
    }
}
