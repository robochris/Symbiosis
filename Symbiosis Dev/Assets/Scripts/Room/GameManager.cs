using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private RoomGenerator roomGen;
    // private WaveManager waveMan;
    public static GameManager Instance;
    //public string firstRoomSceneName = "Chris's Test eniviroment"; // Name of your testing room scene

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
            // Suppose these two scripts are on the same object as GameManager
            roomGen = GetComponent<RoomGenerator>();
            //waveMan = GetComponent<WaveManager>();

            // 1) Generate the room
            roomGen.GenerateRoom();

            // 2) Start waves
            //waveMan.StartWaves();
    }
}
