using UnityEngine;

public class InteractiveZone : MonoBehaviour
{
    public TileType zoneType;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            switch (zoneType)
            {
                case TileType.Water:
                    Debug.Log("Player entered Water zone – start swimming");
                    // Add your swimming logic here.
                    break;
                case TileType.Lava:
                    Debug.Log("Player entered Lava zone – start burning");
                    // Add your burn/damage logic here.
                    break;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            switch (zoneType)
            {
                case TileType.Water:
                    Debug.Log("Player exited Water zone – stop swimming");
                    // End swimming effects.
                    break;
                case TileType.Lava:
                    Debug.Log("Player exited Lava zone – stop burning");
                    // End burn effects.
                    break;
            }
        }
    }
}
