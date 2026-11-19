using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Shrunk
{
    /// <summary>
    /// Everything that is shown over the whole screen for a moment: the title over the lab when the game opens, the
    /// card between floors, the bigger card on arriving in a biome, a boss's title card, and the summary when a biome
    /// is finished. It builds its own canvas, so a scene only needs this one component on its Game object.
    /// </summary>
    public class ScreenCards : MonoBehaviour
    {
        public static ScreenCards Instance { get; private set; }
        /// <summary>True while the title is up: real input is ignored until the player starts.</summary>
        public static bool Blocking => Instance != null && Instance.TitleShowing;
        /// <summary>The title is shown once each time the game is opened, not on every return to the lab.</summary>
        public static bool TitleSeen;
        /// <summary>Headless test runs skip the title and the boss freeze, unless a test asks for them.</summary>
        public static bool ShowInTests;

        [Tooltip("On in the lab: the game opens here, with the title over it.")]
        public bool title;
        public string gameName = "SHRUNK";
        public string tagline = "Small problem. Big everything else.";
        [Tooltip("Seconds the world stands still for a boss's title card.")]
        public float bossFreeze = 1.7f;

        public bool TitleShowing { get; private set; }
        /// <summary>What the cards last said, for tests.</summary>
        public string LastHeading { get; private set; } = "";
        public string LastLine { get; private set; } = "";
        public string LastSummary { get; private set; } = "";
        public bool CardShowing => _card != null && _card.alpha > 0.01f;
        public bool SummaryShowing => _summary != null && _summary.gameObject.activeSelf;

        /// <summary>One line under "Floor N". Half of them are true.</summary>
        public static readonly string[] FloorLines =
        {
            "Tip: the ants were here first. They would like you to know that.",
            "Tip: shoot the ones that are shooting you. This one is free.",
            "Tip: a shell stops shots from the front. Ants have a back, too.",
            "Tip: the dodge has a moment where nothing can hurt you. Only a moment.",
            "Tip: red strip on the floor means stand somewhere else.",
            "Tip: larva pods pop for coins. The swelling ones pop for other reasons.",
            "Tip: crowned ants hit harder, last longer and pay better.",
            "Tip: samples only count if you get them home.",
            "Tip: the treasure room is free. Nothing else down here is.",
            "Tip: the pink crumbs are food. The shop sells one; bosses drop one.",
            "Tip: a bombardier throws over walls, at where you are going. Stop going there.",
            "Tip: the small pink ant makes the others faster. It is the one to shoot.",
            "Reminder: you are roughly the size of a sprinkle.",
            "Reminder: the lab is insured. You are not.",
            "Fact: an ant can carry fifty times its own weight. You weigh almost nothing.",
            "Fact: this colony has no word for 'guest'.",
            "Fact: nobody has ever asked the ants how they feel about all this.",
            "It gets deeper. It does not get friendlier.",
            "Further down. Mind your head, wherever it is.",
        };

        /// <summary>The cake's own lines, used instead of the ants' while the run is there.</summary>
        public static readonly string[] CakeLines =
        {
            "Tip: the red disc under you is about to be something. Leave.",
            "Tip: a soda bottle throws at where you are going. Go somewhere else instead.",
            "Tip: the party horn hurts nobody. Shoot it first anyway.",
            "Tip: hot wax stays where it lands. So should you not.",
            "Tip: gumdrops grow up. Deal with them while they are small.",
            "Tip: sugar rush means it gets faster when it is losing.",
            "Reminder: it is somebody's birthday. They are not having as bad a day as you.",
            "Reminder: everything here is food. You are in it.",
            "Fact: a cake this size has its own weather.",
            "Fact: the candles were lit eleven minutes ago. You have been here longer than that.",
            "Nobody has cut the cake yet. Try to be somewhere else when they do.",
            "Further in. It gets sweeter. That is not a good thing.",
        };

        /// <summary>The sofa's own lines.</summary>
        public static readonly string[] SofaLines =
        {
            "Tip: sparks round an enemy mean it is about to go off. Count to four.",
            "Tip: the remote turns everything up. Turn it off first.",
            "Tip: popcorn lands where you were heading. Change your mind.",
            "Tip: a marble only goes in straight lines. Stand somewhere bent.",
            "Tip: when the vacuum's lid is up, everything you do to it counts twice.",
            "Tip: sticky lint slows walking. It does not slow a dodge.",
            "Reminder: everything anybody ever lost is down here. So are you.",
            "Reminder: that coin is worth more than your whole expedition. Leave it.",
            "Fact: nobody has moved this sofa since it arrived.",
            "Fact: the dust is nine tenths other people.",
            "It is darker further in. The torch somebody dropped is still on.",
            "Mind the springs. They are older than you and less forgiving.",
        };

        /// <summary>The fridge's own lines.</summary>
        public static readonly string[] FridgeLines =
        {
            "Tip: frost on the floor slows walking. A dodge goes straight over it.",
            "Tip: a line on the floor in front of the machine is where the row comes up.",
            "Tip: nothing gets into the tub while the lid is on. Wait for the lid.",
            "Tip: the box of baking soda keeps the others fresh. Open it first.",
            "Tip: a yogurt pot throws at where you will be. Be late.",
            "Tip: frozen peas only go straight. So do you, if you are not careful.",
            "Reminder: the light is only on because you are in here.",
            "Reminder: nothing in this fridge is as old as the thing at the back.",
            "Fact: the use-by date is a suggestion. The thing in the tub took it as a dare.",
            "Fact: somebody opens the door every hour, looks, and shuts it again.",
            "It gets colder further back. So does everything else.",
            "Mind the shelf edge. It is a long way down to the salad drawer.",
        };

        /// <summary>The line under "Floor N" for a biome: its own if it has any, otherwise the ants'. The floor's seed picks it.</summary>
        public static string TipFor(string biomeId, int seed)
        {
            string[] lines = biomeId == "cake" ? CakeLines : biomeId == "sofa" ? SofaLines : biomeId == "fridge" ? FridgeLines : FloorLines;
            return lines[Mathf.Abs(seed) % lines.Length];
        }

        CanvasGroup _card, _titleGroup;
        Text _heading, _line, _titleText, _titleHint, _summaryText;
        RectTransform _summary;
        Font _font;
        Coroutine _showing;
        float _summaryUntil;
        bool _summaryShown;

        // What the summary counts, for the biome being played.
        float _biomeStarted;
        int _killsAtStart, _samplesAtStart, _itemsAtStart, _hits, _lastFloor;
        BossEnemy _announced;
        PlayerController _watched;
        float _orbit;

        static bool Quiet => Application.isBatchMode && !ShowInTests;

        void Awake()
        {
            Instance = this;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
        }

        void OnEnable()
        {
            FloorManager.FloorBuilt += OnFloorBuilt;
            FloorManager.RoomChanged += OnRoomChanged;
        }

        void OnDisable()
        {
            FloorManager.FloorBuilt -= OnFloorBuilt;
            FloorManager.RoomChanged -= OnRoomChanged;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_frozen) Time.timeScale = _timeBefore;
        }

        void Start()
        {
            if (title && !TitleSeen && !Quiet) ShowTitle();
        }

        // ---------- building the pictures ----------

        void Build()
        {
            var canvasObject = new GameObject("Screen Cards", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // The card: a dark band across the middle with a heading and one line.
            _card = Panel("Card", canvasObject.transform, new Vector2(0f, 0.36f), new Vector2(1f, 0.64f), new Color(0f, 0f, 0f, 0.82f)).gameObject.AddComponent<CanvasGroup>();
            _heading = Label("Heading", _card.transform, 96, new Vector2(0f, 46f), FontStyle.Bold);
            _line = Label("Line", _card.transform, 40, new Vector2(0f, -62f), FontStyle.Normal);
            _card.alpha = 0f;
            _card.blocksRaycasts = false;

            // The summary: a panel at the top, out of the way of the portals.
            _summary = Panel("Summary", canvasObject.transform, new Vector2(0.27f, 0.56f), new Vector2(0.73f, 0.88f), new Color(0f, 0f, 0f, 0.82f));
            _summaryText = Label("Text", _summary, 38, Vector2.zero, FontStyle.Normal);
            _summaryText.lineSpacing = 1.15f;
            _summary.gameObject.SetActive(false);

            // The title: the game's name low on the screen, so the Void shows above it.
            _titleGroup = Panel("Title", canvasObject.transform, new Vector2(0f, 0f), new Vector2(1f, 0.42f), new Color(0f, 0f, 0f, 0.55f)).gameObject.AddComponent<CanvasGroup>();
            _titleText = Label("Name", _titleGroup.transform, 190, new Vector2(0f, 80f), FontStyle.Bold);
            var tag = Label("Tagline", _titleGroup.transform, 44, new Vector2(0f, -52f), FontStyle.Italic);
            tag.text = tagline;
            _titleHint = Label("Hint", _titleGroup.transform, 28, new Vector2(0f, -205f), FontStyle.Normal);
            _titleHint.color = new Color(1f, 1f, 1f, 0.7f);

            // Two buttons: the big one plays, the small one in the corner throws the save away (asked twice).
            _playButton = Button("Play", _titleGroup.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -135f), new Vector2(560f, 92f), new Color(0.45f, 0.95f, 1f, 0.95f), 46, out _playLabel);
            _playLabel.color = new Color(0.02f, 0.05f, 0.08f);
            _eraseButton = Button("Erase", _titleGroup.transform, new Vector2(1f, 0f), new Vector2(-190f, 48f), new Vector2(340f, 56f), new Color(1f, 1f, 1f, 0.12f), 26, out _eraseLabel);
            _titleGroup.alpha = 0f;
            _titleGroup.blocksRaycasts = false;
        }

        RectTransform Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = colour;
            image.raycastTarget = false;
            return rect;
        }

        RectTransform Button(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color colour, int textSize, out Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = colour;
            image.raycastTarget = false;
            label = Label("Label", rect, textSize, Vector2.zero, FontStyle.Bold);
            return rect;
        }

        Text Label(string name, Transform parent, int size, Vector2 position, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = position;
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        // ---------- the title ----------

        RectTransform _playButton, _eraseButton;
        Text _playLabel, _eraseLabel;
        float _eraseArmedUntil;

        /// <summary>What the title's buttons say, for tests.</summary>
        public string PlayLabel => _playLabel.text;
        public string EraseLabel => _eraseLabel.text;
        /// <summary>True for a few seconds after the erase button is first pressed: a second press means it.</summary>
        public bool EraseArmed => Time.unscaledTime < _eraseArmedUntil;

        public void ShowTitle()
        {
            TitleShowing = true;
            _titleText.text = gameName;
            RunSave run = SaveSystem.Data.run;
            MetaSave meta = SaveSystem.Data.meta;
            bool touch = UnityEngine.Device.Application.isMobilePlatform;
            _playLabel.text = run.active ? $"CONTINUE  ·  floor {run.floorNumber}" : meta.runsStarted > 0 ? "PLAY" : "START";
            _titleHint.text = touch ? "" : "or press any key";
            _eraseArmedUntil = 0f;
            _eraseLabel.text = "erase save";
            // Nothing to erase on a first visit.
            _eraseButton.gameObject.SetActive(meta.runsStarted > 0 || run.active);
            _titleGroup.alpha = 1f;
            ShowHud(false);
            var follow = CameraFollow.Instance;
            if (follow != null) follow.enabled = false;
            _orbit = 200f;
        }

        public void DismissTitle()
        {
            if (!TitleShowing) return;
            TitleShowing = false;
            TitleSeen = true;
            _titleGroup.alpha = 0f;
            ShowHud(true);
            var follow = CameraFollow.Instance;
            if (follow != null)
            {
                follow.enabled = true;
                follow.Snap();
            }
        }

        /// <summary>
        /// The small button in the corner: the first press asks, a second within a few seconds throws away all
        /// progress and opens the game again from nothing.
        /// </summary>
        public void PressErase()
        {
            if (!TitleShowing) return;
            if (!EraseArmed)
            {
                _eraseArmedUntil = Time.unscaledTime + 4f;
                _eraseLabel.text = "really? press again";
                return;
            }
            SaveSystem.Wipe();
            Radio.Forget();
            RunDirector.LastRunMessage = null;
            TitleSeen = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        // The game's own HUD is put away while the title is up.
        void ShowHud(bool show)
        {
            var hud = FindAnyObjectByType<Hud>();
            var canvas = hud != null ? hud.GetComponentInParent<Canvas>() : null;
            if (canvas != null) canvas.enabled = show;
        }

        void Update()
        {
            if (TitleShowing)
            {
                // The camera drifts round the Void, in whatever style it is wearing, until the player starts.
                var player = PlayerController.Instance;
                var view = Camera.main;
                if (player != null && view != null)
                {
                    _orbit += 14f * Time.unscaledDeltaTime;
                    Vector3 middle = player.transform.position + Vector3.up * 1.5f;
                    Vector3 from = middle + Quaternion.Euler(8f, _orbit, 0f) * new Vector3(0f, 0f, -4.6f);
                    // Looking a little below the Void puts it in the top half of the screen, clear of the name.
                    view.transform.SetPositionAndRotation(from, Quaternion.LookRotation(middle - Vector3.up * 1.1f - from));
                }

                if (_eraseArmedUntil > 0f && !EraseArmed)
                {
                    _eraseArmedUntil = 0f;
                    _eraseLabel.text = "erase save";
                }

                // A key or a pad button plays. A click or a tap has to land on a button, so the small one in the
                // corner can be reached without starting the game.
                bool key = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                           || (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame));
                Vector2? press = null;
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) press = Mouse.current.position.ReadValue();
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) press = Touchscreen.current.primaryTouch.position.ReadValue();

                if (press.HasValue && _eraseButton.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(_eraseButton, press.Value, null)) PressErase();
                else if (key || (press.HasValue && RectTransformUtility.RectangleContainsScreenPoint(_playButton, press.Value, null))) DismissTitle();
            }

            if (SummaryShowing && Time.unscaledTime >= _summaryUntil) _summary.gameObject.SetActive(false);

            // Count the hits for the summary.
            var current = PlayerController.Instance;
            if (current != _watched)
            {
                if (_watched != null) _watched.Health.Damaged -= OnPlayerHit;
                _watched = current;
                if (_watched != null) _watched.Health.Damaged += OnPlayerHit;
            }
        }

        void OnPlayerHit(int amount, Vector3 direction)
        {
            if (amount > 0) _hits++;
        }

        // ---------- cards ----------

        /// <summary>Shows the band with a heading and a line for a few seconds. The game carries on underneath.</summary>
        public void ShowCard(string heading, string line, float seconds, bool freeze = false)
        {
            LastHeading = heading;
            LastLine = line;
            _heading.text = heading;
            _line.text = line;
            if (_showing != null) StopCoroutine(_showing);
            Unfreeze();
            _showing = StartCoroutine(Run(seconds, freeze && !Quiet));
        }

        bool _frozen;
        float _timeBefore = 1f;

        void Unfreeze()
        {
            if (!_frozen) return;
            _frozen = false;
            Time.timeScale = _timeBefore;
        }

        IEnumerator Run(float seconds, bool freeze)
        {
            if (freeze && Time.timeScale > 0f)
            {
                _frozen = true;
                _timeBefore = GameLoop.InSlowMotion ? 1f : Time.timeScale;
                Time.timeScale = 0f;
            }

            const float fade = 0.2f;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                _card.alpha = Mathf.Clamp01(Mathf.Min(t / fade, (seconds - t) / fade));
                if (freeze && t >= bossFreeze) Unfreeze();
                yield return null;
            }
            _card.alpha = 0f;
            Unfreeze();
            _showing = null;
        }

        void OnFloorBuilt(FloorManager floors)
        {
            if (floors.biome == null) return;
            bool arrived = floors.FloorInBiome == 1 || floors.FloorNumber < _lastFloor || _lastFloor == 0;
            _lastFloor = floors.FloorNumber;
            _announced = null;
            _summary.gameObject.SetActive(false);

            if (arrived)
            {
                // A new biome (or a new run, or a saved one picked up): start the summary's count again.
                _biomeStarted = Time.time;
                _killsAtStart = RunState.Kills;
                _samplesAtStart = RunState.Samples;
                var player = PlayerController.Instance;
                _itemsAtStart = player != null && player.Items != null ? player.Items.Collected.Count : 0;
                _hits = 0;
                _summaryShown = false;

                string theme = string.IsNullOrEmpty(floors.biome.mutationTheme) ? "" : floors.biome.mutationTheme;
                string depth = floors.depth > 1 ? $"depth {floors.depth}  ·  samples worth ×{RunDirector.RewardMultiplier(floors.depth):0.#}" : "";
                string line = theme.Length > 0 && depth.Length > 0 ? theme + "  ·  " + depth : theme + depth;
                ShowCard(floors.biome.displayName.ToUpperInvariant(), line, 3f);
                return;
            }

            string tip = TipFor(floors.biome.id, floors.Layout.seed);
            ShowCard("FLOOR " + floors.FloorInBiome, tip, 2.2f);
        }

        void OnRoomChanged(FloorManager floors)
        {
            // A boss has walked on: its name, and one line about it, with the world held still.
            if (floors.Boss != null && floors.Boss != _announced && !floors.Boss.Health.IsDead)
            {
                _announced = floors.Boss;
                ShowCard(floors.Boss.displayName.ToUpperInvariant(), floors.Boss.subtitle, bossFreeze + 0.5f, true);
                return;
            }

            // The biome's boss is down and the portals are up: how it went.
            if (floors.ExtractPortal != null && !_summaryShown && floors.FloorInBiome == 10)
            {
                _summaryShown = true;
                ShowSummary(floors);
            }
        }

        void ShowSummary(FloorManager floors)
        {
            var player = PlayerController.Instance;
            int items = player != null && player.Items != null ? player.Items.Collected.Count - _itemsAtStart : 0;
            int seconds = Mathf.RoundToInt(Time.time - _biomeStarted);
            int kills = RunState.Kills - _killsAtStart;
            int samples = RunState.Samples - _samplesAtStart;
            string verdict = _hits == 0 ? "Not a scratch. Suspicious." : _hits <= 5 ? "Barely touched." : _hits <= 15 ? "A few new dents." : "Mostly dents now.";

            LastSummary = (floors.biome.isBottom ? "DR. FINCH, MISPLACED\n" : $"{floors.biome.displayName.ToUpperInvariant()} CLEARED\n") +
                          $"Time {seconds / 60}:{seconds % 60:00}   ·   Kills {kills}   ·   Hits taken {_hits}\n" +
                          $"Samples +{samples}   ·   Items +{Mathf.Max(0, items)}\n" +
                          verdict + (floors.biome.isBottom ? "  There is no deeper. Go home." : "  Go home, or go deeper.");
            _summaryText.text = LastSummary;
            _summary.gameObject.SetActive(true);
            _summaryUntil = Time.unscaledTime + 12f;
        }
    }
}
