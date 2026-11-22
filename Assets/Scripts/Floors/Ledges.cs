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
            foreach (var corner in corners)
            {
                if (made >= wanted) break;
                if (TryBuild(room, shape, corner, rng.Next(2) == 0, sides, top)) made++;
            }
        }

        static bool TryBuild(Room room, RoomShape shape, Vector2Int corner, bool rampAlongX, Material sides, Material topMaterial)
        {
            Vector2Int middle = shape.centre + corner * 4;
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
