using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Pressure: an optional dial for players who have beaten a world. Each level makes enemies tougher and a little
    /// quicker, and makes samples worth more when they come home. Level 1 opens after the first boss; each further
    /// level opens once a world's boss has been beaten at the level before it. Set at the gauge in the lab. Daily
    /// runs, the Gauntlet and the trips to the bottom ignore it.
    /// </summary>
    public static class Pressure
    {
        public const int Max = 5;
        public const float HealthPerLevel = 0.2f, SpeedPerLevel = 0.04f, RewardPerLevel = 0.25f;

        /// <summary>The level in force on this run (0 in the lab, or with none chosen).</summary>
        public static int Active { get; private set; }

        /// <summary>The level chosen at the gauge, if it is open.</summary>
        public static int Chosen => Mathf.Clamp(SaveSystem.Data.meta.pressure, 0, Unlocked);

        /// <summary>The highest level that can be chosen.</summary>
        public static int Unlocked
        {
            get
            {
                if (Achievements.EverythingUnlocked) return Max;
                MetaSave meta = SaveSystem.Data.meta;
                if (meta.bossesBeaten <= 0) return 0;
                return Mathf.Clamp(meta.pressureBest + 1, 1, Max);
            }
        }

        public static float RewardScale => 1f + RewardPerLevel * Active;

        /// <summary>Puts a level in force (and the enemies' scaling with it).</summary>
        public static void Apply(int level)
        {
            Active = Mathf.Clamp(level, 0, Max);
            Enemy.HealthScale = 1f + HealthPerLevel * Active;
            Enemy.PressureRush = 1f + SpeedPerLevel * Active;
        }

        /// <summary>Steps the gauge to the next open level, round to 0, and remembers it.</summary>
        public static int Next()
        {
            int next = Chosen + 1;
            if (next > Unlocked) next = 0;
            SaveSystem.Data.meta.pressure = next;
            SaveSystem.Save();
            return next;
        }

        /// <summary>A world's boss beaten on a run at this level: the next level opens.</summary>
        public static void BossBeaten()
        {
            MetaSave meta = SaveSystem.Data.meta;
            if (Active > meta.pressureBest)
            {
                meta.pressureBest = Active;
                SaveSystem.Save();
            }
        }

        public static string Describe(int level) => level <= 0
            ? "no pressure: the run as it comes"
            : $"pressure {level}: enemies {Mathf.RoundToInt(HealthPerLevel * level * 100)}% tougher, samples worth ×{1f + RewardPerLevel * level:0.##}";
    }

    /// <summary>The pressure gauge in the lab: use it to turn the dial.</summary>
    public class PressureGauge : MonoBehaviour
    {
        readonly StepPad _pad = new StepPad { radius = 1f };
        Transform _needle;
        public static PressureGauge Current { get; private set; }

        public static PressureGauge Place(Vector3 position)
        {
            var go = new GameObject("Pressure Gauge");
            go.transform.position = position;
            var brass = new Color(0.8f, 0.62f, 0.25f);
            Part.Make(PrimitiveType.Cylinder, "Pipe", go.transform, new Vector3(0f, 0.7f, 0f), new Vector3(0.18f, 0.7f, 0.18f), new Color(0.4f, 0.42f, 0.45f));
            var face = Part.Make(PrimitiveType.Cylinder, "Face", go.transform, new Vector3(0f, 1.65f, 0f), new Vector3(0.9f, 0.06f, 0.9f), new Color(0.95f, 0.93f, 0.85f));
            face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var rim = Part.Make(PrimitiveType.Cylinder, "Rim", go.transform, new Vector3(0f, 1.65f, 0.03f), new Vector3(1f, 0.05f, 1f), brass);
            rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            // A red band at the top of the dial.
            var red = Part.Make(PrimitiveType.Cube, "Red", go.transform, new Vector3(0.22f, 1.88f, -0.04f), new Vector3(0.25f, 0.06f, 0.02f), new Color(0.9f, 0.15f, 0.15f));
            red.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            var pivot = new GameObject("Needle").transform;
            pivot.SetParent(go.transform, false);
            pivot.localPosition = new Vector3(0f, 1.65f, -0.05f);
            Part.Make(PrimitiveType.Cube, "Hand", pivot, new Vector3(0f, 0.18f, 0f), new Vector3(0.04f, 0.36f, 0.02f), new Color(0.1f, 0.1f, 0.1f));
            Part.Make(PrimitiveType.Cylinder, "Mat", go.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.6f, 0.02f, 1.6f), new Color(1f, 0.45f, 0.3f));
            var gauge = go.AddComponent<PressureGauge>();
            gauge._needle = pivot;
            UseMarkers.Rescan();
            return gauge;
        }

        void OnEnable()
        {
            Current = this;
            WorldPrompt.Add(Prompt, transform);
        }

        void OnDisable()
        {
            if (Current == this) Current = null;
            WorldPrompt.Remove(Prompt);
        }

        void Update()
        {
            // The needle swings from far left (none) to far right (the most).
            float target = Mathf.Lerp(70f, -70f, Pressure.Chosen / (float)Pressure.Max);
            if (_needle != null) _needle.localRotation = Quaternion.Slerp(_needle.localRotation, Quaternion.Euler(0f, 0f, target), Time.deltaTime * 6f);
            var player = PlayerController.Instance;
            if (player == null || Pressure.Unlocked == 0 || !_pad.SteppedOn(transform.position, player.transform.position)) return;
            int level = Pressure.Next();
            Sfx.Play(SfxId.PowerOn, 0.5f);
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast("Next run: " + Pressure.Describe(level) + ".", 3.5f);
        }

        string Prompt(Vector3 playerPosition)
        {
            if (_pad.Distance(transform.position, playerPosition) > 2f) return null;
            if (Pressure.Unlocked == 0) return "A pressure gauge, at nothing. Beat a boss and it will start to mean something.";
            string more = Pressure.Unlocked < Pressure.Max ? $" Beat a world's boss at pressure {Pressure.Unlocked} to open the next." : "";
            return $"The pressure gauge: next run at {Pressure.Describe(Pressure.Chosen)}.{more}\n{Interact.Verb} to turn the dial.";
        }
    }

    /// <summary>
    /// A heartbeat of stillness when something big dies: the game all but stops for a moment, then carries on. An elite
    /// gets a short one; a boss a longer one.
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        public const float EliteSeconds = 0.06f, BossSeconds = 0.18f;
        public static bool Off;
        static HitStop _instance;
        float _until, _restore, _slow = 0.05f;
        bool _holding;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (_instance != null) return;
            var go = new GameObject("Hit stop");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<HitStop>();
        }

        void OnEnable() => Enemy.Killed += OnKilled;
        void OnDisable() => Enemy.Killed -= OnKilled;

        void OnKilled(Enemy enemy)
        {
            if (Off || enemy == null || Time.timeScale <= 0f) return;
            // Bosses already get the game's own slow motion; this is just the elite's freeze.
            float seconds = enemy is BossEnemy ? 0f : enemy.IsElite ? EliteSeconds : 0f;
            if (seconds <= 0f) return;
            if (!_holding) _restore = Time.timeScale;
            _holding = true;
            bool boss = enemy is BossEnemy;
            // A boss: a long slow-motion moment on the last hit. An elite: a short freeze.
            _until = Time.unscaledTime + (boss ? 0.9f : seconds);
            _slow = boss ? 0.25f : 0.05f;
            Time.timeScale = _slow;
        }

        /// <summary>True while the freeze holds: anything pausing meanwhile should come back to full speed.</summary>
        public static bool Holding => _instance != null && _instance._holding;

        void Update()
        {
            if (!_holding || Time.unscaledTime < _until) return;
            _holding = false;
            // Only put it back if nothing else (a pause, a card) has taken the clock meanwhile.
            if (Mathf.Approximately(Time.timeScale, _slow)) Time.timeScale = _restore;
        }
    }
}
