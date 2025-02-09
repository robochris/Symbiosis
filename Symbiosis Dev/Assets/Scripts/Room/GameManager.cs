using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public string firstRoomSceneName = "Chris's Test eniviroment"; // Name of your testing room scene

    private void Awake()
    {
        // Singleton pattern: only one instance should persist.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep GameManager across scene loads
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Load the first room (testing room) additively.
        SceneManager.LoadScene(firstRoomSceneName, LoadSceneMode.Additive);
    }
}
