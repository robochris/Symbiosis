using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RespawnItem(GameObject item, float delay)
    {
        StartCoroutine(RespawnCoroutine(item, delay));
    }

    private IEnumerator RespawnCoroutine(GameObject item, float delay)
    {
        yield return new WaitForSeconds(delay);
        item.SetActive(true);
    }
}
