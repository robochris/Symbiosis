using UnityEngine;
using UnityEngine.UIElements;

public class EnemyDeathDrop : MonoBehaviour

{
    public GameObject itemPrefab;
    public int dropChance = 100;
    void OnDestroy()
    {
        DropItem();
    }


    void DropItem()
    {
        if (Random.Range(0, 100) < dropChance)
        {
            Instantiate(itemPrefab, transform.position, Quaternion.identity);
            }
        }
    }
