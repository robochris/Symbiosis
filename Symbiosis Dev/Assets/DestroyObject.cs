using UnityEngine;

public class scrip : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
        Destroy(gameObject); // Destroys THIS bullet
    }

}
