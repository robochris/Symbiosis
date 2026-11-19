using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>Shortcuts for getting into the game quickly. Menu: Shrunk > Open ...</summary>
    public static class ShrunkMenu
    {
        [MenuItem("Shrunk/Open Lab Scene (start here)", priority = 0)]
        public static void OpenLab() => Open("Assets/Scenes/Lab.unity");

        [MenuItem("Shrunk/Open Run Scene (straight into a run)", priority = 1)]
        public static void OpenRun() => Open("Assets/Scenes/Run.unity");

        [MenuItem("Shrunk/Delete Saved Progress", priority = 60)]
        public static void DeleteSave()
        {
            if (!EditorUtility.DisplayDialog("Delete saved progress?", "This removes banked samples, lab upgrades and any run in progress.", "Delete", "Cancel")) return;
            SaveSystem.Profile = "player";
            SaveSystem.Wipe();
            Debug.Log("[Shrunk] Saved progress deleted.");
        }

        [MenuItem("Shrunk/Playtest: Unlock Everything", priority = 61)]
        public static void UnlockEverything()
        {
            if (!EditorUtility.DisplayDialog("Unlock everything?", "Every biome, style, item, hero power, notebook page and the hidden door, written into your save. Delete Saved Progress undoes it.", "Unlock", "Cancel")) return;
            if (!EditorApplication.isPlaying) SaveSystem.Profile = "player";
            Playtest.UnlockEverything();
            Debug.Log("[Shrunk] Everything unlocked. If the game is running, walk back into the lab to see it.");
        }

        [MenuItem("Shrunk/Playtest: Add 500 Samples", priority = 62)]
        public static void AddSamples()
        {
            if (!EditorApplication.isPlaying) SaveSystem.Profile = "player";
            Playtest.AddSamples(500);
            Debug.Log("[Shrunk] 500 samples added.");
        }

        [MenuItem("Shrunk/Open Arena Scene (one ant, empty room)", priority = 2)]
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
