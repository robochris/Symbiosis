using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>Shortcuts for getting into the game quickly. Menu: Shrunk > Open ...</summary>
    public static class ShrunkMenu
    {
        [MenuItem("Shrunk/Open Run Scene (the game)", priority = 0)]
        public static void OpenRun() => Open("Assets/Scenes/Run.unity");

        [MenuItem("Shrunk/Open Arena Scene (one ant, empty room)", priority = 1)]
        public static void OpenArena() => Open("Assets/Scenes/Arena.unity");

        static void Open(string path)
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(path);
            Debug.Log("[Shrunk] Opened " + path + ". Press the Play button at the top to start.");
        }
    }
}
