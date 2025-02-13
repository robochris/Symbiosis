using UnityEngine;

public enum RoomTileType
{
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
    public float weight = 1f;
    public string biome;

    public float tileWidth = 1;
    public float tileHeight = 1;
}
