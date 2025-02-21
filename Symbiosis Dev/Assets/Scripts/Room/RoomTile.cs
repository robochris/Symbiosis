using UnityEngine;

public enum RoomTileType
{
    None,
    Floor,
    Wall,
    Door,
    Window
}

[CreateAssetMenu(fileName = "RoomPiece", menuName = "RoomGeneration/RoomPiece")]
public class RoomPiece : ScriptableObject
{
    public RoomTileType tileType;
    public GameObject prefab;

    [Range(0.01f, 10f)]
    public float weight = 1f;

    public string biome;

    // Change to int for strict grid alignment
    public Vector2Int Size = new Vector2Int(1, 1);

    // Optional: Store actual prefab size
    public Vector3 prefabSize = Vector3.one;
}
