using UnityEngine;
using System.Collections.Generic;

public enum TileType
{
    None,
    Ground,
    Water,
    Lava
}

public class ProceduralRoomGeneratorNoise : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 70;
    public int gridHeight = 70;
    public float tileSize = 1f;

    [Header("Map Shape")]
    [Tooltip("Scale for the map shape noise (lower values produce larger smooth regions)")]
    public float mapNoiseScale = 0.2f;
    [Tooltip("Threshold for including a tile in the map (lower values result in a smaller, more irregular shape)")]
    [Range(0f, 1f)]
    public float mapThreshold = 0.5f;

    [Header("Noise & Tile Assignment")]
    [Tooltip("Scale of noise sampling for water and lava regions")]
    public float noiseScale = 0.1f;
    [Tooltip("Tile becomes water if waterNoise exceeds this threshold and is higher than lavaNoise")]
    [Range(0f, 1f)]
    public float waterThreshold = 0.6f;
    [Tooltip("Tile becomes lava if lavaNoise exceeds this threshold and is higher than waterNoise")]
    [Range(0f, 1f)]
    public float lavaThreshold = 0.6f;

    [Header("Separation Condition")]
    [Tooltip("Any lava tile within this distance of a water tile will be converted to ground (water wins)")]
    public int minDistanceBetweenWaterAndLava = 10;

    [Header("Materials")]
    public Material groundMaterial;  // e.g., gold
    public Material waterMaterial;   // e.g., blue flat-color
    public Material lavaMaterial;    // e.g., red flat-color

    [Header("Mesh Irregularity")]
    [Tooltip("Amount of vertex jitter for water/lava meshes to break up the grid look")]
    public float jitterScale = 0.5f;

    private TileType[,] grid;

    void Start()
    {
        GenerateGridData();
        EnforceSeparation();
        BuildMeshes();
    }

    // Generate grid data using two noise maps—but only for cells inside the map mask.
    void GenerateGridData()
    {
        grid = new TileType[gridWidth, gridHeight];

        // Calculate the center and maximum distance (for radial falloff).
        Vector2 center = new Vector2(gridWidth / 2f, gridHeight / 2f);
        float maxDist = center.magnitude;

        // Offsets for the two noise maps.
        float waterOffsetX = 100f, waterOffsetY = 100f;
        float lavaOffsetX = 200f, lavaOffsetY = 200f;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Compute normalized distance from center.
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                // Get shape noise for an irregular boundary.
                float shapeNoise = Mathf.PerlinNoise(x * mapNoiseScale, y * mapNoiseScale);
                // Determine if the cell is inside the map.
                bool isInside = (dist + shapeNoise * 0.5f < mapThreshold);

                if (!isInside)
                {
                    grid[x, y] = TileType.None;
                    continue;
                }

                // Sample water and lava noise.
                float waterNoise = Mathf.PerlinNoise((x + waterOffsetX) * noiseScale, (y + waterOffsetY) * noiseScale);
                float lavaNoise = Mathf.PerlinNoise((x + lavaOffsetX) * noiseScale, (y + lavaOffsetY) * noiseScale);

                // Decide tile type based on noise values.
                if (waterNoise > waterThreshold && waterNoise > lavaNoise)
                {
                    grid[x, y] = TileType.Water;
                }
                else if (lavaNoise > lavaThreshold && lavaNoise > waterNoise)
                {
                    grid[x, y] = TileType.Lava;
                }
                else
                {
                    grid[x, y] = TileType.Ground;
                }
            }
        }
    }

    // For each lava cell, if there’s a water cell within minDistance, convert it to ground.
    void EnforceSeparation()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == TileType.Lava)
                {
                    if (!IsFarFromType(x, y, TileType.Water, minDistanceBetweenWaterAndLava))
                    {
                        grid[x, y] = TileType.Ground;
                    }
                }
            }
        }
    }

    // Check if cell (x,y) is at least minDistance away from any cell of type other.
    bool IsFarFromType(int x, int y, TileType other, int minDistance)
    {
        for (int dx = -minDistance; dx <= minDistance; dx++)
        {
            for (int dy = -minDistance; dy <= minDistance; dy++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx >= 0 && ny >= 0 && nx < gridWidth && ny < gridHeight)
                {
                    if (grid[nx, ny] == other)
                    {
                        if (Mathf.Sqrt(dx * dx + dy * dy) < minDistance)
                            return false;
                    }
                }
            }
        }
        return true;
    }

    // Build all meshes.
    void BuildMeshes()
    {
        BuildGroundMesh();
        BuildIrregularMeshForType(TileType.Water, waterMaterial, "WaterZones");
        BuildIrregularMeshForType(TileType.Lava, lavaMaterial, "LavaZones");
    }

    // Build a combined mesh for all Ground cells (boxy appearance).
    void BuildGroundMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] != TileType.Ground)
                    continue;
                Vector3 bottomLeft = new Vector3(x * tileSize, 0, y * tileSize);
                int index = vertices.Count;
                vertices.Add(bottomLeft);
                vertices.Add(bottomLeft + new Vector3(tileSize, 0, 0));
                vertices.Add(bottomLeft + new Vector3(tileSize, 0, tileSize));
                vertices.Add(bottomLeft + new Vector3(0, 0, tileSize));

                triangles.Add(index);
                triangles.Add(index + 1);
                triangles.Add(index + 2);
                triangles.Add(index);
                triangles.Add(index + 2);
                triangles.Add(index + 3);

                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 1));
            }
        }
        if (vertices.Count == 0)
            return;

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();

        GameObject groundGO = new GameObject("GroundZone");
        groundGO.transform.parent = transform;
        MeshFilter mf = groundGO.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        MeshRenderer mr = groundGO.AddComponent<MeshRenderer>();
        mr.material = groundMaterial;
        MeshCollider mc = groundGO.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
    }

    // Build a combined mesh for all cells of a given type (Water or Lava) with jittered vertices.
    void BuildIrregularMeshForType(TileType type, Material material, string objectName)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] != type)
                    continue;
                Vector3 bottomLeft = new Vector3(x * tileSize, 0, y * tileSize);

                // Jitter each corner to reduce boxiness.
                float offsetBLx = (Mathf.PerlinNoise(x * 0.3f, y * 0.3f) - 0.5f) * jitterScale;
                float offsetBLz = (Mathf.PerlinNoise((x + 100) * 0.3f, (y + 100) * 0.3f) - 0.5f) * jitterScale;
                Vector3 vBL = bottomLeft + new Vector3(offsetBLx, 0, offsetBLz);

                float offsetBRx = (Mathf.PerlinNoise((x + 1) * 0.3f, y * 0.3f) - 0.5f) * jitterScale;
                float offsetBRz = (Mathf.PerlinNoise((x + 1 + 100) * 0.3f, (y + 100) * 0.3f) - 0.5f) * jitterScale;
                Vector3 vBR = bottomLeft + new Vector3(tileSize, 0, 0) + new Vector3(offsetBRx, 0, offsetBRz);

                float offsetTRx = (Mathf.PerlinNoise((x + 1) * 0.3f, (y + 1) * 0.3f) - 0.5f) * jitterScale;
                float offsetTRz = (Mathf.PerlinNoise((x + 1 + 100) * 0.3f, (y + 1 + 100) * 0.3f) - 0.5f) * jitterScale;
                Vector3 vTR = bottomLeft + new Vector3(tileSize, 0, tileSize) + new Vector3(offsetTRx, 0, offsetTRz);

                float offsetTLx = (Mathf.PerlinNoise(x * 0.3f, (y + 1) * 0.3f) - 0.5f) * jitterScale;
                float offsetTLz = (Mathf.PerlinNoise((x + 100) * 0.3f, (y + 1 + 100) * 0.3f) - 0.5f) * jitterScale;
                Vector3 vTL = bottomLeft + new Vector3(0, 0, tileSize) + new Vector3(offsetTLx, 0, offsetTLz);

                int index = vertices.Count;
                vertices.Add(vBL);
                vertices.Add(vBR);
                vertices.Add(vTR);
                vertices.Add(vTL);

                triangles.Add(index);
                triangles.Add(index + 1);
                triangles.Add(index + 2);
                triangles.Add(index);
                triangles.Add(index + 2);
                triangles.Add(index + 3);

                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 1));
            }
        }
        if (vertices.Count == 0)
            return;

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();

        GameObject zoneGO = new GameObject(objectName);
        zoneGO.transform.parent = transform;
        MeshFilter mf = zoneGO.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        MeshRenderer mr = zoneGO.AddComponent<MeshRenderer>();
        mr.material = material;

        // Add a BoxCollider as a trigger.
        BoxCollider bc = zoneGO.AddComponent<BoxCollider>();
        bc.center = mesh.bounds.center;
        bc.size = mesh.bounds.size;
        bc.isTrigger = true;

        InteractiveZone iz = zoneGO.AddComponent<InteractiveZone>();
        iz.zoneType = type;
    }
}
