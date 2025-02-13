using UnityEngine;

public class RoomGenerator : MonoBehaviour
{
    [Header("Room Size")]
    public int minSize = 10;
    public int maxSize = 30;

    [Header("Walls")]
    public float wallHeight = 3f;
    public float wallThickness = 0.5f;
    [Tooltip("Width of the door gap in the front/back walls.")]
    public float doorGapWidth = 2f;

    [Header("Prefabs")]
    [Tooltip("A single door prefab that must include a child named 'PlayerSpawnMarker'.")]
    public GameObject doorPrefab;
    [Tooltip("Obstacle prefabs for random props in the room.")]
    public GameObject[] obstaclePrefabs;
    [Tooltip("Number of obstacles to spawn randomly.")]
    public int obstacleCount = 5;

    // Set this flag externally when generating the first room
    public bool isFirstRoom = false;

    // Parent that holds the entire generated room
    private GameObject roomParent;

    // These properties store the exact door spawn positions (taken from the door marker)
    public Vector3 ForwardDoorPosition { get; private set; }
    public Vector3 BackwardDoorPosition { get; private set; }
    public float CurrentDepth { get; private set; } // Useful for debugging/positioning

    /// <summary>
    /// Sets the random seed and generates the room.
    /// </summary>
    public void GenerateRoomWithSeed(int seed)
    {
        Random.InitState(seed);
        GenerateRoom();
    }

