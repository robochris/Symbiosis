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
    [Tooltip("A single door prefab to use for both forward and backward doors.")]
    public GameObject doorPrefab;
    [Tooltip("Obstacle prefabs for random props in the room.")]
    public GameObject[] obstaclePrefabs;
    [Tooltip("Number of obstacles to spawn randomly.")]
    public int obstacleCount = 5;
    public bool firstRoom = true;
    private GameObject roomParent;

    /// <summary>
    /// Generates a room using the given random seed (for deterministic layouts).
    /// </summary>
    public void GenerateRoomWithSeed(int seed)
    {
        // This line ensures all Random.Range calls are deterministic based on 'seed'
        Random.InitState(seed);

        // Then call our regular GenerateRoom() which actually does the building
        GenerateRoom();
    }

    /// <summary>
    /// Creates a new random room with walls, floor, plus a forward door (front) and backward door (back).
    /// Uses the same door prefab for both doors but configures them differently.
    /// </summary>
    public void GenerateRoom()
    {
        // 1) Random room size
        int width = Random.Range(minSize, maxSize);
        int depth = Random.Range(minSize, maxSize);

        // Create a parent object for all room elements
        roomParent = new GameObject("ProceduralRoom");
        roomParent.transform.position = Vector3.zero;

        // 2) Floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(roomParent.transform, false);
        // A Plane is 10x10 in Unity, so multiply 0.1f to match 'width'/'depth'
        floor.transform.localScale = new Vector3(width * 0.1f, 1f, depth * 0.1f);
        floor.layer = LayerMask.NameToLayer("Default");

        float halfW = width * 0.5f;
        float halfD = depth * 0.5f;

        // 3) Left and Right walls (full)
        CreateWall(new Vector3(-halfW, wallHeight / 2f, 0),
                   new Vector3(wallThickness, wallHeight, depth),
                   "LeftWall");
        CreateWall(new Vector3(halfW, wallHeight / 2f, 0),
                   new Vector3(wallThickness, wallHeight, depth),
                   "RightWall");

        // 4) Front wall with gap
        float gapHalf = doorGapWidth * 0.5f;
        float frontZ = halfD;

        // Front wall, left piece
        float leftFrontWidth = halfW - gapHalf;
        float leftFrontX = -halfW + (leftFrontWidth * 0.5f);
        CreateWall(new Vector3(leftFrontX, wallHeight / 2f, frontZ),
                   new Vector3(leftFrontWidth, wallHeight, wallThickness),
                   "FrontWallLeft");

        // Front wall, right piece
        float rightFrontWidth = halfW - gapHalf;
        float rightFrontX = halfW - (rightFrontWidth * 0.5f);
        CreateWall(new Vector3(rightFrontX, wallHeight / 2f, frontZ),
                   new Vector3(rightFrontWidth, wallHeight, wallThickness),
                   "FrontWallRight");

        // 5) Place the front door at the gap, rotated to face the room center
        //    So if the room center is (0,0,0), the door looks inward
        if (doorPrefab != null && !firstRoom)
        {
            Vector3 frontDoorPos = new Vector3(0, 0, frontZ);

            // "Look at center" approach
            Vector3 center = Vector3.zero; // assume room center is (0,0,0)
            Vector3 dirToCenter = (center - frontDoorPos).normalized;
            Quaternion frontDoorRot = Quaternion.LookRotation(dirToCenter, Vector3.up);
            frontDoorRot *= Quaternion.Euler(0f, 90f, 0f);
            GameObject frontDoor = Instantiate(doorPrefab, frontDoorPos, frontDoorRot, roomParent.transform);
            frontDoor.name = "ForwardDoor";
            // (Optional) If using a DoorTrigger script:
            // var trigger = frontDoor.GetComponent<DoorTrigger>();
            // if (trigger) trigger.isForwardDoor = true;
        }

        // 6) Back wall with gap
        float backZ = -halfD;

        // Back wall, left piece
        float leftBackWidth = halfW - gapHalf;
        float leftBackX = -halfW + (leftBackWidth * 0.5f);
        CreateWall(new Vector3(leftBackX, wallHeight / 2f, backZ),
                   new Vector3(leftBackWidth, wallHeight, wallThickness),
                   "BackWallLeft");

        // Back wall, right piece
        float rightBackWidth = halfW - gapHalf;
        float rightBackX = halfW - (rightBackWidth * 0.5f);
        CreateWall(new Vector3(rightBackX, wallHeight / 2f, backZ),
                   new Vector3(rightBackWidth, wallHeight, wallThickness),
                   "BackWallRight");

        // 7) Place the back door, also facing room center
        if (doorPrefab != null)
        {
            Vector3 backDoorPos = new Vector3(0, 0, backZ);

            // "Look at center" for the back door
            Vector3 center = Vector3.zero;
            Vector3 dirToCenter = (center - backDoorPos).normalized;
            Quaternion backDoorRot = Quaternion.LookRotation(dirToCenter, Vector3.up);
            backDoorRot *= Quaternion.Euler(0f, -90f, 0f);
            GameObject backDoor = Instantiate(doorPrefab, backDoorPos, backDoorRot, roomParent.transform);
            backDoor.name = "BackwardDoor";
            firstRoom = false;
            // (Optional) If using a DoorTrigger script:
            // var trigger = backDoor.GetComponent<DoorTrigger>();
            // if (trigger) trigger.isForwardDoor = false;
        }

        // 8) Spawn random obstacles
        for (int i = 0; i < obstacleCount; i++)
        {
            SpawnObstacle(width, depth);
        }
    }


    /// <summary>
    /// Removes the old room from the scene.
    /// </summary>
    public void DestroyCurrentRoom()
    {
        if (roomParent != null)
        {
            Destroy(roomParent);
            roomParent = null;
        }
    }

    /// <summary>
    /// Creates a wall piece using a primitive Cube, placed on "Walls" layer for camera fade, etc.
    /// </summary>
    private void CreateWall(Vector3 localPos, Vector3 localScale, string wallName)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.SetParent(roomParent.transform, false);
        wall.name = wallName;
        wall.transform.localPosition = localPos;
        wall.transform.localScale = localScale;

        // If you use a WallTransparency script, set the layer to "Walls"
        wall.layer = LayerMask.NameToLayer("Walls");
    }

    /// <summary>
    /// Spawns a random obstacle within the room bounds.
    /// </summary>
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
        // Typically on Default or another layer, not "Walls"
    }
}
