using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// Builds the Run scene, the Ant Colony biome data file and the placeholder props for special rooms.
    /// Rooms themselves are not built here: every room is carved at random while the game runs.
    /// Menu: Shrunk > Rebuild Run Scene. Rebuilding replaces the scene and props, so hand edits to them are lost.
    /// </summary>
    public static class RunSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Run.unity";
        const string ArenaPath = "Assets/Scenes/Arena.unity";
        const string BiomePath = "Assets/Data/AntColony.asset";

        [MenuItem("Shrunk/Rebuild Everything", priority = 20)]
        public static void BuildAll()
        {
            ArenaSceneBuilder.Build();
            Build();
        }

        [MenuItem("Shrunk/Rebuild Run Scene", priority = 21)]
        public static void Build()
        {
            ArenaSceneBuilder.EnsureFolder("Assets", "Materials");
            ArenaSceneBuilder.EnsureFolder("Assets", "Prefabs");
            ArenaSceneBuilder.EnsureFolder("Assets/Prefabs", "Props");
            ArenaSceneBuilder.EnsureFolder("Assets", "Data");
            ArenaSceneBuilder.EnsureFolder("Assets", "Scenes");

            var voidBody = ArenaSceneBuilder.Lit("VoidBody", new Color(0.03f, 0.03f, 0.05f), 0.35f);
            var eyeGlow = ArenaSceneBuilder.Unlit("EyeGlow", new Color(0.45f, 0.95f, 1f));
            var bulletGlow = ArenaSceneBuilder.Unlit("BulletGlow", new Color(0.6f, 1f, 0.95f));
            var antBody = ArenaSceneBuilder.Lit("AntBody", new Color(0.55f, 0.16f, 0.10f), 0.4f);
            var antDark = ArenaSceneBuilder.Lit("AntDark", new Color(0.16f, 0.06f, 0.05f), 0.4f);

            var bulletPrefab = ArenaSceneBuilder.BuildBulletPrefab(bulletGlow);
            var antPrefab = ArenaSceneBuilder.BuildAntPrefab(antBody, antDark);
            var biome = BuildBiome(antPrefab);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ArenaSceneBuilder.BuildLighting();
            var player = ArenaSceneBuilder.BuildPlayer(voidBody, eyeGlow, bulletPrefab);
            ArenaSceneBuilder.BuildCamera(player.transform);
            var touch = ArenaSceneBuilder.BuildHud();
            player.GetComponent<PlayerInputReader>().touch = touch;
            ExtendHud(touch.gameObject);

            var game = new GameObject("Game");
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

        // ---------- HUD additions ----------

        static void ExtendHud(GameObject canvas)
        {
            var hud = canvas.GetComponent<Hud>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Kills moves under the health pips so the minimap can have the top-right corner.
            var kills = (RectTransform)hud.killsText.transform;
            kills.anchorMin = kills.anchorMax = new Vector2(0f, 1f);
            kills.anchoredPosition = new Vector2(255f, -215f);
            hud.killsText.alignment = TextAnchor.MiddleLeft;
            hud.killsText.fontSize = 36;

            hud.floorText = ArenaSceneBuilder.UiText("Floor", canvas.transform, font, "Floor 1", 44, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(255f, -160f), new Vector2(400f, 60f));

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

        // ---------- biome ----------

        static BiomeDefinition BuildBiome(Enemy ant)
        {
            var wall = ArenaSceneBuilder.Lit("Wall", new Color(0.23f, 0.15f, 0.10f), 0.05f);
            var root = ArenaSceneBuilder.Lit("Root", new Color(0.3f, 0.2f, 0.12f), 0.1f);
            var gold = ArenaSceneBuilder.Unlit("Gold", new Color(1f, 0.82f, 0.2f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));
            var pad = ArenaSceneBuilder.Unlit("StartPad", new Color(0.2f, 0.5f, 0.55f));
            var beetle = ArenaSceneBuilder.Lit("Beetle", new Color(0.1f, 0.25f, 0.2f), 0.7f);

            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(BiomePath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, BiomePath);
            }

            biome.displayName = "Backyard Ant Colony";
            biome.floorMaterial = ArenaSceneBuilder.Lit("Soil", new Color(0.36f, 0.25f, 0.16f), 0.05f);
            biome.wallMaterial = wall;
            biome.doorMaterial = ArenaSceneBuilder.Lit("DoorLocked", new Color(0.85f, 0.3f, 0.15f), 0.3f);
            biome.enemies = new[] { ant };

            // Each prop fits inside the 3×3 tiles (6 m square) that every room keeps clear around its centre.
            var startProp = new GameObject("StartPad");
            Decor(PrimitiveType.Cylinder, "Pad", startProp.transform, new Vector3(0f, 0.01f, 0f), new Vector3(4f, 0.01f, 4f), pad);
            biome.startProp = ArenaSceneBuilder.SavePrefab(startProp, "Props/StartPad");

            var exit = new GameObject("ExitHole");
            Decor(PrimitiveType.Cylinder, "Rim", exit.transform, new Vector3(0f, 0.01f, 0f), new Vector3(3.6f, 0.01f, 3.6f), gold);
            Decor(PrimitiveType.Cylinder, "Hole", exit.transform, new Vector3(0f, 0.02f, 0f), new Vector3(3f, 0.02f, 3f), black);
            biome.exitProp = ArenaSceneBuilder.SavePrefab(exit, "Props/ExitHole");

            var treasure = new GameObject("TreasurePedestal");
            Solid(PrimitiveType.Cylinder, "Pedestal", treasure.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 0.5f, 1.4f), wall);
            Decor(PrimitiveType.Sphere, "Item Placeholder", treasure.transform, new Vector3(0f, 1.6f, 0f), Vector3.one * 0.8f, gold);
            biome.treasureProp = ArenaSceneBuilder.SavePrefab(treasure, "Props/TreasurePedestal");

            var shop = new GameObject("ShopStall");
            Solid(PrimitiveType.Cube, "Counter", shop.transform, new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 1.2f, 1f), root);
            Solid(PrimitiveType.Sphere, "Beetle Merchant", shop.transform, new Vector3(0f, 1f, 1.7f), new Vector3(1.6f, 1.6f, 1.6f), beetle);
            Decor(PrimitiveType.Sphere, "Ware 1", shop.transform, new Vector3(-0.5f, 1.45f, 0f), Vector3.one * 0.45f, gold);
            Decor(PrimitiveType.Sphere, "Ware 2", shop.transform, new Vector3(0.5f, 1.45f, 0f), Vector3.one * 0.45f, gold);
            biome.shopProp = ArenaSceneBuilder.SavePrefab(shop, "Props/ShopStall");

            EditorUtility.SetDirty(biome);
            return biome;
        }

        static GameObject Solid(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material) =>
            ArenaSceneBuilder.Primitive(type, name, parent, position, scale, material, true);

        static GameObject Decor(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material) =>
            ArenaSceneBuilder.Primitive(type, name, parent, position, scale, material, false);
    }
}
