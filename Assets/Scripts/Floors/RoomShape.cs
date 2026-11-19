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
        public const int Size = 17;
        /// <summary>Metres per tile. Size × TileSize is the room's footprint on the floor grid.</summary>
        public const float TileSize = 2f;
        /// <summary>Tiles across a doorway, and across the tunnel that leads in from it.</summary>
        public const int DoorWidth = 3;
        const int Min = 1, Max = Size - 2; // the inside, without the border
        const int DoorLo = (Size - DoorWidth) / 2;

        public int seed;
        public Doors doors;
        public RoomType type;
        /// <summary>A boss room: the whole square, wide open.</summary>
        public bool isArena;
        /// <summary>One quarter of a big boss arena: open along the two sides it shares with the other quarters.</summary>
        public bool isArenaPart;
        /// <summary>Which quarter: (0,0) south-west to (1,1) north-east.</summary>
        public Vector2Int arenaQuadrant;
        public readonly bool[,] open = new bool[Size, Size];
        /// <summary>Middle of the main chamber. It and the eight tiles around it are always open and free of pillars.</summary>
        public Vector2Int centre;
        /// <summary>Open tiles inside the border: places things can stand.</summary>
        public readonly List<Vector2Int> interior = new List<Vector2Int>();
        /// <summary>Open tiles with a pebble on them: low cover to fight around. Never touching a wall or each other, so there is always a way past.</summary>
        public readonly List<Vector2Int> cover = new List<Vector2Int>();
        /// <summary>Open tiles with nothing on them: where enemies can appear.</summary>
        public readonly List<Vector2Int> free = new List<Vector2Int>();
        /// <summary>What kind of fight room this is: what its cover is made of, and anything extra in it.</summary>
        public RoomFeature feature;
        /// <summary>Dew rooms only: the marked tiles droplets fall on. Still free floor.</summary>
        public readonly List<Vector2Int> spots = new List<Vector2Int>();
        /// <summary>Tiles against a wall where a clump of glowing mushrooms grows: the room's lamps. Still free floor.</summary>
        public readonly List<Vector2Int> glows = new List<Vector2Int>();
        /// <summary>Out of 100: how many fight rooms with exactly two doorways are tunnel bends.</summary>
        public const int BendChance = 25;
        /// <summary>For tests and trying things out: every fight room gets this feature.</summary>
        public static RoomFeature? ForceFeature;

        /// <summary>One of the border tiles that form the doorway on a side (0 North, 1 East, 2 South, 3 West). index runs 0 to DoorWidth - 1.</summary>
        public static Vector2Int DoorTile(int side, int index)
        {
            int along = DoorLo + index;
            switch (side)
            {
                case 0: return new Vector2Int(along, Size - 1);
                case 1: return new Vector2Int(Size - 1, along);
                case 2: return new Vector2Int(along, 0);
                default: return new Vector2Int(0, along);
            }
        }

        bool HasDoor(int side) => (doors & (Doors)(1 << side)) != 0;

        public int DoorCount
        {
            get
            {
                int count = 0;
                for (int side = 0; side < 4; side++) if (HasDoor(side)) count++;
                return count;
            }
        }

        void OpenDoorway(int side)
        {
            for (int i = 0; i < DoorWidth; i++)
            {
                Vector2Int tile = DoorTile(side, i);
                open[tile.x, tile.y] = true;
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

            // Floors 1-5 keep chambers on the small side; deeper floors use the big ones.
            int floor = FloorLayout.FloorInBiome(floorNumber);
            float size = floor <= 5 ? (float)rng.NextDouble() * 0.35f : 0.5f + (float)rng.NextDouble() * 0.5f;

            // A big arena: this square is one quarter of it. Each boss always fights in the same place.
            if (layout.InArena(node.cell))
            {
                Vector2Int quadrant = node.cell - layout.arenaOrigin;
                Doors outer = node.doors;
                outer &= quadrant.x == 0 ? ~Doors.East : ~Doors.West;
                outer &= quadrant.y == 0 ? ~Doors.North : ~Doors.South;
                return ArenaPart(seed, outer, node.type, quadrant, floor == 10 ? ArenaKind.ThroneHall : ArenaKind.Nursery, Mathf.Abs(layout.seed % 3));
            }

            // Where there was no space for the big one, the exit room of a boss floor is still an arena: the whole square, wide open.
            bool arena = node.type == RoomType.Exit && FloorLayout.IsBossFloor(floorNumber);
            return Generate(seed, node.doors, node.type, size, arena);
        }

        public enum ArenaKind
        {
            /// <summary>The mini boss: a round chamber 52 m across, pillars to circle round, pods along the walls.</summary>
            Nursery,
            /// <summary>The biome boss: a hall 44 m by 56 m with two rows of columns.</summary>
            ThroneHall
        }

        /// <summary>Tiles along each side of a whole arena: two rooms' worth.</summary>
        public const int ArenaSize = Size * 2;

        /// <summary>
        /// Whether a tile of the whole arena is open floor. x and y count from its south-west corner. An arena is laid
        /// out by hand, not carved: the same boss always fights in the same place. The variant (0 to 2) only moves the
        /// pillars or takes some columns away. Nothing solid ever sits on the seams between the four quarters.
        /// </summary>
        public static bool ArenaOpen(ArenaKind kind, int variant, int x, int y)
        {
            if (x < 1 || y < 1 || x > ArenaSize - 2 || y > ArenaSize - 2) return false;
            float dx = Mathf.Abs(x - (ArenaSize - 1) * 0.5f), dy = Mathf.Abs(y - (ArenaSize - 1) * 0.5f);

            if (kind == ArenaKind.Nursery)
            {
                if (dx * dx + dy * dy > 13.3f * 13.3f) return false;
                switch (variant)
                {
                    case -1: return true; // the bare outline, with nothing standing in it
                    case 0: return !(Between(dx, 5f, 7f) && Between(dy, 5f, 7f));          // four pillars close in
                    case 1: return !(Between(dx, 7f, 9f) && Between(dy, 7f, 9f));          // four pillars far out
                    default: return !((Between(dx, 3f, 5f) && Between(dy, 8f, 10f)) || (Between(dx, 8f, 10f) && Between(dy, 3f, 5f))); // a ring of eight
                }
            }

            if (dx > 11f || dy > 14f) return false;
            bool column = variant >= 0 && Between(dx, 6f, 8f) && ((variant != 2 && Between(dy, 4f, 6f)) || (variant != 1 && Between(dy, 10f, 12f)));
            return !column;
        }

        /// <summary>The nursery's pods: eight tiles round the wall, the same in every variant.</summary>
        public static bool ArenaPod(ArenaKind kind, int x, int y)
        {
            if (kind != ArenaKind.Nursery) return false;
            float dx = Mathf.Abs(x - (ArenaSize - 1) * 0.5f), dy = Mathf.Abs(y - (ArenaSize - 1) * 0.5f);
            return (dx == 11.5f && dy == 2.5f) || (dx == 2.5f && dy == 11.5f);
        }

        static bool Between(float value, float low, float high) => value >= low && value <= high;

        /// <summary>One quarter of a big arena, as the room for one grid square.</summary>
        /// <param name="doors">Only the doorways to rooms outside the arena.</param>
        public static RoomShape ArenaPart(int seed, Doors doors, RoomType type, Vector2Int quadrant, ArenaKind kind, int variant)
        {
            var shape = new RoomShape { seed = seed, doors = doors, type = type, isArena = true, isArenaPart = true, arenaQuadrant = quadrant };
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    shape.open[x, y] = ArenaOpen(kind, variant, quadrant.x * Size + x, quadrant.y * Size + y);

            // Its own middle is tucked into the corner nearest the middle of the whole arena.
            shape.centre = new Vector2Int(quadrant.x == 0 ? Size - 3 : 2, quadrant.y == 0 ? Size - 3 : 2);
            for (int x = shape.centre.x - 1; x <= shape.centre.x + 1; x++)
                for (int y = shape.centre.y - 1; y <= shape.centre.y + 1; y++)
                    shape.open[x, y] = true;

            for (int side = 0; side < 4; side++)
            {
                if (!shape.HasDoor(side)) continue;
                shape.OpenDoorway(side);
                shape.CarveTunnel(side, shape.centre.x, shape.centre.y);
            }

            // A tunnel in from a doorway may cut across the arena's floor, but it never eats a pillar or a column.
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    int gx = quadrant.x * Size + x, gy = quadrant.y * Size + y;
                    if (shape.open[x, y] && !ArenaOpen(kind, variant, gx, gy) && ArenaOpen(kind, -1, gx, gy)) shape.open[x, y] = false;
                }
            }
            shape.CollectInterior();

            for (int x = Min; x <= Max; x++)
            {
                for (int y = Min; y <= Max; y++)
                {
                    if (shape.open[x, y] && ArenaPod(kind, quadrant.x * Size + x, quadrant.y * Size + y)) shape.cover.Add(new Vector2Int(x, y));
                }
            }
            if (shape.cover.Count > 0)
            {
                shape.free.RemoveAll(shape.cover.Contains);
                shape.feature = RoomFeature.Nursery;
            }
            shape.PlaceGlows();
            return shape;
        }

        // The side of this quarter that opens onto another quarter of the arena.
        bool IsSeam(int x, int y) =>
            isArenaPart && ((x == (arenaQuadrant.x == 0 ? Size - 1 : 0)) || (y == (arenaQuadrant.y == 0 ? Size - 1 : 0)));

        /// <param name="size01">0 = the smallest chamber, 1 = the largest.</param>
        /// <param name="arena">True for a boss room: every inside tile open, no cover.</param>
        public static RoomShape Generate(int seed, Doors doors, RoomType type, float size01, bool arena = false)
        {
            var rng = new System.Random(seed);
            var shape = new RoomShape { seed = seed, doors = doors, type = type, isArena = arena };
            bool fight = (type == RoomType.Normal || type == RoomType.Exit) && !arena;

            // One through-room in four is a tunnel bend: a small chamber tucked as far from both doorways as it will
            // go, so the tunnels to it are long and turn a corner. Decided on its own dice, before anything is carved.
            int doorCount = 0;
            for (int side = 0; side < 4; side++) if (shape.HasDoor(side)) doorCount++;
            bool bend = fight && type == RoomType.Normal && doorCount == 2 && new System.Random(seed ^ 0x2545f491).Next(100) < BendChance;

            if (arena)
            {
                for (int x = Min; x <= Max; x++)
                    for (int y = Min; y <= Max; y++)
                        shape.open[x, y] = true;
                shape.centre = new Vector2Int(Size / 2, Size / 2);
                for (int side = 0; side < 4; side++)
                {
                    if (shape.HasDoor(side)) shape.OpenDoorway(side);
                }
                shape.CollectInterior();
                shape.PlaceGlows();
                return shape;
            }

            // 1. The main chamber: an oval or a box somewhere inside the room.
            // Half-widths in tiles: a fight chamber is 9 to 15 tiles across (18 to 30 m), the quiet rooms 7.
            int half = bend ? 2 : fight ? Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(4f, 7f, size01)), 4, 7) : 3;
            int halfX = half, halfY = half;
            if (fight && half > 4)
            {
                int squash = rng.Next(3);
                int by = rng.Next(1, 3);
                if (squash == 0) halfX = Mathf.Max(4, halfX - by);
                else if (squash == 1) halfY = Mathf.Max(4, halfY - by);
            }

            int cx = rng.Next(Min + halfX, Max - halfX + 1);
            int cy = rng.Next(Min + halfY, Max - halfY + 1);
            if (bend)
            {
                // Away from each doorway; with doorways facing each other, off to one side or the other.
                bool north = shape.HasDoor(0), east = shape.HasDoor(1), south = shape.HasDoor(2), west = shape.HasDoor(3);
                int low = Min + half, high = Max - half;
                cy = north && south ? cy : north ? low : south ? high : (rng.Next(2) == 0 ? low : high);
                cx = east && west ? cx : east ? low : west ? high : (rng.Next(2) == 0 ? low : high);
                if (north && south) cx = rng.Next(2) == 0 ? low : high;
                if (east && west) cy = rng.Next(2) == 0 ? low : high;
            }
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
            if (fight && !bend)
            {
                int pockets = rng.Next(0, size01 < 0.4f ? 3 : 4);
                for (int i = 0; i < pockets; i++)
                {
                    var from = shape.RandomOpen(rng);
                    int px = Mathf.Clamp(from.x + rng.Next(-3, 4), Min, Max);
                    int py = Mathf.Clamp(from.y + rng.Next(-3, 4), Min, Max);
                    int radius = rng.Next(1, 4);
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

            // 3. Tunnels: a path as wide as the doorway, from each doorway to the chamber.
            for (int side = 0; side < 4; side++)
            {
                if (!shape.HasDoor(side)) continue;
                shape.OpenDoorway(side);
                shape.CarveTunnel(side, cx, cy);
            }

            // A pocket can land just clear of everything else. Anything that can't be walked to is filled back in.
            shape.FillUnreachable();

            // 4. Pillars: lumps of earth left standing as cover, as long as they don't cut anything off.
            if (fight && !bend)
            {
                int tries = rng.Next(0, 4 + half * 2);
                for (int i = 0; i < tries; i++)
                {
                    var tile = shape.RandomOpen(rng);
                    if (!shape.CanBePillar(tile)) continue;
                    shape.open[tile.x, tile.y] = false;
                    if (!shape.IsConnected()) shape.open[tile.x, tile.y] = true;
                }
            }

            shape.CollectInterior();

            // 5. Pebbles: low cover dotted about the floor, more of it in bigger rooms.
            if (fight)
            {
                int tries = shape.interior.Count / 5;
                for (int i = 0; i < tries; i++)
                {
                    var tile = shape.interior[rng.Next(shape.interior.Count)];
                    if (shape.CanHoldCover(tile)) shape.cover.Add(tile);
                }
                shape.free.RemoveAll(shape.cover.Contains);
            }

            shape.PlaceGlows();

            // 6. What kind of room it is. Its own dice, so adding a feature never changes the shape of any room.
            if (fight && type == RoomType.Normal)
            {
                var dice = new System.Random(seed ^ 0x3c6ef372);
                int roll = dice.Next(100);
                shape.feature = ForceFeature ?? (roll < 40 ? RoomFeature.Open : roll < 60 ? RoomFeature.FoodStore
                    : roll < 75 ? RoomFeature.RootHall : roll < 90 ? RoomFeature.Nursery : RoomFeature.DewRoom);
                // A bend is a bend whatever else was rolled or asked for: the shape must never depend on the feature.
                if (bend || shape.feature == RoomFeature.TunnelBend) shape.feature = bend ? RoomFeature.TunnelBend : RoomFeature.Open;

                if (shape.feature == RoomFeature.DewRoom)
                {
                    for (int i = 0; i < 14 && shape.spots.Count < 5; i++)
                    {
                        var tile = shape.free[dice.Next(shape.free.Count)];
                        if (shape.CanHoldSpot(tile)) shape.spots.Add(tile);
                    }
                    if (shape.spots.Count == 0) shape.feature = RoomFeature.Open;
                }
                else if (shape.cover.Count == 0)
                {
                    // Nothing to make the feature out of.
                    shape.feature = RoomFeature.Open;
                }
            }
            return shape;
        }

        bool CanHoldSpot(Vector2Int tile)
        {
            if (!CanBePillar(tile)) return false;
            foreach (var other in spots)
            {
                if (Mathf.Abs(tile.x - other.x) <= 2 && Mathf.Abs(tile.y - other.y) <= 2) return false;
            }
            return true;
        }

        bool CanHoldCover(Vector2Int tile)
        {
            // Clear of the middle (the exit hole opens there) and of the doorways...
            if (Mathf.Abs(tile.x - centre.x) <= 2 && Mathf.Abs(tile.y - centre.y) <= 2) return false;
            if (!CanBePillar(tile)) return false;

            // ...with open floor all the way round it...
            for (int x = tile.x - 1; x <= tile.x + 1; x++)
                for (int y = tile.y - 1; y <= tile.y + 1; y++)
                    if (!open[x, y]) return false;

            // ...and not right next to another one.
            foreach (var other in cover)
            {
                if (Mathf.Abs(tile.x - other.x) <= 2 && Mathf.Abs(tile.y - other.y) <= 2) return false;
            }
            return true;
        }

        // Up to three lamps, spread out, each on a tile with a wall beside it. Their own dice again.
        void PlaceGlows()
        {
            var dice = new System.Random(seed ^ 0x1b873593);
            var edge = new List<Vector2Int>();
            foreach (var tile in free)
            {
                if (WallSide(tile) >= 0 && (isArena || CanBePillar(tile))) edge.Add(tile);
            }
            for (int i = 0; i < 40 && glows.Count < 3 && edge.Count > 0; i++)
            {
                var tile = edge[dice.Next(edge.Count)];
                bool crowded = false;
                foreach (var other in glows) crowded |= Mathf.Abs(tile.x - other.x) + Mathf.Abs(tile.y - other.y) < 6;
                if (!crowded) glows.Add(tile);
            }
        }

        /// <summary>The side of a tile that has solid earth against it (0 North, 1 East, 2 South, 3 West), or -1 if none.</summary>
        public int WallSide(Vector2Int tile)
        {
            for (int side = 0; side < 4; side++)
            {
                Vector2Int next = tile + FloorLayout.Directions[side];
                if (next.x >= 0 && next.y >= 0 && next.x < Size && next.y < Size && !open[next.x, next.y]) return side;
            }
            return -1;
        }

        Vector2Int RandomOpen(System.Random rng)
        {
            var tiles = new List<Vector2Int>();
            for (int x = Min; x <= Max; x++)
                for (int y = Min; y <= Max; y++)
                    if (open[x, y]) tiles.Add(new Vector2Int(x, y));
            return tiles[rng.Next(tiles.Count)];
        }

        // A square brush as wide as the doorway walks from just inside it to the chamber centre: straight in first, then across.
        void CarveTunnel(int side, int cx, int cy)
        {
            const int last = Max - (DoorWidth - 1); // furthest the brush's corner can go
            int x, y;
            switch (side)
            {
                case 0: x = DoorLo; y = last; break;
                case 1: x = last; y = DoorLo; break;
                case 2: x = DoorLo; y = Min; break;
                default: x = Min; y = DoorLo; break;
            }
            int tx = Mathf.Clamp(cx - DoorWidth / 2, Min, last), ty = Mathf.Clamp(cy - DoorWidth / 2, Min, last);
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
            for (int dx = 0; dx < DoorWidth; dx++)
                for (int dy = 0; dy < DoorWidth; dy++)
                    open[x + dx, y + dy] = true;
        }

        bool CanBePillar(Vector2Int tile)
        {
            // Not in the clear middle...
            if (Mathf.Abs(tile.x - centre.x) <= 1 && Mathf.Abs(tile.y - centre.y) <= 1) return false;

            // ...and not near a doorway, where it would block the way in.
            for (int side = 0; side < 4; side++)
            {
                if (!HasDoor(side)) continue;
                for (int i = 0; i < DoorWidth; i++)
                {
                    Vector2Int door = DoorTile(side, i);
                    if (Mathf.Abs(tile.x - door.x) + Mathf.Abs(tile.y - door.y) <= 3) return false;
                }
            }
            return true;
        }

        void CollectInterior()
        {
            interior.Clear();
            for (int x = Min; x <= Max; x++)
                for (int y = Min; y <= Max; y++)
                    if (open[x, y]) interior.Add(new Vector2Int(x, y));
            free.Clear();
            free.AddRange(interior);
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
                if (!HasDoor(side)) continue;
                for (int i = 0; i < DoorWidth; i++)
                {
                    Vector2Int door = DoorTile(side, i);
                    doorTiles.Add(door);
                    if (!open[door.x, door.y]) problems.Add($"doorway {(Doors)(1 << side)} is blocked");
                }
            }

            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    bool border = x == 0 || y == 0 || x == Size - 1 || y == Size - 1;
                    if (border && open[x, y] && !doorTiles.Contains(new Vector2Int(x, y)) && !IsSeam(x, y)) problems.Add($"hole in the outer wall at {x},{y}");
                }
            }

            if (!IsConnected()) problems.Add("part of the room cannot be reached");

            foreach (var tile in glows)
            {
                if (!free.Contains(tile)) problems.Add($"glow at {tile.x},{tile.y} is not on free floor");
                if (WallSide(tile) < 0) problems.Add($"glow at {tile.x},{tile.y} has no wall to grow against");
            }
            if (glows.Count == 0) problems.Add("a room with no lamps");
            foreach (var tile in spots)
            {
                if (!free.Contains(tile)) problems.Add($"dew spot at {tile.x},{tile.y} is not on free floor");
            }
            if (feature == RoomFeature.DewRoom && spots.Count == 0) problems.Add("dew room with nowhere for the dew to fall");
            if (feature == RoomFeature.TunnelBend && (interior.Count > 110 || DoorCount != 2))
                problems.Add($"a tunnel bend with {interior.Count} open tiles and {DoorCount} doorways");
            if ((feature == RoomFeature.FoodStore || feature == RoomFeature.RootHall || feature == RoomFeature.Nursery) && cover.Count == 0)
                problems.Add($"{feature} with no cover tiles to build it from");

            foreach (var tile in cover)
            {
                if (Mathf.Abs(tile.x - centre.x) <= 2 && Mathf.Abs(tile.y - centre.y) <= 2) problems.Add($"cover at {tile.x},{tile.y} is in the middle");
                if (free.Contains(tile)) problems.Add($"cover at {tile.x},{tile.y} is also listed as free floor");
                for (int x = tile.x - 1; x <= tile.x + 1; x++)
                    for (int y = tile.y - 1; y <= tile.y + 1; y++)
                    {
                        if (!open[x, y]) problems.Add($"cover at {tile.x},{tile.y} touches a wall");
                        else if ((x != tile.x || y != tile.y) && cover.Contains(new Vector2Int(x, y))) problems.Add($"cover at {tile.x},{tile.y} touches more cover");
                    }
            }
            if (interior.Count < 12) problems.Add($"only {interior.Count} open tiles");
            return problems;
        }

        /// <summary>Text picture: # earth, . floor, o cover, ~ dew spot, C centre, D doorway.</summary>
        public string ToAscii()
        {
            var text = new StringBuilder();
            for (int y = Size - 1; y >= 0; y--)
            {
                for (int x = 0; x < Size; x++)
                {
                    bool border = x == 0 || y == 0 || x == Size - 1 || y == Size - 1;
                    char symbol = !open[x, y] ? '#' : border ? 'D' : (x == centre.x && y == centre.y) ? 'C' : cover.Contains(new Vector2Int(x, y)) ? 'o' : spots.Contains(new Vector2Int(x, y)) ? '~' : '.';
                    text.Append(symbol).Append(' ');
                }
                text.AppendLine();
            }
            return text.ToString();
        }
    }
}
