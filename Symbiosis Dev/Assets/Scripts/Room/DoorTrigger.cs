using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorTrigger : MonoBehaviour
{
    public string presetSceneName = "TryThisScene"; // Set this to your test scene's name

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entered Door Trigger");
        if (other.CompareTag("Player")) // Ensure your player is tagged "Player"
        {
            SceneManager.LoadScene(presetSceneName);
        }
    }
}
