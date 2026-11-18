using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The shape of one room as a flat grid of tiles: open floor or solid earth. No 3D, no scene.
    /// Every room is carved fresh from a seed, so no two are the same, but every one obeys the same rules:
    /// doorways sit in fixed spots on the edge, everything open is connected, and the middle is always clear.
    /// </summary>
    public class RoomShape
    {
        /// <summary>Tiles along each side, including the one-tile border that holds the doorways.</summary>
        public const int Size = 12;
        /// <summary>Metres per tile. Size × TileSize is the room's footprint on the floor grid.</summary>
        public const float TileSize = 2f;
        const int Min = 1, Max = Size - 2; // the inside, without the border

        public int seed;
        public Doors doors;
        public RoomType type;
        /// <summary>A boss room: the whole square, wide open.</summary>
        public bool isArena;
        public readonly bool[,] open = new bool[Size, Size];
        /// <summary>Middle of the main chamber. It and the eight tiles around it are always open and free of pillars.</summary>
        public Vector2Int centre;
        /// <summary>Open tiles inside the border: places things can stand.</summary>
        public readonly List<Vector2Int> interior = new List<Vector2Int>();

        /// <summary>The two border tiles that form the doorway on a side (0 North, 1 East, 2 South, 3 West).</summary>
        public static void DoorTiles(int side, out Vector2Int a, out Vector2Int b)
        {
            const int lo = Size / 2 - 1, hi = Size / 2;
            switch (side)
            {
                case 0: a = new Vector2Int(lo, Size - 1); b = new Vector2Int(hi, Size - 1); break;
                case 1: a = new Vector2Int(Size - 1, lo); b = new Vector2Int(Size - 1, hi); break;
                case 2: a = new Vector2Int(lo, 0); b = new Vector2Int(hi, 0); break;
                default: a = new Vector2Int(0, lo); b = new Vector2Int(0, hi); break;
            }
        }

        /// <summary>Centre of a tile, measured from the middle of the room.</summary>
        public static Vector3 TileLocal(Vector2Int tile) =>
            new Vector3((tile.x - (Size - 1) * 0.5f) * TileSize, 0f, (tile.y - (Size - 1) * 0.5f) * TileSize);

        /// <summary>The shape for one room of a floor. The same floor seed always gives the same rooms.</summary>
        public static RoomShape GenerateFor(FloorLayout layout, RoomNode node, int floorNumber)
        {
            int seed = unchecked(layout.seed * 73856093 ^ node.cell.x * 19349663 ^ node.cell.y * 83492791);
            var rng = new System.Random(seed);

            // Floors 1-5 keep chambers small; deeper floors mix in big ones.
            int floor = FloorLayout.FloorInBiome(floorNumber);
            float size = floor <= 5 ? (float)rng.NextDouble() * 0.3f : 0.45f + (float)rng.NextDouble() * 0.55f;

            // The exit room of a boss floor is the boss's arena: the whole square, wide open.
            bool arena = node.type == RoomType.Exit && FloorLayout.IsBossFloor(floorNumber);
            return Generate(seed, node.doors, node.type, size, arena);
        }

        /// <param name="size01">0 = the smallest chamber, 1 = the largest.</param>
        /// <param name="arena">True for a boss room: every inside tile open, no cover.</param>
        public static RoomShape Generate(int seed, Doors doors, RoomType type, float size01, bool arena = false)
        {
            var rng = new System.Random(seed);
            var shape = new RoomShape { seed = seed, doors = doors, type = type, isArena = arena };
            bool fight = (type == RoomType.Normal || type == RoomType.Exit) && !arena;

            if (arena)
            {
                for (int x = Min; x <= Max; x++)
                    for (int y = Min; y <= Max; y++)
                        shape.open[x, y] = true;
                shape.centre = new Vector2Int(Size / 2, Size / 2);
                for (int side = 0; side < 4; side++)
                {
                    if ((doors & (Doors)(1 << side)) == 0) continue;
                    DoorTiles(side, out var a, out var b);
                    shape.open[a.x, a.y] = true;
                    shape.open[b.x, b.y] = true;
                }
                shape.CollectInterior();
                return shape;
            }

            // 1. The main chamber: an oval or a box somewhere inside the room.
            int half = fight ? Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, 4f, size01)), 2, 4) : 2;
            int halfX = half, halfY = half;
            if (fight && half > 2)
            {
                int squash = rng.Next(3);
                if (squash == 0) halfX--;
                else if (squash == 1) halfY--;
            }

            int cx = rng.Next(Min + halfX, Max - halfX + 1);
            int cy = rng.Next(Min + halfY, Max - halfY + 1);
            shape.centre = new Vector2Int(cx, cy);
            bool box = rng.Next(4) == 0;

            for (int x = cx - halfX; x <= cx + halfX; x++)
            {
                for (int y = cy - halfY; y <= cy + halfY; y++)
                {
                    float dx = (x - cx) / (halfX + 0.4f), dy = (y - cy) / (halfY + 0.4f);
                    if (box || dx * dx + dy * dy <= 1f) shape.open[x, y] = true;
                }
            }
            for (int x = cx - 1; x <= cx + 1; x++)
                for (int y = cy - 1; y <= cy + 1; y++)
                    shape.open[x, y] = true;

            // 2. Side pockets: a few round bulges that overlap the chamber, for an uneven outline.
            if (fight)
            {
                int pockets = rng.Next(0, size01 < 0.35f ? 2 : 3);
                for (int i = 0; i < pockets; i++)
                {
                    var from = shape.RandomOpen(rng);
                    int px = Mathf.Clamp(from.x + rng.Next(-2, 3), Min, Max);
                    int py = Mathf.Clamp(from.y + rng.Next(-2, 3), Min, Max);
                    int radius = rng.Next(1, 3);
                    for (int x = px - radius; x <= px + radius; x++)
                    {
                        for (int y = py - radius; y <= py + radius; y++)
                        {
                            if (x < Min || x > Max || y < Min || y > Max) continue;
                            if ((x - px) * (x - px) + (y - py) * (y - py) <= radius * radius + 1) shape.open[x, y] = true;
                        }
                    }
                }
            }

            // 3. Tunnels: a two-tile-wide path from each doorway to the chamber.
            for (int side = 0; side < 4; side++)
            {
                if ((doors & (Doors)(1 << side)) == 0) continue;
                DoorTiles(side, out var a, out var b);
                shape.open[a.x, a.y] = true;
                shape.open[b.x, b.y] = true;
                shape.CarveTunnel(side, cx, cy);
            }

            // A pocket can land just clear of everything else. Anything that can't be walked to is filled back in.
            shape.FillUnreachable();

            // 4. Pillars: lumps of earth left standing as cover, as long as they don't cut anything off.
            if (fight)
            {
                int tries = rng.Next(0, 6);
                for (int i = 0; i < tries; i++)
                {
                    var tile = shape.RandomOpen(rng);
                    if (!shape.CanBePillar(tile)) continue;
                    shape.open[tile.x, tile.y] = false;
                    if (!shape.IsConnected()) shape.open[tile.x, tile.y] = true;
                }
            }

            shape.CollectInterior();
            return shape;
        }

        Vector2Int RandomOpen(System.Random rng)
        {
            var tiles = new List<Vector2Int>();
            for (int x = Min; x <= Max; x++)
                for (int y = Min; y <= Max; y++)
                    if (open[x, y]) tiles.Add(new Vector2Int(x, y));
            return tiles[rng.Next(tiles.Count)];
        }

        // A 2×2 brush walks from just inside the doorway to the chamber centre: straight in first, then across.
        void CarveTunnel(int side, int cx, int cy)
        {
            const int lo = Size / 2 - 1;
            int x, y;
            switch (side)
            {
                case 0: x = lo; y = Max - 1; break;
                case 1: x = Max - 1; y = lo; break;
                case 2: x = lo; y = Min; break;
                default: x = Min; y = lo; break;
            }
            int tx = Mathf.Clamp(cx, Min, Max - 1), ty = Mathf.Clamp(cy, Min, Max - 1);
            bool vertical = side == 0 || side == 2;

            Stamp(x, y);
            for (int pass = 0; pass < 2; pass++)
            {
                if (vertical == (pass == 0))
                {
                    while (y != ty) { y += ty > y ? 1 : -1; Stamp(x, y); }
                }
                else
                {
                    while (x != tx) { x += tx > x ? 1 : -1; Stamp(x, y); }
                }
            }
        }

        void Stamp(int x, int y)
        {
            open[x, y] = open[x + 1, y] = open[x, y + 1] = open[x + 1, y + 1] = true;
        }

        bool CanBePillar(Vector2Int tile)
        {
            // Not in the clear middle...
            if (Mathf.Abs(tile.x - centre.x) <= 1 && Mathf.Abs(tile.y - centre.y) <= 1) return false;

            // ...and not near a doorway, where it would block the way in.
            for (int side = 0; side < 4; side++)
            {
                if ((doors & (Doors)(1 << side)) == 0) continue;
                DoorTiles(side, out var a, out var b);
                if (Mathf.Abs(tile.x - a.x) + Mathf.Abs(tile.y - a.y) <= 3) return false;
                if (Mathf.Abs(tile.x - b.x) + Mathf.Abs(tile.y - b.y) <= 3) return false;
            }
            return true;
        }

        void CollectInterior()
        {
            interior.Clear();
            for (int x = Min; x <= Max; x++)
                for (int y = Min; y <= Max; y++)
                    if (open[x, y]) interior.Add(new Vector2Int(x, y));
        }

        int OpenCount()
        {
            int count = 0;
            foreach (bool tile in open) if (tile) count++;
            return count;
        }

        /// <summary>True when every open tile can be walked to from the centre.</summary>
        public bool IsConnected()
        {
            Flood(out int reached);
            return reached == OpenCount();
        }

        void FillUnreachable()
        {
            bool[,] seen = Flood(out _);
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    if (open[x, y] && !seen[x, y]) open[x, y] = false;
        }

        // Marks every tile that can be walked to from the centre.
        bool[,] Flood(out int reached)
        {
            var seen = new bool[Size, Size];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(centre);
            seen[centre.x, centre.y] = true;
            reached = 0;

            while (queue.Count > 0)
            {
                var tile = queue.Dequeue();
                reached++;
                foreach (var d in FloorLayout.Directions)
                {
                    int x = tile.x + d.x, y = tile.y + d.y;
                    if (x < 0 || y < 0 || x >= Size || y >= Size || seen[x, y] || !open[x, y]) continue;
                    seen[x, y] = true;
                    queue.Enqueue(new Vector2Int(x, y));
                }
            }
            return seen;
        }

        /// <summary>Checks every rule a room must obey. An empty list means the shape is good.</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();

            for (int x = centre.x - 1; x <= centre.x + 1; x++)
                for (int y = centre.y - 1; y <= centre.y + 1; y++)
                    if (x < Min || x > Max || y < Min || y > Max || !open[x, y]) problems.Add($"middle tile {x},{y} is not open");

            var doorTiles = new HashSet<Vector2Int>();
            for (int side = 0; side < 4; side++)
            {
                if ((doors & (Doors)(1 << side)) == 0) continue;
                DoorTiles(side, out var a, out var b);
                doorTiles.Add(a);
                doorTiles.Add(b);
                if (!open[a.x, a.y] || !open[b.x, b.y]) problems.Add($"doorway {(Doors)(1 << side)} is blocked");
            }

            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    bool border = x == 0 || y == 0 || x == Size - 1 || y == Size - 1;
                    if (border && open[x, y] && !doorTiles.Contains(new Vector2Int(x, y))) problems.Add($"hole in the outer wall at {x},{y}");
                }
            }

            if (!IsConnected()) problems.Add("part of the room cannot be reached");
            if (interior.Count < 12) problems.Add($"only {interior.Count} open tiles");
            return problems;
        }

        /// <summary>Text picture: # earth, . floor, C centre, D doorway.</summary>
        public string ToAscii()
        {
            var text = new StringBuilder();
            for (int y = Size - 1; y >= 0; y--)
            {
                for (int x = 0; x < Size; x++)
                {
                    bool border = x == 0 || y == 0 || x == Size - 1 || y == Size - 1;
                    char symbol = !open[x, y] ? '#' : border ? 'D' : (x == centre.x && y == centre.y) ? 'C' : '.';
                    text.Append(symbol).Append(' ');
                }
                text.AppendLine();
            }
            return text.ToString();
        }
    }
}
