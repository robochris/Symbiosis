using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// A second storey: raised platforms in the corners of a world's fight rooms, each with a ramp up from the floor.
    /// Enemies are put on top as well as below, so some fights are fought up and down; shots tilt to reach them (see
    /// Weapon). The tops are walkable, baked into the floor's navmesh with everything else. Marks on the floor land
    /// on whichever surface is under them.
    /// </summary>
    public static class Ledges
    {
        public const float Height = 1.7f;
        /// <summary>The platform is this many tiles across; the ramp this many tiles long.</summary>
        public const int Span = 3, RampTiles = 2;

        struct Top
        {
            public Room room;
            public Bounds bounds;
        }

        static readonly List<Top> Tops = new List<Top>();

        /// <summary>How many platforms the floor has. For tests.</summary>
        public static int Count => Tops.Count;

        public static void Clear() => Tops.Clear();

        /// <summary>Puts one or two platforms in this room's corners, where there is the room for them.</summary>
        public static void Build(Room room, int seed, Material sides, Material top)
        {
            var shape = room.Shape;
            var rng = new System.Random(seed);
            var corners = new List<Vector2Int> { new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(1, 1) };
            for (int i = corners.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (corners[i], corners[j]) = (corners[j], corners[i]);
            }
            int wanted = 1 + rng.Next(2);
            int made = 0;
            // Each corner in turn, a little further in or out, the ramp either way, until one fits.
            foreach (var corner in corners)
            {
                if (made >= wanted) break;
                bool alongX = rng.Next(2) == 0;
                bool done = false;
                foreach (int reach in new[] { 4, 3, 5 })
                {
                    for (int turn = 0; turn < 2 && !done; turn++) done = TryBuild(room, shape, corner, reach, turn == 0 ? alongX : !alongX, sides, top);
                    if (done) break;
                }
                if (done) made++;
            }
        }

        static bool TryBuild(Room room, RoomShape shape, Vector2Int corner, int reach, bool rampAlongX, Material sides, Material topMaterial)
        {
            Vector2Int middle = shape.centre + corner * reach;
            int half = Span / 2;
            var tiles = new List<Vector2Int>();
            for (int x = -half; x <= half; x++)
                for (int y = -half; y <= half; y++)
                    tiles.Add(middle + new Vector2Int(x, y));
            // The ramp runs back toward the middle of the room.
            Vector2Int step = rampAlongX ? new Vector2Int(-corner.x, 0) : new Vector2Int(0, -corner.y);
            var ramp = new List<Vector2Int>();
            for (int i = 1; i <= RampTiles; i++) ramp.Add(middle + step * (half + i));
            // And a tile clear at its foot, to walk on from.
            var foot = middle + step * (half + RampTiles + 1);

            foreach (var tile in tiles) if (!Clear(shape, tile)) return false;
            foreach (var tile in ramp) if (!Clear(shape, tile)) return false;
            if (!Clear(shape, foot)) return false;

            float size = RoomShape.TileSize;
            Vector3 centre = room.TileWorld(middle);
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Ledge";
            platform.transform.SetParent(room.transform, true);
            platform.transform.position = centre + Vector3.up * (Height * 0.5f);
            platform.transform.localScale = new Vector3(Span * size, Height, Span * size);
            if (sides != null) platform.GetComponent<Renderer>().sharedMaterial = sides;
            // Drawn as a stack of three books, each a little askew, the top one's pages showing at the edge.
            platform.GetComponent<Renderer>().enabled = false;
            var rng = new System.Random(middle.x * 31 + middle.y * 17);
            Color[] covers = { new Color(0.55f, 0.12f, 0.12f), new Color(0.13f, 0.35f, 0.22f), new Color(0.15f, 0.22f, 0.5f), new Color(0.45f, 0.3f, 0.12f), new Color(0.35f, 0.15f, 0.4f) };
            float layer = Height / 3f;
            for (int book = 0; book < 3; book++)
            {
                float y = layer * (book + 0.5f);
                float turn = (float)(rng.NextDouble() * 6.0 - 3.0);
                var cover = Part.Make(PrimitiveType.Cube, "Book " + book, platform.transform.parent, Vector3.zero, new Vector3(Span * size + 0.2f, layer * 0.98f, Span * size - 0.1f), covers[rng.Next(covers.Length)]);
                cover.transform.position = centre + Vector3.up * y;
                cover.transform.rotation = Quaternion.Euler(0f, turn, 0f);
                var pages = Part.Make(PrimitiveType.Cube, "Pages " + book, cover.transform, new Vector3(0.012f, 0f, 0f), new Vector3(0.97f, 0.8f, 1.03f), new Color(0.93f, 0.9f, 0.8f));
                pages.transform.localPosition = new Vector3(0f, 0f, 0f);
            }

            // A lighter lid, so the top reads as somewhere to stand.
            var lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.name = "Ledge Top";
            Object.Destroy(lid.GetComponent<Collider>());
            lid.transform.SetParent(platform.transform.parent, true);
            lid.transform.position = centre + Vector3.up * (Height + 0.02f);
            lid.transform.localScale = new Vector3(Span * size + 0.1f, 0.06f, Span * size + 0.1f);
            if (topMaterial != null) lid.GetComponent<Renderer>().sharedMaterial = topMaterial;

            // The ramp: a slab from the floor at its foot to the platform's edge.
            Vector3 dir = new Vector3(step.x, 0f, step.y);
            float run = RampTiles * size;
            Vector3 edge = centre + dir * (Span * size * 0.5f);
            Vector3 low = edge + dir * run;
            float length = Mathf.Sqrt(run * run + Height * Height);
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "Ramp";
            slab.transform.SetParent(platform.transform.parent, true);
            slab.transform.position = (edge + Vector3.up * Height + low) * 0.5f + Vector3.down * 0.1f;
            slab.transform.rotation = Quaternion.LookRotation(edge + Vector3.up * Height - low);
            slab.transform.localScale = new Vector3(size * 1.6f, 0.2f, length);
            if (topMaterial != null) slab.GetComponent<Renderer>().sharedMaterial = topMaterial;

            // Nothing else is put on these tiles now: no cover, no enemies standing inside the platform.
            foreach (var each in tiles) shape.free.Remove(each);
            foreach (var each in ramp) shape.free.Remove(each);

            Tops.Add(new Top { room = room, bounds = new Bounds(centre + Vector3.up * Height, new Vector3(Span * size, 0.2f, Span * size)) });
            return true;
        }

        static bool Clear(RoomShape shape, Vector2Int tile) =>
            tile.x >= 1 && tile.y >= 1 && tile.x < RoomShape.Size - 1 && tile.y < RoomShape.Size - 1 && shape.open[tile.x, tile.y] && shape.free.Contains(tile);

        /// <summary>Spots on the platforms in this room where an enemy can be put.</summary>
        public static List<Vector3> SpotsIn(Room room)
        {
            var spots = new List<Vector3>();
            foreach (var top in Tops)
            {
                if (top.room != room) continue;
                Vector3 c = top.bounds.center;
                spots.Add(c);
                spots.Add(c + new Vector3(1.5f, 0f, 1.5f));
            }
            return spots;
        }

        /// <summary>The height of whatever is underfoot here: a platform's top, or the floor.</summary>
        public static float SurfaceAt(Vector3 position)
        {
            foreach (var top in Tops)
            {
                Bounds b = top.bounds;
                if (position.x >= b.min.x && position.x <= b.max.x && position.z >= b.min.z && position.z <= b.max.z) return b.center.y;
            }
            return 0f;
        }
    }
}
