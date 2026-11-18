using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// Builds the Milestone 2 content: placeholder Ant Colony rooms, the biome data file and the Run scene.
    /// Menu: Shrunk > Rebuild Run Scene. Rebuilding replaces the scene and the room prefabs, so hand edits to them are lost.
    /// </summary>
    public static class RunSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Run.unity";
        const string ArenaPath = "Assets/Scenes/Arena.unity";
        const string BiomePath = "Assets/Data/AntColony.asset";

        // Every room fills one 24 m grid square: 22 m of floor inside 1 m walls, with a 4 m doorway on each side.
        const float Cell = 24f;
        const float WallThickness = 1f;
        const float WallHeight = 3f;
        const float DoorWidth = 4f;

        static Material _soil, _wall, _door, _sugar, _crumb, _root, _pod, _dew, _gold, _black, _pad, _beetle;

        [MenuItem("Shrunk/Rebuild Everything")]
        public static void BuildAll()
        {
            ArenaSceneBuilder.Build();
            Build();
        }

        [MenuItem("Shrunk/Rebuild Run Scene")]
        public static void Build()
        {
            ArenaSceneBuilder.EnsureFolder("Assets", "Materials");
            ArenaSceneBuilder.EnsureFolder("Assets", "Prefabs");
            ArenaSceneBuilder.EnsureFolder("Assets/Prefabs", "Rooms");
            ArenaSceneBuilder.EnsureFolder("Assets", "Data");
            ArenaSceneBuilder.EnsureFolder("Assets", "Scenes");

            _soil = ArenaSceneBuilder.Lit("Soil", new Color(0.36f, 0.25f, 0.16f), 0.05f);
            _wall = ArenaSceneBuilder.Lit("Wall", new Color(0.23f, 0.15f, 0.10f), 0.05f);
            _door = ArenaSceneBuilder.Lit("DoorLocked", new Color(0.85f, 0.3f, 0.15f), 0.3f);
            _sugar = ArenaSceneBuilder.Lit("Sugar", new Color(0.95f, 0.95f, 0.92f), 0.6f);
            _crumb = ArenaSceneBuilder.Lit("Crumb", new Color(0.78f, 0.6f, 0.35f), 0.1f);
            _root = ArenaSceneBuilder.Lit("Root", new Color(0.3f, 0.2f, 0.12f), 0.1f);
            _pod = ArenaSceneBuilder.Lit("LarvaPod", new Color(0.9f, 0.85f, 0.6f), 0.5f);
            _dew = ArenaSceneBuilder.Unlit("Dew", new Color(0.3f, 0.6f, 0.95f));
            _gold = ArenaSceneBuilder.Unlit("Gold", new Color(1f, 0.82f, 0.2f));
            _black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));
            _pad = ArenaSceneBuilder.Unlit("StartPad", new Color(0.2f, 0.5f, 0.55f));
            _beetle = ArenaSceneBuilder.Lit("Beetle", new Color(0.1f, 0.25f, 0.2f), 0.7f);

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
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(BiomePath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, BiomePath);
            }

            biome.displayName = "Backyard Ant Colony";
            biome.startRoom = StartRoom();
            biome.exitRoom = ExitRoom();
            biome.treasureRoom = TreasureRoom();
            biome.shopRoom = ShopRoom();
            biome.normalRooms = new[] { OpenChamber(), TunnelBend(), FoodStore(), RootHall(), Nursery(), DewRoom() };
            biome.enemies = new[] { ant };
            EditorUtility.SetDirty(biome);
            return biome;
        }

        // ---------- rooms ----------

        static readonly Vector3[] CornerSpawns =
        {
            new Vector3(7f, 0f, 7f), new Vector3(-7f, 0f, -7f), new Vector3(-7f, 0f, 7f), new Vector3(7f, 0f, -7f),
            new Vector3(0f, 0f, 8f), new Vector3(0f, 0f, -8f)
        };

        static Room StartRoom()
        {
            var room = Shell("Start", null, out var props);
            Decor(PrimitiveType.Cylinder, "Landing Pad", props, new Vector3(0f, 0.01f, 0f), new Vector3(5f, 0.01f, 5f), _pad);
            return Save(room);
        }

        static Room ExitRoom()
        {
            var room = Shell("Exit", CornerSpawns, out var props);
            var hole = Decor(PrimitiveType.Cylinder, "Exit Hole", room.transform, new Vector3(0f, 0.02f, 0f), new Vector3(3.2f, 0.02f, 3.2f), _black);
            Decor(PrimitiveType.Cylinder, "Rim", hole.transform, new Vector3(0f, -0.5f, 0f), new Vector3(1.15f, 0.5f, 1.15f), _gold);
            hole.SetActive(false);
            room.exitHole = hole;
            return Save(room);
        }

        static Room TreasureRoom()
        {
            var room = Shell("Treasure", null, out var props);
            Solid(PrimitiveType.Cylinder, "Pedestal", props, new Vector3(0f, 0.5f, 0f), new Vector3(1.6f, 0.5f, 1.6f), _wall);
            Decor(PrimitiveType.Sphere, "Item Placeholder", props, new Vector3(0f, 1.6f, 0f), Vector3.one * 0.8f, _gold);
            return Save(room);
        }

        static Room ShopRoom()
        {
            var room = Shell("Shop", null, out var props);
            Solid(PrimitiveType.Cube, "Counter", props, new Vector3(0f, 0.6f, 6f), new Vector3(9f, 1.2f, 1.5f), _root);
            Solid(PrimitiveType.Sphere, "Beetle Merchant", props, new Vector3(0f, 1.2f, 8.2f), new Vector3(2.6f, 2f, 2.2f), _beetle);
            for (int i = -1; i <= 1; i++)
            {
                Solid(PrimitiveType.Cylinder, "Stand " + (i + 2), props, new Vector3(i * 3f, 0.4f, 3.5f), new Vector3(1f, 0.4f, 1f), _wall);
                Decor(PrimitiveType.Sphere, "Ware " + (i + 2), props, new Vector3(i * 3f, 1.2f, 3.5f), Vector3.one * 0.6f, _gold);
            }
            return Save(room);
        }

        /// <summary>The big fight room: nothing in the way, room to kite.</summary>
        static Room OpenChamber()
        {
            var room = Shell("OpenChamber", CornerSpawns, out _);
            return Save(room);
        }

        /// <summary>Two big blocks in opposite corners squeeze the room into a diagonal passage.</summary>
        static Room TunnelBend()
        {
            var spawns = new[]
            {
                new Vector3(7f, 0f, 7f), new Vector3(-7f, 0f, -7f), new Vector3(8f, 0f, 3f), new Vector3(-8f, 0f, -3f),
                new Vector3(3f, 0f, 8f), new Vector3(-3f, 0f, -8f)
            };
            var room = Shell("TunnelBend", spawns, out var props);
            Solid(PrimitiveType.Cube, "Earth NW", props, new Vector3(-6.75f, 1.5f, 6.75f), new Vector3(8.5f, 3f, 8.5f), _wall);
            Solid(PrimitiveType.Cube, "Earth SE", props, new Vector3(6.75f, 1.5f, -6.75f), new Vector3(8.5f, 3f, 8.5f), _wall);
            return Save(room);
        }

        /// <summary>Giant crumbs and a sugar cube as cover.</summary>
        static Room FoodStore()
        {
            var room = Shell("FoodStore", CornerSpawns, out var props);
            Solid(PrimitiveType.Cube, "Sugar Cube", props, new Vector3(-3.5f, 1.4f, 2.5f), new Vector3(2.8f, 2.8f, 2.8f), _sugar).transform.localRotation = Quaternion.Euler(0f, 20f, 0f);
            Solid(PrimitiveType.Sphere, "Crumb 1", props, new Vector3(4f, 0.9f, -3f), new Vector3(3f, 1.8f, 2.4f), _crumb);
            Solid(PrimitiveType.Sphere, "Crumb 2", props, new Vector3(3.5f, 0.7f, 4f), new Vector3(1.8f, 1.4f, 2f), _crumb);
            Solid(PrimitiveType.Sphere, "Crumb 3", props, new Vector3(-4.5f, 0.6f, -4f), new Vector3(2f, 1.2f, 1.6f), _crumb);
            return Save(room);
        }

        /// <summary>Thick roots as pillars, for breaking line of sight.</summary>
        static Room RootHall()
        {
            var room = Shell("RootHall", CornerSpawns, out var props);
            var spots = new[] { new Vector3(4.5f, 0f, 4.5f), new Vector3(-4.5f, 0f, 4.5f), new Vector3(4.5f, 0f, -4.5f), new Vector3(-4.5f, 0f, -4.5f), Vector3.zero };
            for (int i = 0; i < spots.Length; i++)
            {
                float width = i == 4 ? 2.2f : 1.6f;
                Solid(PrimitiveType.Cylinder, "Root " + (i + 1), props, spots[i] + Vector3.up * 1.5f, new Vector3(width, 1.5f, width), _root);
            }
            return Save(room);
        }

        /// <summary>Larva pods along the walls. They are scenery for now; popping them comes with items.</summary>
        static Room Nursery()
        {
            var room = Shell("Nursery", CornerSpawns, out var props);
            var spots = new[]
            {
                new Vector3(9.5f, 0f, 9.5f), new Vector3(-9.5f, 0f, 9.5f), new Vector3(9.5f, 0f, -9.5f), new Vector3(-9.5f, 0f, -9.5f),
                new Vector3(9.8f, 0f, 4f), new Vector3(-9.8f, 0f, -4f), new Vector3(4f, 0f, -9.8f), new Vector3(-4f, 0f, 9.8f),
                new Vector3(2.5f, 0f, 1.5f), new Vector3(-2f, 0f, -2.5f)
            };
            for (int i = 0; i < spots.Length; i++)
            {
                Solid(PrimitiveType.Sphere, "Larva Pod " + (i + 1), props, spots[i] + Vector3.up * 0.7f, new Vector3(1.3f, 1.6f, 1.3f), _pod);
            }
            return Save(room);
        }

        /// <summary>Marked spots where water will fall. Scenery for now; the slow effect comes later.</summary>
        static Room DewRoom()
        {
            var room = Shell("DewRoom", CornerSpawns, out var props);
            var spots = new[] { new Vector3(4f, 0f, 2f), new Vector3(-3f, 0f, 5f), new Vector3(-5f, 0f, -3f), new Vector3(2f, 0f, -5.5f), new Vector3(0f, 0f, 0f) };
            for (int i = 0; i < spots.Length; i++)
            {
                Decor(PrimitiveType.Cylinder, "Dew Mark " + (i + 1), props, spots[i] + Vector3.up * 0.01f, new Vector3(3f, 0.01f, 3f), _dew);
                Decor(PrimitiveType.Sphere, "Droplet " + (i + 1), props, spots[i] + Vector3.up * 2.6f, new Vector3(0.9f, 1.1f, 0.9f), _dew);
            }
            return Save(room);
        }

        /// <summary>Floor, four walls with a doorway each, and the door pieces the generator switches on and off.</summary>
        static Room Shell(string name, Vector3[] spawns, out Transform props)
        {
            var root = new GameObject(name);
            var room = root.AddComponent<Room>();
            room.interiorHalfSize = Cell * 0.5f - WallThickness;

            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Floor", root.transform, new Vector3(0f, -0.5f, 0f), new Vector3(Cell, 1f, Cell), _soil, true);

            var walls = new GameObject("Walls").transform;
            walls.SetParent(root.transform, false);
            float edge = Cell * 0.5f - WallThickness * 0.5f;
            float sideLength = (Cell - DoorWidth) * 0.5f;
            float sideOffset = DoorWidth * 0.5f + sideLength * 0.5f;
            string[] names = { "North", "East", "South", "West" };

            room.slots = new DoorSlot[4];
            for (int i = 0; i < 4; i++)
            {
                Vector2Int d = FloorLayout.Directions[i];
                var outward = new Vector3(d.x, 0f, d.y);
                var along = new Vector3(d.y, 0f, d.x); // runs along the wall
                Vector3 centre = outward * edge + Vector3.up * WallHeight * 0.5f;

                Vector3 Size(float length, float thickness) =>
                    new Vector3(Mathf.Abs(along.x) * length + Mathf.Abs(outward.x) * thickness, WallHeight,
                                Mathf.Abs(along.z) * length + Mathf.Abs(outward.z) * thickness);

                ArenaSceneBuilder.Primitive(PrimitiveType.Cube, names[i] + " Wall A", walls, centre + along * sideOffset, Size(sideLength, WallThickness), _wall, true);
                ArenaSceneBuilder.Primitive(PrimitiveType.Cube, names[i] + " Wall B", walls, centre - along * sideOffset, Size(sideLength, WallThickness), _wall, true);

                room.slots[i] = new DoorSlot
                {
                    wallFill = ArenaSceneBuilder.Primitive(PrimitiveType.Cube, names[i] + " Wall Fill", walls, centre, Size(DoorWidth, WallThickness), _wall, true),
                    door = ArenaSceneBuilder.Primitive(PrimitiveType.Cube, names[i] + " Door", walls, centre, Size(DoorWidth, WallThickness * 0.6f), _door, true)
                };
                room.slots[i].door.SetActive(false);
            }

            props = new GameObject("Props").transform;
            props.SetParent(root.transform, false);

            var spawnRoot = new GameObject("Enemy Spawns").transform;
            spawnRoot.SetParent(root.transform, false);
            var points = new List<Transform>();
            if (spawns != null)
            {
                for (int i = 0; i < spawns.Length; i++)
                {
                    var point = new GameObject("Spawn " + (i + 1)).transform;
                    point.SetParent(spawnRoot, false);
                    point.localPosition = spawns[i];
                    points.Add(point);
                }
            }
            room.enemySpawns = points.ToArray();
            return room;
        }

        static GameObject Solid(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material) =>
            ArenaSceneBuilder.Primitive(type, name, parent, position, scale, material, true);

        static GameObject Decor(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material) =>
            ArenaSceneBuilder.Primitive(type, name, parent, position, scale, material, false);

        static Room Save(Room room) =>
            ArenaSceneBuilder.SavePrefab(room.gameObject, "Rooms/" + room.name).GetComponent<Room>();
    }
}
