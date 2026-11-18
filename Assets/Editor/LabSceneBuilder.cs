using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// Builds the Lab scene: the hub between runs. A small room you walk around in, with the workbench (six upgrade
    /// pads) along one wall and the portal opposite. Menu: Shrunk > Rebuild Lab Scene.
    /// </summary>
    public static class LabSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Lab.unity";

        [MenuItem("Shrunk/Rebuild Lab Scene", priority = 22)]
        public static void Build()
        {
            ArenaSceneBuilder.EnsureFolder("Assets", "Scenes");
            ContentBuilder.Build(out var bulletPrefab);
            UpgradeDefinition[] upgrades = ContentBuilder.BuildUpgrades();

            var floorMat = ArenaSceneBuilder.Lit("LabFloor", new Color(0.32f, 0.36f, 0.42f), 0.5f);
            var wallMat = ArenaSceneBuilder.Lit("LabWall", new Color(0.18f, 0.2f, 0.26f), 0.3f);
            var benchMat = ArenaSceneBuilder.Lit("LabBench", new Color(0.5f, 0.52f, 0.56f), 0.7f);
            var voidBody = ArenaSceneBuilder.Lit("VoidBody", new Color(0.03f, 0.03f, 0.05f), 0.35f);
            var eyeGlow = ArenaSceneBuilder.Unlit("EyeGlow", new Color(0.45f, 0.95f, 1f));
            var portalGlow = ArenaSceneBuilder.Unlit("EyeGlow", new Color(0.45f, 0.95f, 1f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ArenaSceneBuilder.BuildLighting();

            // ----- the room -----
            const float halfWidth = 11f, halfDepth = 8f, height = 4f;
            var room = new GameObject("Lab Room").transform;
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Floor", room, new Vector3(0f, -0.5f, 0f), new Vector3(halfWidth * 2f, 1f, halfDepth * 2f), floorMat, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall North", room, new Vector3(0f, height * 0.5f, halfDepth + 0.5f), new Vector3(halfWidth * 2f + 2f, height, 1f), wallMat, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall South", room, new Vector3(0f, height * 0.5f, -halfDepth - 0.5f), new Vector3(halfWidth * 2f + 2f, height, 1f), wallMat, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall East", room, new Vector3(halfWidth + 0.5f, height * 0.5f, 0f), new Vector3(1f, height, halfDepth * 2f), wallMat, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall West", room, new Vector3(-halfWidth - 0.5f, height * 0.5f, 0f), new Vector3(1f, height, halfDepth * 2f), wallMat, true);

            // ----- the workbench: a long bench with one pad per upgrade in front of it -----
            var bench = new GameObject("Workbench").transform;
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Bench", bench, new Vector3(0f, 0.55f, 6.6f), new Vector3(19f, 1.1f, 1.6f), benchMat, true);

            Color[] colours =
            {
                new Color(0.9f, 0.3f, 0.35f), new Color(0.3f, 0.6f, 1f), new Color(0.45f, 0.95f, 1f),
                new Color(1f, 0.8f, 0.2f), new Color(0.5f, 0.9f, 0.45f), new Color(0.85f, 0.5f, 1f)
            };
            for (int i = 0; i < upgrades.Length; i++)
            {
                var upgrade = upgrades[i];
                var glow = ArenaSceneBuilder.Unlit("Upgrade" + upgrade.type, colours[i % colours.Length]);
                float x = -8f + i * 3.2f;

                var station = new GameObject(upgrade.displayName + " Pad");
                station.transform.SetParent(bench, false);
                station.transform.position = new Vector3(x, 0f, 4.2f);
                ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Pad", station.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.8f, 0.02f, 1.8f), glow, false);

                // Equipment on the bench that lights up level by level, so progress is something you can see.
                ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Machine", station.transform, new Vector3(0f, 1.35f, 2.4f), new Vector3(1.2f, 0.5f, 0.9f), wallMat, false);
                var markers = new GameObject[upgrade.MaxLevel];
                for (int level = 0; level < upgrade.MaxLevel; level++)
                {
                    markers[level] = ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Level " + (level + 1), station.transform,
                        new Vector3(0f, 1.85f + level * 0.45f, 2.4f), new Vector3(0.6f, 0.35f, 0.6f), glow, false);
                    markers[level].SetActive(false);
                }

                var pad = station.AddComponent<UpgradePad>();
                pad.upgrade = upgrade;
                pad.levelMarkers = markers;
            }

            // ----- the portal -----
            var portal = ContentBuilder.Portal("Portal", portalGlow, black, benchMat);
            portal.transform.position = new Vector3(0f, 0f, -5.2f);
            portal.AddComponent<LabPortal>();

            // ----- player, camera, HUD -----
            var player = ArenaSceneBuilder.BuildPlayer(voidBody, eyeGlow, bulletPrefab);
            player.transform.position = new Vector3(0f, 0.05f, -1f);
            ArenaSceneBuilder.BuildCamera(player.transform);
            var touch = ArenaSceneBuilder.BuildHud();
            player.GetComponent<PlayerInputReader>().touch = touch;

            var hud = touch.GetComponent<Hud>();
            hud.lab = true;
            hud.killsText.gameObject.SetActive(false);
            hud.powerFill.transform.parent.gameObject.SetActive(false); // nothing to fight in the lab

            var game = new GameObject("Game");
            ContentBuilder.AddGameAssets(game);
            game.AddComponent<LabDirector>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            RunSceneBuilder.UpdateBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Shrunk] Lab scene rebuilt at " + ScenePath);
        }
    }
}
