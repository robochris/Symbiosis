using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Kills in quick succession chain into a combo, counted over the player's head from the third one on. Every
    /// fifth link drops coins at the player's feet, more the longer the chain. The best chain is kept and shown on
    /// the run's card. A chain breaks when a few seconds pass with no kill.
    /// </summary>
    public class KillCombo : MonoBehaviour
    {
        /// <summary>Seconds a chain waits for the next kill.</summary>
        public const float Window = 2.6f;
        /// <summary>A chain shows from this many kills.</summary>
        public const int ShowsFrom = 3;
        /// <summary>Coins drop every this many kills in a chain.</summary>
        public const int Milestone = 5;
        /// <summary>Tests switch it off, so their coin counts stay their own.</summary>
        public static bool Off;

        public static int Chain { get; private set; }
        /// <summary>The longest chain this run.</summary>
        public static int RunBest { get; private set; }

        static KillCombo _instance;
        float _lastKill = -99f, _pop;
        TextMesh _text;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (_instance != null) return;
            var go = new GameObject("Kill combo");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<KillCombo>();
        }

        /// <summary>A new run: the chain and the run's best start again.</summary>
        public static void StartRun()
        {
            Chain = 0;
            RunBest = 0;
        }

        void OnEnable() => Enemy.Killed += OnKilled;
        void OnDisable() => Enemy.Killed -= OnKilled;

        void OnKilled(Enemy enemy)
        {
            if (enemy != null) Register();
        }

        /// <summary>One more kill for the chain. Called for every enemy killed; tests call it directly.</summary>
        public static void Register()
        {
            if (!Off && _instance != null) _instance.Count();
        }

        void Count()
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            Chain = Time.time - _lastKill <= Window ? Chain + 1 : 1;
            _lastKill = Time.time;
            if (Chain > RunBest) RunBest = Chain;
            if (Chain < ShowsFrom) return;

            _pop = 1f;
            bool milestone = Chain % Milestone == 0;
            int coins = milestone ? Chain / Milestone : 0;
            if (coins > 0)
            {
                Vector2 scatter = Random.insideUnitCircle;
                Pickup.Coin(player.transform.position + new Vector3(scatter.x, 0f, scatter.y), coins);
                Sfx.Play(SfxId.Item, 0.5f);
            }
            Show(player, milestone ? $"x{Chain} COMBO!\n+{coins} coin{(coins == 1 ? "" : "s")}" : $"x{Chain}");
            var meta = SaveSystem.Data.meta;
            if (Chain > meta.bestCombo)
            {
                meta.bestCombo = Chain;
                // Written with the next save: the floor's, or the run's end.
            }
        }

        void Show(PlayerController player, string text)
        {
            if (_text == null)
            {
                var go = new GameObject("Combo");
                go.transform.SetParent(transform, false);
                _text = go.AddComponent<TextMesh>();
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _text.font = font;
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                _text.anchor = TextAnchor.LowerCenter;
                _text.alignment = TextAlignment.Center;
                _text.fontSize = 64;
                _text.fontStyle = FontStyle.Bold;
                go.AddComponent<FaceCamera>();
            }
            _text.text = text;
            _text.gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            if (_text == null || !_text.gameObject.activeSelf) return;
            var player = PlayerController.Instance;
            float left = Window - (Time.time - _lastKill);
            if (player == null || left <= 0f || Chain < ShowsFrom)
            {
                _text.gameObject.SetActive(false);
                return;
            }
            _pop = Mathf.MoveTowards(_pop, 0f, Time.deltaTime * 4f);
            _text.transform.position = player.transform.position + Vector3.up * 2.4f;
            // Bigger for a longer chain, a pop on each kill, and fading as the window runs out.
            float size = 0.05f + Mathf.Min(Chain, 20) * 0.002f;
            _text.characterSize = size * (1f + _pop * 0.35f);
            Color colour = Chain >= 10 ? new Color(1f, 0.55f, 0.15f) : Chain >= Milestone ? new Color(1f, 0.85f, 0.2f) : Color.white;
            colour.a = Mathf.Clamp01(left / (Window * 0.4f));
            _text.color = colour;
        }
    }
}
