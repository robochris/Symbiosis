using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [Tooltip("If true, calls RoomChainManager.GoForward(). If false, calls GoBack().")]
    public bool isForwardDoor;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RoomChainManager.Instance.lastDoorUsedIsForward = isForwardDoor;

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
