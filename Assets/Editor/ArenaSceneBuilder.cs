using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// Builds the Arena scene: one empty room for trying out a single enemy. Also holds the helpers the other builders share.
    /// Menu: Shrunk > Rebuild Arena Scene. Rebuilding replaces the scene and prefabs, so hand edits to them are lost.
    /// </summary>
    public static class ArenaSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Arena.unity";
        internal const string MaterialFolder = "Assets/Materials";
        internal const string PrefabFolder = "Assets/Prefabs";
        const float ArenaHalfSize = 15f;

        [MenuItem("Shrunk/Rebuild Arena Scene")]
        public static void Build()
        {
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets", "Scenes");

            var soil = Lit("Soil", new Color(0.36f, 0.25f, 0.16f), 0.05f);
            var wall = Lit("Wall", new Color(0.23f, 0.15f, 0.10f), 0.05f);
            var voidBody = Lit("VoidBody", new Color(0.03f, 0.03f, 0.05f), 0.35f);
            var eyeGlow = Unlit("EyeGlow", new Color(0.45f, 0.95f, 1f));

            var biome = ContentBuilder.Build(out var bulletPrefab);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            BuildRoom(soil, wall);
            var player = BuildPlayer(voidBody, eyeGlow, bulletPrefab);
            BuildCamera(player.transform);
            var touch = BuildHud();
            player.GetComponent<PlayerInputReader>().touch = touch;

            var game = new GameObject("Game");
            ContentBuilder.AddGameAssets(game);
            game.AddComponent<GameLoop>();
            var spawner = game.AddComponent<EnemySpawner>();
            spawner.biome = biome;
            spawner.arenaHalfSize = ArenaHalfSize - 3f;

            EditorSceneManager.SaveScene(scene, ScenePath);
            RunSceneBuilder.UpdateBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Shrunk] Arena scene rebuilt at " + ScenePath);
        }

        // ---------- world ----------

        /// <param name="underground">Softer sun, much brighter fill light and a little dark haze: for rooms with a roof, where walls facing away from the sun would otherwise be black.</param>
        internal static void BuildLighting(bool underground = false)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = underground ? 0.9f : 1.3f;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = underground ? new Color(0.95f, 0.88f, 0.82f) : new Color(0.42f, 0.4f, 0.45f);
            RenderSettings.skybox = null;
            RenderSettings.fog = underground;
            if (underground)
            {
                // Far ends of long rooms fade into the dark, the same colour as the camera's background.
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = new Color(0.08f, 0.06f, 0.05f);
                RenderSettings.fogStartDistance = 22f;
                RenderSettings.fogEndDistance = 90f;
            }
        }

        internal static void BuildRoom(Material soil, Material wall)
        {
            var room = new GameObject("Room").transform;
            float size = ArenaHalfSize * 2f;

            Primitive(PrimitiveType.Cube, "Floor", room, new Vector3(0f, -0.5f, 0f), new Vector3(size, 1f, size), soil, true);

            const float height = 3f, thickness = 1f;
            float edge = ArenaHalfSize + thickness * 0.5f;
            Primitive(PrimitiveType.Cube, "Wall North", room, new Vector3(0f, height * 0.5f, edge), new Vector3(size + thickness * 2f, height, thickness), wall, true);
            Primitive(PrimitiveType.Cube, "Wall South", room, new Vector3(0f, height * 0.5f, -edge), new Vector3(size + thickness * 2f, height, thickness), wall, true);
            Primitive(PrimitiveType.Cube, "Wall East", room, new Vector3(edge, height * 0.5f, 0f), new Vector3(thickness, height, size), wall, true);
            Primitive(PrimitiveType.Cube, "Wall West", room, new Vector3(-edge, height * 0.5f, 0f), new Vector3(thickness, height, size), wall, true);
        }

        internal static void BuildCamera(Transform target)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.06f, 0.05f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            go.AddComponent<AudioListener>();
            go.AddComponent<CameraFollow>().target = target;
        }

        // ---------- player ----------

        internal static GameObject BuildPlayer(Material body, Material eyes, Bullet bulletPrefab)
        {
            var root = new GameObject("Player (The Void)");
            root.transform.position = new Vector3(0f, 0.05f, 0f);

            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            var health = root.AddComponent<Health>();
            health.team = Team.Player;
            health.maxHealth = 3;
            health.hitInvulnerability = 0.8f;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            visual.localPosition = new Vector3(0f, 0.9f, 0f);
            // The Void: a dark figure with glowing eyes. It fills the same space the old capsule did (1.8 m tall,
            // 0.4 m round), so every item look still sits where it was made to. The middle of "Visual" is the waist.
            Primitive(PrimitiveType.Sphere, "Head", visual, new Vector3(0f, 0.52f, 0.02f), Vector3.one * 0.64f, body, false);
            Primitive(PrimitiveType.Capsule, "Torso", visual, new Vector3(0f, -0.08f, 0f), new Vector3(0.6f, 0.42f, 0.46f), body, false);
            Primitive(PrimitiveType.Sphere, "Eye L", visual, new Vector3(-0.15f, 0.5f, 0.3f), Vector3.one * 0.13f, eyes, false);
            Primitive(PrimitiveType.Sphere, "Eye R", visual, new Vector3(0.15f, 0.5f, 0.3f), Vector3.one * 0.13f, eyes, false);

            // Legs hang from the hips and swing about them.
            Transform legLeft = Limb("Leg L", visual, new Vector3(-0.16f, -0.44f, 0f), new Vector3(-0.16f, -0.9f, 0f), 0.22f, body, false);
            Transform legRight = Limb("Leg R", visual, new Vector3(0.16f, -0.44f, 0f), new Vector3(0.16f, -0.9f, 0f), 0.22f, body, false);
            // Arms are held out in front, ready to shoot: the right hand is where the shots come from and where a
            // hand item's spike sits; the left is lower, where a gland hangs.
            Transform armLeft = Limb("Arm L", visual, new Vector3(-0.36f, 0.2f, 0f), new Vector3(-0.44f, -0.05f, 0.26f), 0.17f, body, true);
            Transform armRight = Limb("Arm R", visual, new Vector3(0.36f, 0.2f, 0f), new Vector3(0.44f, -0.1f, 0.42f), 0.17f, body, true);

            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(root.transform, false);
            // At the right hand: shots leave from where the arm points.
            muzzle.localPosition = new Vector3(0.44f, 0.78f, 0.62f);

            var weapon = root.AddComponent<Weapon>();
            weapon.bulletPrefab = bulletPrefab;
            weapon.muzzle = muzzle;

            root.AddComponent<PlayerInputReader>();
            var player = root.AddComponent<PlayerController>();
            player.weapon = weapon;
            player.visual = visual;
            root.AddComponent<HitFlash>();
            root.AddComponent<PlayerItems>();
            root.AddComponent<PlayerStyle>().styles = Styles(body, eyes);
            var voidBody = root.AddComponent<VoidBody>();
            voidBody.legLeft = legLeft;
            voidBody.legRight = legRight;
            voidBody.armLeft = armLeft;
            voidBody.armRight = armRight;
            var power = root.AddComponent<HeroPower>();
            power.eyes = new[] { visual.Find("Eye L"), visual.Find("Eye R") };
            power.surgeMaterial = eyes;
            return root;
        }

        // ---------- HUD ----------

        internal static TouchControls BuildHud()
        {
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var parent = canvasGo.transform;
            var white = new Color(1f, 1f, 1f, 0.22f);
            var glow = new Color(0.45f, 0.95f, 1f, 1f);

            // Sticks rest in the lower corners and jump to wherever the thumb lands.
            var moveBase = UiImage("Move Stick", parent, knob, white, Vector2.zero, new Vector2(300f, 280f), 260f);
            var moveKnob = UiImage("Knob", moveBase, knob, new Color(1f, 1f, 1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 110f);
            var aimBase = UiImage("Aim Stick", parent, knob, white, new Vector2(1f, 0f), new Vector2(-520f, 280f), 260f);
            var aimKnob = UiImage("Knob", aimBase, knob, new Color(glow.r, glow.g, glow.b, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, 110f);

            var dodge = UiImage("Dodge Button", parent, knob, white, new Vector2(1f, 0f), new Vector2(-190f, 200f), 200f);
            var fill = UiImage("Ready Ring", dodge, knob, new Color(glow.r, glow.g, glow.b, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, 200f).GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            var dodgeLabel = UiText("Label", dodge, font, "DODGE", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 60f));
            dodgeLabel.color = Color.white;

            var pips = new Image[3];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = UiImage("Health " + (i + 1), parent, knob, glow, new Vector2(0f, 1f), new Vector2(80f + i * 70f, -80f), 54f).GetComponent<Image>();
            }

            var kills = UiText("Kills", parent, font, "Kills: 0", 44, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-260f, -80f), new Vector2(400f, 60f));
            var hint = UiText("Desktop Hint", parent, font, "WASD move  ·  mouse aim  ·  hold click to fire  ·  Space dodge", 30, TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(1700f, 50f));
            FitAcross(hint, 18f);
            hint.color = new Color(1f, 1f, 1f, 0.6f);

            var crosshair = UiImage("Crosshair", parent, knob, new Color(1f, 1f, 1f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, 14f).GetComponent<Image>();

            var cameraButton = UiImage("Camera Button", parent, null, new Color(0f, 0f, 0f, 0.35f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), 70f);
            cameraButton.sizeDelta = new Vector2(420f, 70f);
            var cameraLabel = UiText("Label", cameraButton, font, "V  camera: Shoulder", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 70f));

            var resources = UiText("Resources", parent, font, "Coins 0   ·   Samples 0", 34, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(255f, -140f), new Vector2(400f, 50f));
            var prompt = UiText("Prompt", parent, font, "", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(1700f, 60f));
            // Above the hint, and growing upward: a long prompt wraps onto a second line inside the screen.
            FitAcross(prompt, 104f);
            prompt.color = new Color(1f, 0.95f, 0.7f);

            // Hero power: above the right thumb. On PC it stays on screen as the meter.
            var power = UiImage("Power Button", parent, knob, white, new Vector2(1f, 0f), new Vector2(-190f, 440f), 180f);
            var powerFill = UiImage("Meter", power, knob, new Color(1f, 0.8f, 0.2f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, 180f).GetComponent<Image>();
            powerFill.type = Image.Type.Filled;
            powerFill.fillMethod = Image.FillMethod.Radial360;
            powerFill.fillOrigin = (int)Image.Origin360.Top;
            powerFill.fillAmount = 0f;
            var powerLabel = UiText("Label", power, font, "POWER", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 80f));

            var touch = canvasGo.AddComponent<TouchControls>();
            touch.cameraButton = cameraButton;
            touch.powerButton = power;
            touch.moveBase = moveBase;
            touch.moveKnob = moveKnob;
            touch.aimBase = aimBase;
            touch.aimKnob = aimKnob;
            touch.dodgeButton = dodge;

            var hud = canvasGo.AddComponent<Hud>();
            hud.healthPips = pips;
            hud.killsText = kills;
            hud.dodgeFill = fill;
            hud.desktopHint = hint.gameObject;
            hud.touch = touch;
            hud.crosshair = crosshair;
            hud.cameraLabel = cameraLabel;
            hud.resourcesText = resources;
            hud.promptText = prompt;
            hud.powerFill = powerFill;
            hud.powerLabel = powerLabel;
            canvasGo.AddComponent<EnemyArrows>();
            canvasGo.AddComponent<PauseMenu>();

            // Boss health bar, under the camera label.
            var bossBar = UiImage("Boss Bar", parent, null, new Color(0f, 0f, 0f, 0.55f), new Vector2(0.5f, 1f), new Vector2(0f, -185f), 30f);
            bossBar.sizeDelta = new Vector2(900f, 34f);
            var bossFill = UiImage("Fill", bossBar, null, new Color(0.85f, 0.2f, 0.15f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, 30f);
            bossFill.sizeDelta = new Vector2(892f, 26f);
            // A plain block pinned to the left end of the bar; the HUD shrinks its width as the boss loses health.
            bossFill.pivot = new Vector2(0f, 0.5f);
            bossFill.anchorMin = bossFill.anchorMax = new Vector2(0f, 0.5f);
            bossFill.anchoredPosition = new Vector2(4f, 0f);
            var bossFillImage = bossFill.GetComponent<Image>();
            hud.bossName = UiText("Name", bossBar, font, "Boss", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 38f), new Vector2(900f, 40f));
            hud.bossBar = bossBar.gameObject;
            hud.bossFill = bossFillImage;
            bossBar.gameObject.SetActive(false);
            return touch;
        }

        internal static RectTransform UiImage(string name, Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 position, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        // Stretches a line of text across the bottom of the screen with a margin either side, whatever shape the
        // screen is, and lets it wrap upward instead of running off the edges.
        static void FitAcross(Text text, float fromBottom)
        {
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.05f, 0f);
            rect.anchorMax = new Vector2(0.95f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, fromBottom);
            rect.sizeDelta = new Vector2(0f, 60f);
            text.alignment = TextAnchor.LowerCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        internal static Text UiText(string name, Transform parent, Font font, string content, int size, TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 box)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = box;
            var text = go.GetComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            // A dark edge, so white text reads against a bright biome as well as a dark one.
            var edge = go.AddComponent<Outline>();
            edge.effectColor = new Color(0f, 0f, 0f, 0.75f);
            edge.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        // ---------- helpers ----------

        /// <summary>The character styles: full swaps of what the Void is made of. The first is the one it is built in.</summary>
        internal static VoidStyle[] Styles(Material shadowBody, Material shadowEyes)
        {
            var chrome = Lit("StyleChrome", new Color(0.82f, 0.84f, 0.88f), 0.92f);
            chrome.SetFloat("_Metallic", 1f);
            var ember = Lit("StyleEmber", new Color(0.3f, 0.05f, 0.02f), 0.5f);
            var toy = Lit("StyleToy", new Color(0.12f, 0.4f, 0.95f), 0.85f);
            var ghost = Lit("StyleGhost", new Color(0.9f, 0.93f, 0.97f), 0.15f);
            var gold = Lit("StyleGold", new Color(1f, 0.78f, 0.25f), 0.9f);
            gold.SetFloat("_Metallic", 1f);
            return new[]
            {
                new VoidStyle { displayName = "Shadow", body = shadowBody, eyes = shadowEyes },
                new VoidStyle { displayName = "Chrome", body = chrome, eyes = Unlit("StyleEyesWhite", new Color(0.95f, 1f, 1f)) },
                new VoidStyle { displayName = "Ember", body = ember, eyes = Unlit("Fire", new Color(1f, 0.55f, 0.1f)) },
                new VoidStyle { displayName = "Toy", body = toy, eyes = Unlit("StyleEyesYellow", new Color(1f, 0.92f, 0.3f)) },
                new VoidStyle { displayName = "Ghost", body = ghost, eyes = Unlit("StyleEyesDark", new Color(0.05f, 0.05f, 0.1f)) },
                new VoidStyle { displayName = "Gold", body = gold, eyes = Unlit("StyleEyesRed", new Color(1f, 0.2f, 0.2f)) },
                new VoidStyle { displayName = "Lint", body = Lit("Dust", new Color(0.62f, 0.6f, 0.58f), 0.05f), eyes = Unlit("StaticSpark", new Color(1f, 0.95f, 0.4f)) },
                new VoidStyle { displayName = "Frost", body = Lit("IceCube", new Color(0.7f, 0.88f, 1f), 0.95f), eyes = Unlit("StyleEyesWhite", new Color(0.95f, 1f, 1f)) },
                new VoidStyle { displayName = "Paper", body = Lit("StylePaper", new Color(0.93f, 0.89f, 0.76f), 0.05f), eyes = Unlit("StyleEyesDark", new Color(0.05f, 0.05f, 0.1f)) },
                new VoidStyle { displayName = "Brass", body = Lit("StyleBrass", new Color(0.78f, 0.6f, 0.22f), 0.8f), eyes = Unlit("StyleEyesDark", new Color(0.05f, 0.05f, 0.1f)) },
                new VoidStyle { displayName = "Beetle", body = Lit("StyleBeetle", new Color(0.1f, 0.4f, 0.25f), 0.9f), eyes = Unlit("Gold", new Color(1f, 0.82f, 0.2f)) },
                new VoidStyle { displayName = "Ink", body = Lit("StyleInk", new Color(0.08f, 0.1f, 0.3f), 0.85f), eyes = Unlit("StyleEyesWhite", new Color(0.95f, 1f, 1f)) },
                new VoidStyle { displayName = "Lab Coat", body = Lit("LabCoat", new Color(0.93f, 0.94f, 0.96f), 0.3f), eyes = Unlit("FinchDose", new Color(0.45f, 1f, 0.6f)) },
                new VoidStyle { displayName = "Moss", body = Lit("StyleMoss", new Color(0.28f, 0.5f, 0.18f), 0.15f), eyes = Unlit("ToadEye", new Color(1f, 0.75f, 0.15f)) },
            };
        }

        // A limb from one point to another: an empty at the first point (the joint it turns about), holding a capsule
        // that reaches to the second, with a ball on the end for a hand if asked for.
        static Transform Limb(string name, Transform parent, Vector3 from, Vector3 to, float thickness, Material material, bool hand)
        {
            var joint = new GameObject(name).transform;
            joint.SetParent(parent, false);
            joint.localPosition = from;
            Vector3 along = to - from;
            float length = along.magnitude;
            joint.localRotation = Quaternion.FromToRotation(Vector3.down, along.normalized);

            Primitive(PrimitiveType.Capsule, "Limb", joint, new Vector3(0f, -length * 0.5f, 0f), new Vector3(thickness, length * 0.5f, thickness), material, false);
            if (hand) Primitive(PrimitiveType.Sphere, "Hand", joint, new Vector3(0f, -length, 0f), Vector3.one * (thickness * 1.25f), material, false);
            return joint;
        }

        internal static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        internal static GameObject SavePrefab(GameObject instance, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabFolder + "/" + name + ".prefab");
            Object.DestroyImmediate(instance);
            return prefab;
        }

        internal static Material Lit(string name, Color color, float smoothness)
        {
            var material = LoadOrCreate(name, "Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        internal static Material Unlit(string name, Color color)
        {
            var material = LoadOrCreate(name, "Universal Render Pipeline/Unlit");
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Reuses the existing asset so hand-tuned references to it survive a rebuild.
        internal static Material LoadOrCreate(string name, string shaderName)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var shader = Shader.Find(shaderName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            return material;
        }

        internal static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
