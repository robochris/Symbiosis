using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Shrunk
{
    /// <summary>
    /// A menu for trying things out without playing up to them: start in any world at any floor, skip ahead, beat the
    /// boss, take no damage, hand out items. F1 opens and closes it on a PC; on a phone, tap with three fingers at once.
    /// It works in the lab (start a run anywhere) and in a run (jump anywhere). Set Enabled to false for a public release.
    /// </summary>
    public class DevMenu : MonoBehaviour
    {
        public const bool Enabled = true;

        public static DevMenu Instance { get; private set; }
        public static bool Open { get; set; }
        /// <summary>No damage taken while on.</summary>
        public static bool God { get; set; }

        static readonly string[] StageNames = { "1", "2", "3 (mini-boss)", "4", "5", "6 (boss)" };

        Vector2 _scroll;
        int _stage;
        string _note;
        float _noteUntil;
        bool _wasThreeFingers;
        float _speed = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (!Enabled || Instance != null) return;
            var go = new GameObject("Dev menu");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<DevMenu>();
        }

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!Mathf.Approximately(_speed, 1f)) Time.timeScale = 1f;
            _speed = 1f;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame) Toggle();

            var touch = Touchscreen.current;
            if (touch != null)
            {
                int down = 0;
                foreach (var t in touch.touches) if (t.press.isPressed) down++;
                bool three = down >= 3;
                if (three && !_wasThreeFingers) Toggle();
                _wasThreeFingers = three;
            }

            if (God)
            {
                var player = PlayerController.Instance;
                if (player != null && player.Health != null) player.Health.SetInvulnerable(0.5f);
            }
        }

        void Toggle()
        {
            Open = !Open;
            _scroll = Vector2.zero;
        }

        void Say(string note)
        {
            _note = note;
            _noteUntil = Time.unscaledTime + 3f;
        }

        // ---------- what the buttons do ----------

        static List<BiomeDefinition> Worlds(FloorManager floors)
        {
            var list = new List<BiomeDefinition>();
            if (floors == null) return list;
            foreach (var world in floors.AllBiomes()) if (world != null && !world.isBottom) list.Add(world);
            return list;
        }

        /// <summary>Straight to this floor (1 to FloorsPerBiome) of this world, in the run under way.</summary>
        public static void JumpTo(string worldId, int stage)
        {
            var floors = FloorManager.Instance;
            if (floors == null || !floors.SetBiome(worldId)) return;
            int perWorld = Mathf.Max(1, FloorLayout.FloorsPerBiome);
            stage = Mathf.Clamp(stage, 1, perWorld);
            floors.BuildFloor((Mathf.Max(1, floors.depth) - 1) * perWorld + stage);
        }

        /// <summary>From the lab: a fresh run, starting on this floor of this world (null: the usual first world).</summary>
        public static void StartRunAt(string worldId, int stage)
        {
            RunDirector.DevWorld = worldId ?? "";
            RunDirector.DevStage = stage;
            SaveSystem.Data.run.active = false;
            SaveSystem.Save();
            Open = false;
            SceneManager.LoadScene("Run");
        }

        static void KillAll(bool bossToo)
        {
            var list = new List<Enemy>(Enemy.Active);
            foreach (var enemy in list)
            {
                if (enemy == null || enemy.Health == null) continue;
                if (!bossToo && enemy is BossEnemy) continue;
                enemy.Health.Kill();
            }
        }

        static void GiveItem(bool all)
        {
            var player = PlayerController.Instance;
            var assets = GameAssets.Instance;
            if (player == null || player.Items == null || assets == null || assets.items == null) return;
            if (all)
            {
                foreach (var item in assets.items.items) if (item != null) player.Items.Add(item);
                return;
            }
            var picks = assets.items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), 1);
            if (picks.Count > 0) player.Items.Add(picks[0]);
        }

        // ---------- drawing ----------

        void OnGUI()
        {
            if (!Open)
            {
                if (_note != null && Time.unscaledTime < _noteUntil) GUI.Label(new Rect(10, 10, 600, 30), _note);
                return;
            }

            float scale = Mathf.Max(1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = 460f, h = Screen.height / scale - 20f;
            GUI.skin.button.fontSize = 15;
            GUI.skin.label.fontSize = 15;
            GUI.skin.toggle.fontSize = 15;

            GUILayout.BeginArea(new Rect(10, 10, w, h), GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>DEV MENU</b>  (F1 / three fingers)");
            if (GUILayout.Button("Close", GUILayout.Width(70))) Open = false;
            GUILayout.EndHorizontal();
            if (_note != null && Time.unscaledTime < _noteUntil) GUILayout.Label(_note);

            _scroll = GUILayout.BeginScrollView(_scroll);
            var floors = FloorManager.Instance;
            bool inRun = floors != null && RunDirector.Instance != null;

            if (inRun)
            {
                int stage = FloorLayout.FloorOfBiome(floors.FloorNumber);
                GUILayout.Label($"Now: {floors.biome?.displayName}, floor {stage} of {FloorLayout.FloorsPerBiome}");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Next floor")) floors.BuildFloor(floors.FloorNumber + 1);
                if (GUILayout.Button("Boss floor")) JumpTo(floors.biome.id, FloorLayout.FloorsPerBiome);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Go to boss room"))
                {
                    var player = PlayerController.Instance;
                    if (player != null && FloorLayout.IsBossFloor(floors.FloorNumber)) player.Teleport(floors.ArenaCentre + new Vector3(0f, 0.05f, -9f));
                    else Say("Not a boss floor.");
                }
                if (GUILayout.Button("Kill enemies")) KillAll(false);
                if (GUILayout.Button("Kill boss")) { if (floors.Boss != null) floors.Boss.Health.Kill(); else Say("No boss yet: walk in first."); }
                GUILayout.EndHorizontal();

                GUILayout.Space(6);
                God = GUILayout.Toggle(God, " God mode (no damage)");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Full heal")) { var p = PlayerController.Instance; if (p != null) p.Health.Heal(99); }
                if (GUILayout.Button("+1 max health")) { var p = PlayerController.Instance; if (p != null) p.Health.SetMaxHealth(p.Health.maxHealth + 1, true); }
                if (GUILayout.Button("+100 coins")) RunState.AddCoins(100);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Random item")) GiveItem(false);
                if (GUILayout.Button("Every item")) GiveItem(true);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Game speed x{_speed:0.##}", GUILayout.Width(150));
                foreach (float s in new[] { 0.5f, 1f, 2f, 4f })
                {
                    if (GUILayout.Button("x" + s)) { _speed = s; Time.timeScale = s; }
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Not in a run. Start one, and this menu opens again inside it to pick a world and floor.");
                if (GUILayout.Button("Start a run")) { StartRunAt(null, 1); Open = true; }
            }

            GUILayout.Space(6);
            GUILayout.Label("Jump to: pick a floor, then a world");
            _stage = GUILayout.SelectionGrid(_stage, StageNames, 3);
            var worlds = inRun ? Worlds(floors) : new List<BiomeDefinition>();
            for (int i = 0; i < worlds.Count; i += 2)
            {
                GUILayout.BeginHorizontal();
                for (int j = i; j < Mathf.Min(i + 2, worlds.Count); j++)
                {
                    var world = worlds[j];
                    if (!GUILayout.Button(world.displayName)) continue;
                    JumpTo(world.id, _stage + 1);
                    Open = false;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            GUILayout.Label("Save file");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock everything"))
            {
                Playtest.UnlockEverything();
                Say("Everything unlocked. Erase the save to undo it.");
            }
            if (GUILayout.Button("+500 samples")) { Playtest.AddSamples(500); Say("500 samples added."); }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Back to the lab")) { Open = false; Time.timeScale = 1f; SceneManager.LoadScene("Lab"); }
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
