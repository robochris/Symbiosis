using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// Builds the Run scene. Rooms are not built here: every room is carved at random while the game runs.
    /// Menu: Shrunk > Rebuild Run Scene. Rebuilding replaces the scene, prefabs and data files, so hand edits to them are lost.
    /// </summary>
    public static class RunSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Run.unity";
        const string ArenaPath = "Assets/Scenes/Arena.unity";

        [MenuItem("Shrunk/Rebuild Everything", priority = 20)]
        public static void BuildAll()
        {
            ArenaSceneBuilder.Build();
            Build();
        }

        [MenuItem("Shrunk/Rebuild Run Scene", priority = 21)]
        public static void Build()
        {
            ArenaSceneBuilder.EnsureFolder("Assets", "Scenes");
            var biome = ContentBuilder.Build(out var bulletPrefab);

            var voidBody = ArenaSceneBuilder.Lit("VoidBody", new Color(0.03f, 0.03f, 0.05f), 0.35f);
            var eyeGlow = ArenaSceneBuilder.Unlit("EyeGlow", new Color(0.45f, 0.95f, 1f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ArenaSceneBuilder.BuildLighting();
            var player = ArenaSceneBuilder.BuildPlayer(voidBody, eyeGlow, bulletPrefab);
            ArenaSceneBuilder.BuildCamera(player.transform);
            var touch = ArenaSceneBuilder.BuildHud();
            player.GetComponent<PlayerInputReader>().touch = touch;
            ExtendHud(touch.gameObject);

            var game = new GameObject("Game");
            ContentBuilder.AddGameAssets(game);
            game.AddComponent<GameLoop>();
            game.AddComponent<FloorManager>().biome = biome;

            EditorSceneManager.SaveScene(scene, ScenePath);
            UpdateBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Shrunk] Run scene rebuilt at " + ScenePath);
        }

        /// <summary>The Run scene is what the game starts in; the Arena stays in the build as a test room.</summary>
        internal static void UpdateBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            if (File.Exists(ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            if (File.Exists(ArenaPath)) scenes.Add(new EditorBuildSettingsScene(ArenaPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void ExtendHud(GameObject canvas)
        {
            var hud = canvas.GetComponent<Hud>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Kills moves under the health pips so the minimap can have the top-right corner.
            var kills = (RectTransform)hud.killsText.transform;
            kills.anchorMin = kills.anchorMax = new Vector2(0f, 1f);
            kills.anchoredPosition = new Vector2(255f, -250f);
            hud.killsText.alignment = TextAnchor.MiddleLeft;
            hud.killsText.fontSize = 36;

            hud.floorText = ArenaSceneBuilder.UiText("Floor", canvas.transform, font, "Floor 1", 44, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(255f, -195f), new Vector2(400f, 60f));

            var frame = new GameObject("Minimap", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)frame.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-40f, -40f);
            rect.sizeDelta = new Vector2(300f, 300f);
            var background = frame.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.35f);
            background.raycastTarget = false;

            var container = new GameObject("Tiles", typeof(RectTransform));
            var tiles = (RectTransform)container.transform;
            tiles.SetParent(rect, false);
            tiles.anchorMin = tiles.anchorMax = tiles.pivot = new Vector2(0.5f, 0.5f);
            tiles.sizeDelta = new Vector2(270f, 270f);

            frame.AddComponent<Minimap>().container = tiles;
        }
    }
}