    /// <summary>
    /// Generates a new room with a random size, floor, walls, doors, and obstacles.
    /// </summary>
    public void GenerateRoom()
    {
        // 1) Determine room dimensions
        int width = Random.Range(minSize, maxSize);
        int depth = Random.Range(minSize, maxSize);
        CurrentDepth = depth;
        float halfW = width * 0.5f;
        float halfD = depth * 0.5f;

        // 2) Create a parent object for the room
        roomParent = new GameObject("ProceduralRoom");
        roomParent.transform.position = Vector3.zero;

        // 3) Create the floor (Unity Plane is 10x10 by default)
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(roomParent.transform, false);
        floor.transform.localScale = new Vector3(width * 0.1f, 1f, depth * 0.1f);
        floor.layer = LayerMask.NameToLayer("Default");

        // 4) Create full left and right walls
        CreateWall(new Vector3(-halfW, wallHeight / 2f, 0), new Vector3(wallThickness, wallHeight, depth), "LeftWall");
        CreateWall(new Vector3(halfW, wallHeight / 2f, 0), new Vector3(wallThickness, wallHeight, depth), "RightWall");

        // 5) Create front wall (with or without gap based on isFirstRoom)
        float frontZ = halfD;
        if (!isFirstRoom)
        {
            float gapHalf = doorGapWidth * 0.5f;
            float leftFrontWidth = halfW - gapHalf;
            float leftFrontX = -halfW + (leftFrontWidth * 0.5f);
            CreateWall(new Vector3(leftFrontX, wallHeight / 2f, frontZ), new Vector3(leftFrontWidth, wallHeight, wallThickness), "FrontWallLeft");

            float rightFrontWidth = halfW - gapHalf;
            float rightFrontX = halfW - (rightFrontWidth * 0.5f);
            CreateWall(new Vector3(rightFrontX, wallHeight / 2f, frontZ), new Vector3(rightFrontWidth, wallHeight, wallThickness), "FrontWallRight");

            // 6) Instantiate the forward door on the front gap with a 90° rotation offset
            if (doorPrefab != null)
            {
                Vector3 forwardDoorPos = new Vector3(0, 0, frontZ);
                Vector3 center = Vector3.zero;
                Quaternion baseRotation = Quaternion.LookRotation((center - forwardDoorPos).normalized, Vector3.up);
                Quaternion doorOffset = Quaternion.Euler(0, 90, 0);
                Quaternion finalRotation = baseRotation * doorOffset;
                GameObject fDoor = Instantiate(doorPrefab, forwardDoorPos, finalRotation, roomParent.transform);
                fDoor.name = "ForwardDoor";

                // Instead of using the prefab's marker, compute the spawn position inside the room.
                float spawnOffset = 1.0f; // Adjust as needed
                float floorHeight = 1.0f; // Set this to your floor's Y position
                Vector3 insideDirection = (center - forwardDoorPos).normalized;
                ForwardDoorPosition = new Vector3(
                    forwardDoorPos.x + insideDirection.x * spawnOffset,
                    floorHeight,
                    forwardDoorPos.z + insideDirection.z * spawnOffset
                );
            }
        }
        else
        {
            // For the first room, build a complete front wall with no gap.
            CreateWall(new Vector3(0, wallHeight / 2f, frontZ), new Vector3(width, wallHeight, wallThickness), "FrontWall");
        }

        // 7) Create back wall with a gap for the backward door
        float backZ = -halfD;
        float gapHalfBack = doorGapWidth * 0.5f;
        float leftBackWidth = halfW - gapHalfBack;
        float leftBackX = -halfW + (leftBackWidth * 0.5f);
        CreateWall(new Vector3(leftBackX, wallHeight / 2f, backZ), new Vector3(leftBackWidth, wallHeight, wallThickness), "BackWallLeft");
        float rightBackWidth = halfW - gapHalfBack;
        float rightBackX = halfW - (rightBackWidth * 0.5f);
        CreateWall(new Vector3(rightBackX, wallHeight / 2f, backZ), new Vector3(rightBackWidth, wallHeight, wallThickness), "BackWallRight");

        // 8) Instantiate the backward door on the back gap with a 90° rotation offset
        if (doorPrefab != null)
        {
            Vector3 backDoorPos = new Vector3(0, 0, backZ);
            Vector3 center = Vector3.zero;
            Quaternion baseRotation = Quaternion.LookRotation((center - backDoorPos).normalized, Vector3.up);
            Quaternion doorOffset = Quaternion.Euler(0, 90, 0);
            Quaternion finalRotation = baseRotation * doorOffset;
            GameObject bDoor = Instantiate(doorPrefab, backDoorPos, finalRotation, roomParent.transform);
            bDoor.name = "BackwardDoor";

            float spawnOffset = 1.0f; // Adjust as needed
            float floorHeight = 1.0f; // Set this to your floor's Y position
            Vector3 insideDirection = (center - backDoorPos).normalized;
            BackwardDoorPosition = new Vector3(
                    backDoorPos.x + insideDirection.x * spawnOffset,
                    floorHeight,
                    backDoorPos.z + insideDirection.z * spawnOffset
            );
        }

        // 9) Spawn random obstacles
        for (int i = 0; i < obstacleCount; i++)
        {
            SpawnObstacle(width, depth);
        }
    }

    public void DestroyCurrentRoom()
    {
        if (roomParent != null)
        {
            Destroy(roomParent);
            roomParent = null;
        }
        // Notify WallTransparency to clear old references
        if (WallTransparency.Instance != null)
            WallTransparency.Instance.ClearOldRenderers();
    }

    private void CreateWall(Vector3 localPos, Vector3 localScale, string wallName)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.SetParent(roomParent.transform, false);
        wall.name = wallName;
        wall.transform.localPosition = localPos;
        wall.transform.localScale = localScale;
        // Set wall to the "Walls" layer (make sure you have created this layer in Unity)
        wall.layer = LayerMask.NameToLayer("Boundry");
    }

    private void SpawnObstacle(int width, int depth)
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;
        float halfW = width * 0.5f;
        float halfD = depth * 0.5f;
        float x = Random.Range(-halfW + 1f, halfW - 1f);
        float z = Random.Range(-halfD + 1f, halfD - 1f);
        Vector3 pos = new Vector3(x, 0, z);
        GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        GameObject obstacle = Instantiate(prefab, pos, Quaternion.identity, roomParent.transform);
        obstacle.name = "Obstacle_" + prefab.name;
    }
}
