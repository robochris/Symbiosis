using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// One room of a floor, built at runtime from a RoomShape: a floor-and-walls mesh, a locking barrier in each
    /// doorway, and the special prop for its type (exit hole, treasure pedestal, shop stall).
    /// </summary>
    public class Room : MonoBehaviour
    {
        public RoomShape Shape { get; private set; }
        public Doors Doors => Shape.doors;
        public bool Locked { get; private set; }
        /// <summary>Exit rooms only: the way down, hidden until the room is cleared.</summary>
        public GameObject ExitHole { get; private set; }
        /// <summary>World position of the middle of the main chamber. Always clear floor.</summary>
        public Vector3 Centre => TileWorld(Shape.centre);

        readonly GameObject[] _doors = new GameObject[4];
        Mesh _visual, _collision;

        public static Room Create(RoomShape shape, BiomeDefinition biome, Transform parent, Vector3 position)
        {
            var go = new GameObject("Room");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var room = go.AddComponent<Room>();
            room.Shape = shape;

            RoomMesh.Build(shape, biome.wallHeight, out room._visual, out room._collision);
            go.AddComponent<MeshFilter>().sharedMesh = room._visual;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { biome.floorMaterial, biome.wallMaterial };
            go.AddComponent<MeshCollider>().sharedMesh = room._collision;

            // A barrier in each doorway, switched on while the room is locked.
            float edge = (RoomShape.Size - 1) * 0.5f * RoomShape.TileSize;
            for (int side = 0; side < 4; side++)
            {
                if ((shape.doors & (Doors)(1 << side)) == 0) continue;
                Vector2Int d = FloorLayout.Directions[side];
                bool alongX = d.x == 0;

                var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
                door.name = (Doors)(1 << side) + " Door";
                door.transform.SetParent(go.transform, false);
                door.transform.localPosition = new Vector3(d.x * edge, biome.wallHeight * 0.5f, d.y * edge);
                door.transform.localScale = new Vector3(alongX ? RoomShape.TileSize * 2f : 0.8f, biome.wallHeight, alongX ? 0.8f : RoomShape.TileSize * 2f);
                door.GetComponent<Renderer>().sharedMaterial = biome.doorMaterial;
                door.SetActive(false);
                room._doors[side] = door;
            }

            GameObject prop = biome.PropFor(shape.type);
            if (prop != null)
            {
                var instance = Instantiate(prop, room.Centre, Quaternion.identity, go.transform);
                if (shape.type == RoomType.Exit)
                {
                    room.ExitHole = instance;
                    instance.SetActive(false);
                }
            }

            return room;
        }

        void OnDestroy()
        {
            if (_visual != null) Destroy(_visual);
            if (_collision != null) Destroy(_collision);
        }

        public Vector3 TileWorld(Vector2Int tile) => transform.position + RoomShape.TileLocal(tile);

        /// <summary>The barrier on a side, or null where there is no doorway.</summary>
        public GameObject DoorObject(int side) => _doors[side];

        public void SetLocked(bool locked)
        {
            if (locked != Locked) Sfx.Play(locked ? SfxId.DoorLock : SfxId.DoorOpen);
            Locked = locked;
            foreach (var door in _doors) if (door != null) door.SetActive(locked);
        }

        /// <summary>True when the point is past the doorways and at least <paramref name="margin"/> into the room.</summary>
        public bool ContainsInside(Vector3 worldPoint, float margin)
        {
            Vector3 local = worldPoint - transform.position;
            float limit = (RoomShape.Size * 0.5f - 1f) * RoomShape.TileSize - margin;
            return Mathf.Abs(local.x) <= limit && Mathf.Abs(local.z) <= limit;
        }
    }

    /// <summary>Turns a flat RoomShape into meshes: floor under open tiles, walls where open meets solid, a cap on solid tiles.</summary>
    public static class RoomMesh
    {
        public static void Build(RoomShape shape, float wallHeight, out Mesh visual, out Mesh collision)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var floor = new List<int>();
            var walls = new List<int>();
            var caps = new List<int>();

            float half = RoomShape.Size * 0.5f * RoomShape.TileSize;
            float t = RoomShape.TileSize;

            for (int x = 0; x < RoomShape.Size; x++)
            {
                for (int y = 0; y < RoomShape.Size; y++)
                {
                    float x0 = x * t - half, x1 = x0 + t, z0 = y * t - half, z1 = z0 + t;

                    if (!shape.open[x, y])
                    {
                        // Top of solid earth, so the room reads as carved out when seen from above.
                        Quad(vertices, normals, caps, Vector3.up,
                            new Vector3(x0, wallHeight, z0), new Vector3(x0, wallHeight, z1), new Vector3(x1, wallHeight, z1), new Vector3(x1, wallHeight, z0));
                        continue;
                    }

                    Quad(vertices, normals, floor, Vector3.up,
                        new Vector3(x0, 0f, z0), new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1), new Vector3(x1, 0f, z0));

                    // A wall on each side that borders solid earth. Outside the grid is the next room's doorway: no wall.
                    if (Solid(shape, x, y + 1))
                        Quad(vertices, normals, walls, Vector3.back,
                            new Vector3(x0, 0f, z1), new Vector3(x0, wallHeight, z1), new Vector3(x1, wallHeight, z1), new Vector3(x1, 0f, z1));
                    if (Solid(shape, x, y - 1))
                        Quad(vertices, normals, walls, Vector3.forward,
                            new Vector3(x1, 0f, z0), new Vector3(x1, wallHeight, z0), new Vector3(x0, wallHeight, z0), new Vector3(x0, 0f, z0));
                    if (Solid(shape, x + 1, y))
                        Quad(vertices, normals, walls, Vector3.left,
                            new Vector3(x1, 0f, z1), new Vector3(x1, wallHeight, z1), new Vector3(x1, wallHeight, z0), new Vector3(x1, 0f, z0));
                    if (Solid(shape, x - 1, y))
                        Quad(vertices, normals, walls, Vector3.right,
                            new Vector3(x0, 0f, z0), new Vector3(x0, wallHeight, z0), new Vector3(x0, wallHeight, z1), new Vector3(x0, 0f, z1));
                }
            }

            visual = new Mesh { name = "Room Visual" };
            visual.SetVertices(vertices);
            visual.SetNormals(normals);
            visual.subMeshCount = 2;
            visual.SetTriangles(floor, 0);
            var wallsAndCaps = new List<int>(walls);
            wallsAndCaps.AddRange(caps);
            visual.SetTriangles(wallsAndCaps, 1);
            visual.RecalculateBounds();

            // Collision leaves the caps out: nothing walks up there, and it keeps them off the navmesh.
            collision = new Mesh { name = "Room Collision" };
            collision.SetVertices(vertices);
            var solid = new List<int>(floor);
            solid.AddRange(walls);
            collision.SetTriangles(solid, 0);
            collision.RecalculateBounds();
        }

        static bool Solid(RoomShape shape, int x, int y)
        {
            if (x < 0 || y < 0 || x >= RoomShape.Size || y >= RoomShape.Size) return false;
            return !shape.open[x, y];
        }

        static void Quad(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, Vector3 normal, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            for (int n = 0; n < 4; n++) normals.Add(normal);
            triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
            triangles.Add(i); triangles.Add(i + 2); triangles.Add(i + 3);
        }
    }
}
