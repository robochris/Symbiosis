public class RoomLayout
{
    public RoomTileType[,] grid;
    public int width, height;
    public RoomLayout(int w, int h)
    {
        width = w;
        height = h;
        grid = new RoomTileType[w, h];
    }
}