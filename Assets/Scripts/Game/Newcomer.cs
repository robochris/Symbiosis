using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Shrunk
{
    /// <summary>
    /// The very first run: Finch's safety net catches a hit that would have ended it, three times, so a first run
    /// gets somewhere. Tests switch it off.
    /// </summary>
    public static class SafetyNet
    {
        public const int Catches = 3;
        public static bool Off;
        public static int Used { get; private set; }

        public static void StartRun() => Used = 0;

        /// <summary>True (and used up) if this lethal hit is caught.</summary>
        public static bool Catch()
        {
            if (Off || SaveSystem.Data.meta.runsStarted > 1 || Used >= Catches || RunDirector.Instance == null) return false;
            Used++;
            int left = Catches - Used;
            var hud = Object.FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast(left > 0 ? $"Finch's safety net caught you. {left} more on this first run." : "Finch's safety net caught you. That was the last one.", 3.5f);
            return true;
        }
    }

    /// <summary>
    /// Getting started: five easy goals for a new player, each paying samples the moment it is done, listed on a
    /// board by the lab door until all five are ticked.
    /// </summary>
    public class StarterGoals : MonoBehaviour
    {
        public enum Goal { Item, MiniBoss, Shop, Home, Upgrade }
        public const int Pay = 30;
        public static bool Off;

        static readonly string[] Names = { "Pick up an item", "Beat a boss", "Find a shop", "Get home with samples", "Buy a lab upgrade" };
        static StarterGoals _instance;
        float _next;
        TextMesh _board;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (_instance != null) return;
            var go = new GameObject("Starter goals");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<StarterGoals>();
        }

        void OnEnable()
        {
            Upgrades.Purchased += OnPurchased;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            Upgrades.Purchased -= OnPurchased;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnPurchased(UpgradeDefinition upgrade) => Tick(Goal.Upgrade);

        public static bool Done(Goal goal) => (SaveSystem.Data.meta.starterGoals & (1 << (int)goal)) != 0;
        public static bool AllDone
        {
            get
            {
                for (int i = 0; i < Names.Length; i++) if (!Done((Goal)i)) return false;
                return true;
            }
        }

        /// <summary>Marks a goal done and pays for it, once.</summary>
        public static void Tick(Goal goal)
        {
            if (Off || Done(goal)) return;
            MetaSave meta = SaveSystem.Data.meta;
            meta.starterGoals |= 1 << (int)goal;
            meta.samples += Pay;
            SaveSystem.Save();
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast($"Goal done: {Names[(int)goal]}. +{Pay} samples to the lab." + (AllDone ? " That is all of them: you know your way around now." : ""), 4f);
            Sfx.Play(SfxId.Item, 0.5f);
            if (_instance != null) _instance.Write();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _board = null;
            // In the lab, a board by the door with the list, until everything on it is done.
            var player = PlayerController.Instance;
            if (Off || AllDone || player == null || FloorManager.Instance != null) return;
            var go = new GameObject("First Steps Board");
            go.transform.position = player.transform.position + new Vector3(4.6f, 0f, -2.2f);
            Part.Make(PrimitiveType.Cube, "Board", go.transform, new Vector3(0f, 1.6f, 0f), new Vector3(2.4f, 1.6f, 0.08f), new Color(0.35f, 0.25f, 0.15f));
            Part.Make(PrimitiveType.Cube, "Leg L", go.transform, new Vector3(-1f, 0.4f, 0f), new Vector3(0.1f, 0.8f, 0.1f), new Color(0.3f, 0.2f, 0.12f));
            Part.Make(PrimitiveType.Cube, "Leg R", go.transform, new Vector3(1f, 0.4f, 0f), new Vector3(0.1f, 0.8f, 0.1f), new Color(0.3f, 0.2f, 0.12f));
            var text = new GameObject("List");
            text.transform.SetParent(go.transform, false);
            text.transform.localPosition = new Vector3(-1.05f, 2.25f, -0.06f);
            text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            _board = text.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _board.font = font;
            text.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            _board.fontSize = 48;
            _board.characterSize = 0.035f;
            _board.anchor = TextAnchor.UpperLeft;
            _board.color = Color.white;
            // Turned to face the way the player looks when they come in.
            go.transform.rotation = Quaternion.LookRotation(player.transform.position - go.transform.position);
            text.transform.localRotation = Quaternion.identity;
            text.transform.localPosition = new Vector3(1.05f, 2.25f, -0.06f);
            text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Write();
        }

        void Write()
        {
            if (_board == null) return;
            string text = "FIRST STEPS  (+" + Pay + " samples each)\n";
            for (int i = 0; i < Names.Length; i++) text += (Done((Goal)i) ? "[x] " : "[  ] ") + Names[i] + "\n";
            _board.text = text;
        }

        void Update()
        {
            if (Off || Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            MetaSave meta = SaveSystem.Data.meta;
            var player = PlayerController.Instance;
            var floors = FloorManager.Instance;
            if (player != null && player.Items != null && player.Items.Collected.Count > 0 && floors != null) Tick(Goal.Item);
            if (meta.bossesBeaten > 0) Tick(Goal.MiniBoss);
            if (floors != null && floors.CurrentNode != null && floors.CurrentNode.type == RoomType.Shop) Tick(Goal.Shop);
            if (meta.extractions > 0) Tick(Goal.Home);
        }
    }

    /// <summary>
    /// Feel: a close call (a shot passing right by during a dodge) gives a burst of speed and a little of the power
    /// meter; and at the last point of health the screen's edges pulse red with a heartbeat.
    /// </summary>
    public class CloseCalls : MonoBehaviour
    {
        public const float Range = 1.3f, BoostSeconds = 1.2f, BoostSpeed = 1.3f, PowerGift = 0.1f;
        /// <summary>Until when the player is boosted (the controller reads it).</summary>
        public static float BoostUntil;
        public static int Count { get; private set; }

        static CloseCalls _instance;
        bool _counted;
        Image _edge;
        float _beat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (_instance != null) return;
            var go = new GameObject("Close calls");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<CloseCalls>();
            _instance.MakeEdge();
        }

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null) { ShowEdge(0f); return; }

            if (!player.IsDodging) _counted = false;
            else if (!_counted && ShotNear(player.transform.position, out var near)) { Reward(player); Parry(near, player); }

            // The last point of health: a heartbeat at the edges.
            Health health = player.Health;
            bool low = health != null && !health.IsDead && health.Current == 1 && health.maxHealth > 1 && FloorManager.Instance != null;
            _beat += Time.unscaledDeltaTime * 1.4f;
            float pulse = Mathf.Pow(Mathf.Abs(Mathf.Sin(_beat * Mathf.PI)), 6f);
            ShowEdge(low ? 0.25f + 0.35f * pulse : 0f);
        }

        static bool ShotNear(Vector3 at, out Bullet near)
        {
            near = null;
            foreach (var bullet in Bullet.Live)
            {
                if (bullet == null || bullet.Team != Team.Enemy) continue;
                Vector3 to = bullet.transform.position - at;
                to.y = 0f;
                if (to.magnitude <= Range) { near = bullet; return true; }
            }
            return false;
        }

        public static int Parries { get; private set; }

        // A shot right on top of the dodge is sent straight back.
        static void Parry(Bullet bullet, PlayerController player)
        {
            if (bullet == null) return;
            Vector3 to = bullet.transform.position - player.transform.position;
            to.y = 0f;
            if (to.magnitude > Range * 0.75f) return;
            bullet.Reflect();
            Parries++;
            Sfx.Play(SfxId.Blocked, 0.8f);
            Fx.Burst(bullet.transform.position, new Color(0.7f, 1f, 1f), 14, 6f, 0.1f, 0.3f);
        }

        void Reward(PlayerController player)
        {
            _counted = true;
            Count++;
            BoostUntil = Time.time + BoostSeconds;
            if (player.Power != null) player.Power.SetMeter(Mathf.Min(1f, player.Power.Meter + PowerGift));
            Sfx.Play(SfxId.Dodge, 1f);
            Fx.Burst(player.transform.position + Vector3.up * 0.6f, new Color(0.6f, 0.95f, 1f), 12, 5f, 0.1f, 0.3f);
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null && Count <= 3) hud.Toast("Close one! A burst of speed, and a little power.", 2f);
        }

        void MakeEdge()
        {
            var canvasObject = new GameObject("Low health edge");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -5;
            var image = new GameObject("Edge").AddComponent<Image>();
            image.transform.SetParent(canvasObject.transform, false);
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            // A soft red ring: clear in the middle, red toward the edges.
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 0.55f) / 0.6f);
                    texture.SetPixel(x, y, new Color(0.9f, 0.05f, 0.05f, d * d));
                }
            texture.Apply();
            image.sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            _edge = image;
            ShowEdge(0f);
        }

        void ShowEdge(float alpha)
        {
            if (_edge == null) return;
            _edge.enabled = alpha > 0.01f;
            _edge.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
