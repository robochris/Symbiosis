using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Shrunk
{
    public enum RoomType { Start, Normal, Exit, Treasure, Shop }

    [Flags]
    public enum Doors { None = 0, North = 1, East = 2, South = 4, West = 8 }

    /// <summary>One square of the floor grid.</summary>
    public class RoomNode
    {
        public Vector2Int cell;
        public RoomType type = RoomType.Normal;
        /// <summary>Sides that open onto a neighbouring room.</summary>
        public Doors doors;
        /// <summary>Rooms walked through to get here from the start.</summary>
        public int distance;

        // Changes while the floor is played.
        public bool visited;
        public bool cleared;

        public int DoorCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < 4; i++) if ((doors & (Doors)(1 << i)) != 0) count++;
                return count;
            }
        }
    }

    /// <summary>
    /// A floor as a flat grid of rooms: no 3D, no Unity scene. Everything about the layout is decided here,
    /// so it can be checked and drawn in 2D before any rooms are built.
    /// </summary>
    public class FloorLayout
    {
        /// <summary>North, East, South, West: the same order as the Doors flags.</summary>
        public static readonly Vector2Int[] Directions =
        {
            new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0)
        };

        public int seed;
        public int targetRooms;
        public readonly Dictionary<Vector2Int, RoomNode> rooms = new Dictionary<Vector2Int, RoomNode>();
        /// <summary>Rooms in the order they were added; the start is first.</summary>
        public readonly List<RoomNode> ordered = new List<RoomNode>();
        public RoomNode start;
        public RoomNode exit;

        /// <summary>About 8 rooms on floor 1, growing to about 15 by floor 9.</summary>
        public static int RoomCountForFloor(int floorNumber)
        {
            float t = Mathf.Clamp01((floorNumber - 1) / 8f);
            return Mathf.RoundToInt(Mathf.Lerp(8f, 15f, t));
        }

        public IEnumerable<RoomNode> Neighbours(RoomNode node)
        {
            for (int i = 0; i < 4; i++)
            {
                if ((node.doors & (Doors)(1 << i)) != 0) yield return rooms[node.cell + Directions[i]];
            }
        }

        /// <summary>The same seed and room count always give the same floor.</summary>
        public static FloorLayout Generate(int seed, int targetRooms)
        {
            targetRooms = Mathf.Max(4, targetRooms);
            var rng = new System.Random(seed);
            var cells = new List<Vector2Int> { Vector2Int.zero };
            var taken = new HashSet<Vector2Int> { Vector2Int.zero };

            // Grow: pick any existing room, step one square in a random direction, add a room if the square is free.
            // Every new room touches an old one, so the floor is always connected.
            while (cells.Count < targetRooms)
            {
                Vector2Int to = cells[rng.Next(cells.Count)] + Directions[rng.Next(4)];
                if (taken.Add(to)) cells.Add(to);
            }

            // Treasure and shop need a dead end each. If the growth didn't leave two, add them.
            Dictionary<Vector2Int, int> distance;
            Vector2Int exitCell;
            List<Vector2Int> deadEnds;
            for (int guard = 0; ; guard++)
            {
                distance = Distances(cells, taken);
                exitCell = Farthest(cells, distance);
                deadEnds = DeadEnds(cells, taken, exitCell);
                if (deadEnds.Count >= 2 || guard >= 32) break;
                if (!TryAddDeadEnd(cells, taken, distance, rng)) break;
            }

            var layout = new FloorLayout { seed = seed, targetRooms = targetRooms };
            foreach (var cell in cells)
            {
                var node = new RoomNode { cell = cell, distance = distance[cell] };
                for (int i = 0; i < 4; i++)
                {
                    if (taken.Contains(cell + Directions[i])) node.doors |= (Doors)(1 << i);
                }
                layout.rooms.Add(cell, node);
                layout.ordered.Add(node);
            }

            layout.start = layout.rooms[Vector2Int.zero];
            layout.start.type = RoomType.Start;
            layout.exit = layout.rooms[exitCell];
            layout.exit.type = RoomType.Exit;

            // Shuffle the dead ends, then hand out treasure and shop.
            for (int i = deadEnds.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (deadEnds[i], deadEnds[j]) = (deadEnds[j], deadEnds[i]);
            }
            if (deadEnds.Count > 0) layout.rooms[deadEnds[0]].type = RoomType.Treasure;
            if (deadEnds.Count > 1) layout.rooms[deadEnds[1]].type = RoomType.Shop;

            return layout;
        }

        static int NeighbourCount(Vector2Int cell, HashSet<Vector2Int> taken)
        {
            int count = 0;
            foreach (var d in Directions) if (taken.Contains(cell + d)) count++;
            return count;
        }

        static Dictionary<Vector2Int, int> Distances(List<Vector2Int> cells, HashSet<Vector2Int> taken)
        {
            var distance = new Dictionary<Vector2Int, int> { { Vector2Int.zero, 0 } };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(Vector2Int.zero);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var d in Directions)
                {
                    var next = cell + d;
                    if (!taken.Contains(next) || distance.ContainsKey(next)) continue;
                    distance[next] = distance[cell] + 1;
                    queue.Enqueue(next);
                }
            }
            return distance;
        }

        static Vector2Int Farthest(List<Vector2Int> cells, Dictionary<Vector2Int, int> distance)
        {
            Vector2Int best = cells[0];
            foreach (var cell in cells) if (distance[cell] > distance[best]) best = cell;
            return best;
        }

        static List<Vector2Int> DeadEnds(List<Vector2Int> cells, HashSet<Vector2Int> taken, Vector2Int exitCell)
        {
            var result = new List<Vector2Int>();
            foreach (var cell in cells)
            {
                if (cell == Vector2Int.zero || cell == exitCell) continue;
                if (NeighbourCount(cell, taken) == 1) result.Add(cell);
            }
            return result;
        }

        // Adds a room on a free square that touches exactly one room. It prefers squares that don't turn an existing
        // dead end into a corridor and don't end up farther away than the exit.
        static bool TryAddDeadEnd(List<Vector2Int> cells, HashSet<Vector2Int> taken, Dictionary<Vector2Int, int> distance, System.Random rng)
        {
            int maxDistance = 0;
            foreach (var cell in cells) maxDistance = Mathf.Max(maxDistance, distance[cell]);

            var preferred = new List<Vector2Int>();
            var fallback = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int>();

            foreach (var cell in cells)
            {
                foreach (var d in Directions)
                {
                    var free = cell + d;
                    if (taken.Contains(free) || !seen.Add(free)) continue;
                    if (NeighbourCount(free, taken) != 1) continue;

                    Vector2Int owner = default;
                    foreach (var back in Directions) if (taken.Contains(free + back)) owner = free + back;

                    bool keepsDeadEnds = owner == Vector2Int.zero || NeighbourCount(owner, taken) >= 2;
                    bool notPastExit = distance[owner] + 1 <= maxDistance;
                    if (keepsDeadEnds && notPastExit) preferred.Add(free);
                    else fallback.Add(free);
                }
            }

            var pool = preferred.Count > 0 ? preferred : fallback;
            if (pool.Count == 0) return false;
            var chosen = pool[rng.Next(pool.Count)];
            taken.Add(chosen);
            cells.Add(chosen);
            return true;
        }

        /// <summary>Checks every rule a floor must obey. An empty list means the layout is good.</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            if (rooms.Count < targetRooms) problems.Add($"only {rooms.Count} rooms, wanted {targetRooms}");
            if (start == null || start.cell != Vector2Int.zero) problems.Add("no start room at the origin");
            if (exit == null) problems.Add("no exit room");

            var count = new Dictionary<RoomType, int>();
            foreach (RoomType type in Enum.GetValues(typeof(RoomType))) count[type] = 0;
            int maxDistance = 0;

            foreach (var node in ordered)
            {
                count[node.type]++;
                maxDistance = Mathf.Max(maxDistance, node.distance);

                for (int i = 0; i < 4; i++)
                {
                    bool hasDoor = (node.doors & (Doors)(1 << i)) != 0;
                    bool hasNeighbour = rooms.TryGetValue(node.cell + Directions[i], out var other);
                    if (hasDoor != hasNeighbour) problems.Add($"{node.cell}: door {(Doors)(1 << i)} does not match its neighbour");
                    if (hasNeighbour && (other.doors & (Doors)(1 << ((i + 2) % 4))) == 0) problems.Add($"{node.cell}: neighbour {other.cell} has no door back");
                }

                if ((node.type == RoomType.Treasure || node.type == RoomType.Shop) && node.DoorCount != 1)
                    problems.Add($"{node.type} at {node.cell} is not a dead end");
            }

            if (count[RoomType.Start] != 1) problems.Add($"{count[RoomType.Start]} start rooms");
            if (count[RoomType.Exit] != 1) problems.Add($"{count[RoomType.Exit]} exit rooms");
            if (count[RoomType.Treasure] != 1) problems.Add($"{count[RoomType.Treasure]} treasure rooms");
            if (count[RoomType.Shop] != 1) problems.Add($"{count[RoomType.Shop]} shop rooms");
            if (exit != null && exit.distance != maxDistance) problems.Add("exit is not the farthest room from the start");
            if (exit != null && exit == start) problems.Add("exit is the start room");

            // Connected: walking through doors from the start reaches every room.
            if (start != null)
            {
                var reached = new HashSet<RoomNode> { start };
                var queue = new Queue<RoomNode>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    foreach (var next in Neighbours(queue.Dequeue()))
                    {
                        if (reached.Add(next)) queue.Enqueue(next);
                    }
                }
                if (reached.Count != rooms.Count) problems.Add($"only {reached.Count} of {rooms.Count} rooms can be reached");
            }

            return problems;
        }

        /// <summary>Text picture of the floor: S start, E exit, T treasure, $ shop, o normal room.</summary>
        public string ToAscii()
        {
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var cell in rooms.Keys)
            {
                minX = Mathf.Min(minX, cell.x); maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y); maxY = Mathf.Max(maxY, cell.y);
            }

            var text = new StringBuilder();
            for (int y = maxY; y >= minY; y--)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    char symbol = '.';
                    if (rooms.TryGetValue(new Vector2Int(x, y), out var node))
                    {
                        switch (node.type)
                        {
                            case RoomType.Start: symbol = 'S'; break;
                            case RoomType.Exit: symbol = 'E'; break;
                            case RoomType.Treasure: symbol = 'T'; break;
                            case RoomType.Shop: symbol = '$'; break;
                            default: symbol = 'o'; break;
                        }
                    }
                    text.Append(symbol).Append(' ');
                }
                text.AppendLine();
            }
            return text.ToString();
        }
    }
}
