using UnityEngine;

public class PersistAllRoots : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
