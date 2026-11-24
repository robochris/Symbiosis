using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Shrunk
{
    /// <summary>
    /// The voice on the lab radio: Dr. Finch, who went in first and talks the player through every run. Text in a small
    /// panel under the health, typed out a letter at a time, so it works with the sound off. It builds its own canvas,
    /// so a scene only needs this one component on its Game object. What it says is in RadioLines.
    /// </summary>
    public class Radio : MonoBehaviour
    {
        public static Radio Instance { get; private set; }
        /// <summary>Tests set this to make chatter certain (1) or impossible (0) instead of a roll.</summary>
        public static float? ChanceOverride;

        [Tooltip("Seconds between two bits of chatter, so the voice never talks over itself or all the time.")]
        public float chatterGap = 25f;
        public float lettersPerSecond = 55f;

        /// <summary>What was last said, for tests.</summary>
        public string LastLine { get; private set; } = "";
        public RadioMoment LastMoment { get; private set; }
        public int SaidCount { get; private set; }
        public bool Showing => _group != null && _group.alpha > 0.01f;

        // Something that happened as the last scene ended (a death, an extraction), to be remarked on in the lab.
        static RadioMoment? _carried;

        CanvasGroup _group;
        Text _text;
        string _line = "";
        float _saidAt = -999f, _hideAt;
        System.Random _rng;
        PlayerController _watched;
        BossEnemy _announced;
        bool _firstFloor = true;
        bool _finchDown;
        Coroutine _waiting;

        /// <summary>Forgets anything left over from a scene that ended. For tests.</summary>
        public static void Forget() => _carried = null;

        void Awake()
        {
            Instance = this;
            _rng = new System.Random();
            Build();
        }

        void OnDestroy()
        {
            // Leaving the bottom with Finch beaten: that, not the samples, is what gets remarked on at home.
            if (_finchDown && _carried != RadioMoment.Death) _carried = RadioMoment.FinchHome;
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            FloorManager.FloorBuilt += OnFloorBuilt;
            FloorManager.RoomChanged += OnRoomChanged;
            FloorManager.DareRung += OnDare;
            Enemy.Killed += OnKilled;
            Pickup.ItemCollected += OnItem;
            RunDirector.Extracted += OnExtracted;
            FinchNotes.Found += OnNote;
            CatBoss.Bored += OnWash;
        }

        void OnDisable()
        {
            FloorManager.FloorBuilt -= OnFloorBuilt;
            FloorManager.RoomChanged -= OnRoomChanged;
            FloorManager.DareRung -= OnDare;
            Enemy.Killed -= OnKilled;
            Pickup.ItemCollected -= OnItem;
            RunDirector.Extracted -= OnExtracted;
            FinchNotes.Found -= OnNote;
            CatBoss.Bored -= OnWash;
            if (_watched != null)
            {
                _watched.Health.Damaged -= OnHit;
                _watched.Health.Died -= OnDied;
            }
        }

        void Start()
        {
            // In the lab (no floors): remark on how the last run ended, or say hello.
            if (FloorManager.Instance != null) return;
            if (RunDirector.CycleRadio)
            {
                RunDirector.CycleRadio = false;
                // Each restart shakes one more thing out of him, in order, once. After that, the usual remarks.
                Say(RadioMoment.Cycle, RadioLines.RestartKey(MachineParts.Cycle), 1.2f);
            }
            else if (_carried.HasValue) Say(_carried.Value, null, 1.2f);
            else if (SaveSystem.Data.meta.runsStarted == 0) Say(RadioMoment.Hello, null, 0.8f);
            else if (SaveSystem.Data.meta.finchBeaten > 0) Say(RadioMoment.After, null, 0.8f);
            else if (LabGrowth.Met(LabGrowth.Needs.AllNotesFound)) Say(RadioMoment.Door, null, 0.8f);
            else if (!SaveSystem.Data.run.active) Say(RadioMoment.Lab, null, 0.8f);
            _carried = null;
        }

        void Build()
        {
            var canvasObject = new GameObject("Radio", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40; // under the full-screen cards
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Left side, under the health, the counts and the item strip, and narrow enough to stay clear of the summary.
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(canvasObject.transform, false);
            rect.anchorMin = new Vector2(0.02f, 0.445f);
            rect.anchorMax = new Vector2(0.262f, 0.68f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = panel.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.6f);
            image.raycastTarget = false;
            _group = panel.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text name = MakeText(rect, font, 26, FontStyle.Bold, new Vector2(0f, 0.8f), new Vector2(1f, 1f), TextAnchor.MiddleLeft);
            name.text = RadioLines.Speaker + "  ·  radio";
            name.color = new Color(0.55f, 0.9f, 1f);
            _text = MakeText(rect, font, 28, FontStyle.Normal, new Vector2(0f, 0f), new Vector2(1f, 0.82f), TextAnchor.UpperLeft);
        }

        static Text MakeText(RectTransform parent, Font font, int size, FontStyle style, Vector2 anchorMin, Vector2 anchorMax, TextAnchor anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(18f, 8f);
            rect.offsetMax = new Vector2(-18f, -6f);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        // ---------- saying things ----------

        /// <summary>
        /// Says something about a moment, unless the player has turned the radio down. Chatter is only said now and
        /// then and never over another line; the rest is always said, after the delay, replacing whatever is up.
        /// Returns whether it will be said.
        /// </summary>
        public bool Say(RadioMoment moment, string about = null, float delay = 0f, float chance = 1f)
        {
            int level = GameSettings.Radio;
            if (level == 0) return false;

            if (RadioLines.IsChatter(moment))
            {
                if (level < 2) return false;
                if (Showing || _waiting != null || Time.unscaledTime - _saidAt < chatterGap) return false;
                float roll = ChanceOverride ?? chance;
                if (_rng.NextDouble() >= roll) return false;
            }

            if (_waiting != null) StopCoroutine(_waiting);
            // Headless runs do not wait: the delays are only there to keep clear of the cards.
            if (delay <= 0f || Application.isBatchMode)
            {
                _waiting = null;
                Show(moment, about);
            }
            else
            {
                _waiting = StartCoroutine(After(moment, about, delay));
            }
            return true;
        }

        IEnumerator After(RadioMoment moment, string about, float delay)
        {
            for (float t = 0f; t < delay || ScreenCards.Blocking; t += Time.unscaledDeltaTime) yield return null;
            _waiting = null;
            Show(moment, about);
        }

        void Show(RadioMoment moment, string about)
        {
            _line = RadioLines.Pick(moment, about, _rng);
            LastLine = _line;
            LastMoment = moment;
            SaidCount++;
            _saidAt = Time.unscaledTime;
            // Long enough to type it out and then read it.
            _hideAt = _saidAt + _line.Length / lettersPerSecond + 2.6f + _line.Length * 0.035f;
            _text.text = "";
            _group.alpha = 1f;
            // The lab has no save point of its own, so what was heard there is written down now.
            if (FloorManager.Instance == null) SaveSystem.Save();
        }

        void Update()
        {
            if (Showing)
            {
                float since = Time.unscaledTime - _saidAt;
                int letters = Mathf.Min(_line.Length, Mathf.CeilToInt(since * lettersPerSecond));
                if (_text.text.Length != letters) _text.text = _line.Substring(0, letters);
                float left = _hideAt - Time.unscaledTime;
                _group.alpha = Mathf.Clamp01(left / 0.4f);
            }

            var player = PlayerController.Instance;
            if (player == _watched) return;
            if (_watched != null)
            {
                _watched.Health.Damaged -= OnHit;
                _watched.Health.Died -= OnDied;
            }
            _watched = player;
            if (_watched != null)
            {
                _watched.Health.Damaged += OnHit;
                _watched.Health.Died += OnDied;
            }
        }

        // ---------- what it notices ----------

        void OnFloorBuilt(FloorManager floors)
        {
            _announced = null;
            bool first = _firstFloor;
            _firstFloor = false;

            if (floors.biome != null && floors.biome.isBottom)
            {
                Say(floors.biome.id == "machine" ? RadioMoment.MachineRoom : RadioMoment.Bottom, null, 3.2f);
            }
            else if (first)
            {
                // The scene has just opened: a new run, or a saved one picked up. After the arrival card.
                bool fresh = floors.FloorNumber == 1 && floors.depth == TimeClock.StartDepth;
                Say(fresh ? RadioMoment.RunStart : RadioMoment.Resume, null, 3.2f);
            }
            else if (floors.Twist != FloorTwist.None)
            {
                Say(RadioMoment.Twist, null, 2.4f);
            }
            else if (floors.FloorInBiome == 1)
            {
                Say(RadioMoment.Biome, floors.biome != null ? floors.biome.id : null, 3.2f);
            }
            else if (floors.depth >= 2)
            {
                Say(RadioMoment.Deep, null, 2.4f, 0.6f);
            }
            else
            {
                Say(RadioMoment.Floor, null, 2.4f, 0.4f);
            }
        }

        void OnRoomChanged(FloorManager floors)
        {
            if (floors.Boss == null || floors.Boss == _announced || floors.Boss.Health.IsDead) return;
            _announced = floors.Boss;
            // After its title card.
            if (floors.Boss is CatBoss) Say(RadioMoment.FinchFight, null, 2.6f);
            else Say(RadioMoment.Boss, floors.Boss.displayName, 2.6f);
        }

        void OnKilled(Enemy enemy)
        {
            if (enemy is CatBoss)
            {
                Say(RadioMoment.FinchDown, null, 1f);
                _finchDown = true;
            }
            else if (enemy is BossEnemy) Say(RadioMoment.BossDown, null, 1f);
        }

        void OnWash(CatBoss cat) => Say(RadioMoment.FinchDose);

        void OnDare(FloorManager floors) => Say(RadioMoment.Dare, null, 0.6f);

        void OnItem(ItemDefinition item)
        {
            // The dose is Finch's own: that always gets a word, a moment after whatever was being said.
            if (item != null && item.type == ItemType.FinchsDose) Say(RadioMoment.Dose, null, 4f);
            else Say(RadioMoment.Item, null, 0f, 0.35f);
        }

        void OnHit(int amount, Vector3 direction)
        {
            if (amount <= 0 || _watched == null) return;
            Health health = _watched.Health;
            if (health.Current == 1 && health.maxHealth > 1) Say(RadioMoment.LowHealth);
        }

        void OnNote(FinchNote note) => Say(RadioMoment.Note, null, 1.5f);

        void OnDied() => _carried = RadioMoment.Death;

        void OnExtracted(int samples) => _carried = RadioMoment.Extract;
    }
}
