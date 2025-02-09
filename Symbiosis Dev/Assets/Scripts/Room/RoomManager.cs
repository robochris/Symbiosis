using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class RoomManager : MonoBehaviour
{
    public string currentRoom;

    // Call this method to transition to a new room
    public void LoadRoom(string roomName)
    {
        StartCoroutine(TransitionRoom(roomName));
    }

    private IEnumerator TransitionRoom(string roomName)
    {
        // Load the new room additively
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(roomName, LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Optionally set the new scene as active so that instantiated objects go there by default
        Scene newScene = SceneManager.GetSceneByName(roomName);
        SceneManager.SetActiveScene(newScene);

        // Unload the current room if one exists
        if (!string.IsNullOrEmpty(currentRoom))
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(currentRoom);
            while (!asyncUnload.isDone)
            {
                yield return null;
            }
        }

        currentRoom = roomName;
    }
}
