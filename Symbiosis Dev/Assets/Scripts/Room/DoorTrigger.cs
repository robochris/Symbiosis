using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [Tooltip("True = forward, False = back")]
    public bool isForwardDoor = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (isForwardDoor)
            {
                RoomChainManager.Instance.GoForward();
            }
            else
            {
                RoomChainManager.Instance.GoBack();
            }
        }
    }
}
