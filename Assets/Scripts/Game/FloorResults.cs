using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Shrunk
{
    /// <summary>
    /// The end of a world, on a screen of its own: how it went, what was found, anything new, and then a card for
    /// each way on (home, deeper, another world). Picking a card goes there; nobody has to walk onto a portal.
    /// The world freezes behind it. The portals are still in the room underneath, for tests and for safety.
    /// </summary>
    public class FloorResults : MonoBehaviour
    {
        /// <summary>Off in headless test runs unless a test asks for it, like the other full-screen cards.</summary>
        public static bool ShowInTests;
        public static bool WillShow => !Application.isBatchMode || ShowInTests;
        public static bool IsOpen => _instance != null && _instance._open;
        public static FloorResults Instance => _instance;

        /// <summary>For tests: how long after the last kill the screen comes up.</summary>
        public static float DelayOverride = -1f;

        static FloorResults _instance;

        public class Choice
        {
            public string title, line;
            public Color tint;
            public bool home;
            public FloorManager.BiomePortal portal;
            public RectTransform card;
            public Image ring;
        }

        readonly List<Choice> _choices = new List<Choice>();
        readonly List<string> _news = new List<string>();
        public IReadOnlyList<Choice> Choices => _choices;
        public IReadOnlyList<string> News => _news;
        public string Heading { get; private set; } = "";
        public int Selected => _selected;

        Canvas _canvas;
        RectTransform _root;
        CanvasGroup _group;
        FloorManager _floors;
        bool _open;
        int _selected;
        int _lastFloor;
        float _openedAt;
        Coroutine _waiting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("Floor Results");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<FloorResults>();
        }

        void OnEnable()
        {
            FloorManager.ChoicesOpened += OnChoices;
            FloorManager.FloorBuilt += OnFloorBuilt;
            Achievements.Unlocked += OnAchievement;
            Progress.LevelledUp += OnLevel;
        }

        void OnDisable()
        {
            FloorManager.ChoicesOpened -= OnChoices;
            FloorManager.FloorBuilt -= OnFloorBuilt;
            Achievements.Unlocked -= OnAchievement;
            Progress.LevelledUp -= OnLevel;
        }

        // ---------- keeping track of what is new this world ----------

        void OnFloorBuilt(FloorManager floors)
        {
            if (_open) Close();
            if (_waiting != null) { StopCoroutine(_waiting); _waiting = null; }
            bool arrived = floors.FloorInBiome == 1 || floors.FloorNumber < _lastFloor || _lastFloor == 0;
            _lastFloor = floors.FloorNumber;
            if (arrived) _news.Clear();
        }

        void OnAchievement(Achievement achievement)
        {
            if (achievement == null) return;
            _news.Add(string.IsNullOrEmpty(achievement.reward) ? achievement.name : $"{achievement.name}  ·  {achievement.reward}");
        }

        void OnLevel(int level)
        {
            _news.Add($"Level {level}  ·  {Progress.Rank(level)}");
        }

        // ---------- opening ----------

        void OnChoices(FloorManager floors)
        {
            if (!WillShow) return;
            if (_waiting != null) StopCoroutine(_waiting);
            _waiting = StartCoroutine(OpenSoon(floors));
        }

        IEnumerator OpenSoon(FloorManager floors)
        {
            // A beat to watch the boss go down, and for its cards to finish.
            yield return new WaitForSecondsRealtime(DelayOverride >= 0f ? DelayOverride : 2.6f);
            while (floors != null && ((ScreenCards.Instance != null && ScreenCards.Instance.CardShowing) || PauseMenu.IsOpen))
                yield return null;
            _waiting = null;
            var player = PlayerController.Instance;
            if (floors == null || player == null || player.Health.IsDead || floors.ExtractPortal == null) yield break;
            // Nothing left lying on the floor: the boss's drop and the last coins come along.
            Pickup.GatherForLeaving(player.transform.position, 40f);
            Open(floors);
        }

        /// <summary>Shows the screen for this floor's ways on.</summary>
        public void Open(FloorManager floors)
        {
            _floors = floors;
            _choices.Clear();
            var (homeTitle, homeLine) = floors.DescribeHome();
            _choices.Add(new Choice { title = homeTitle, line = homeLine, tint = UiKit.Accent, home = true });
            foreach (var portal in floors.Portals)
            {
                var (title, line) = floors.Describe(portal);
                Color tint = portal.leadsTo == floors.biome ? UiKit.Coral : Tint(portal.leadsTo);
                _choices.Add(new Choice { title = title, line = line, tint = tint, portal = portal });
            }
            // Start on the way on, not the way home: one more world is the fun answer.
            _selected = _choices.Count > 1 ? 1 : 0;
            Build();
            _open = true;
            _openedAt = Time.unscaledTime;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Sfx.Play(SfxId.Item);
            Highlight();
        }

        /// <summary>Goes the way of one of the cards.</summary>
        public void Choose(int index)
        {
            if (!_open || index < 0 || index >= _choices.Count) return;
            var choice = _choices[index];
            var floors = _floors;
            Close();
            if (floors == null) return;
            if (choice.home) floors.ChooseHome();
            else floors.UsePortal(choice.portal);
        }

        void Close()
        {
            _open = false;
            if (_root != null) Destroy(_root.gameObject);
            _root = null;
            Time.timeScale = 1f;
        }

        // A world's own colour, lifted so it reads on the dark screen.
        static Color Tint(BiomeDefinition biome)
        {
            if (biome == null) return UiKit.Gold;
            Color.RGBToHSV(biome.ambientColor, out float h, out float s, out _);
            return Color.HSVToRGB(h, Mathf.Clamp(s, 0.35f, 0.7f), 1f);
        }

        // ---------- the screen ----------

        void EnsureCanvas()
        {
            if (_canvas != null) return;
            var go = new GameObject("Results Canvas", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 80; // over the HUD, the radio and the cards
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        void Build()
        {
            EnsureCanvas();
            if (_root != null) Destroy(_root.gameObject);
            _root = UiKit.Fill(new GameObject("Results", typeof(RectTransform)).GetComponent<RectTransform>());
            _root.SetParent(_canvas.transform, false);
            UiKit.Fill(_root);
            _group = _root.gameObject.AddComponent<CanvasGroup>();

            var floors = _floors;
            var cards = ScreenCards.Instance;
            var player = PlayerController.Instance;
            var biome = floors.biome;
            Color tint = Tint(biome);

            // Behind it all: the world, darkened almost away, a glow of the world's colour at the top, dark corners.
            UiKit.Fill(UiKit.Image(_root, "panel", new Color(0.03f, 0.04f, 0.07f, 0.97f), "Dim").rectTransform, -60f);
            var glow = UiKit.Image(_root, "glow", new Color(tint.r, tint.g, tint.b, 0.22f), "Glow");
            UiKit.Place(glow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1600f, 700f));
            UiKit.Fill(UiKit.Image(_root, "vignette", new Color(1f, 1f, 1f, 0.9f), "Vignette").rectTransform);

            // The heading.
            string kicker = floors.InGauntlet ? "GAUNTLET" : biome.isBottom ? "THE BOTTOM" : floors.FloorInBiome == 10 ? "WORLD CLEARED" : "CLEARED";
            var k = UiKit.Label(_root, "Kicker", Spaced(kicker), 28, tint, TextAnchor.MiddleCenter, true);
            UiKit.Place(k.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(1200f, 40f));
            Heading = biome.isBottom ? "DR. FINCH, MISPLACED" : biome.displayName.ToUpperInvariant();
            var h = UiKit.Label(_root, "Heading", Heading, 84, UiKit.Cream, TextAnchor.MiddleCenter, true);
            UiKit.Place(h.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1700f, 100f));
            int hits = cards != null ? cards.WorldHits : 0;
            var v = UiKit.Label(_root, "Verdict", ScreenCards.Verdict(hits), 30, UiKit.Dim, TextAnchor.MiddleCenter);
            v.fontStyle = FontStyle.Italic;
            UiKit.Place(v.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -218f), new Vector2(1400f, 40f));

            // Four numbers.
            int seconds = cards != null ? Mathf.RoundToInt(cards.WorldSeconds) : 0;
            Stat(0, $"{seconds / 60}:{seconds % 60:00}", "TIME");
            Stat(1, (cards != null ? cards.WorldKills : RunState.Kills).ToString(), "KILLS");
            Stat(2, hits.ToString(), "HITS TAKEN");
            Stat(3, "+" + (cards != null ? cards.WorldSamples : RunState.Samples), "SAMPLES");

            // What was found this world, on the left; anything new, on the right.
            var found = new List<ItemDefinition>();
            if (player != null && player.Items != null)
            {
                var all = player.Items.Collected;
                for (int i = cards != null ? cards.WorldItemsFrom : 0; i < all.Count; i++) found.Add(all[i]);
            }
            bool news = _news.Count > 0;
            float leftX = news ? -430f : 0f;
            Section("Found", $"FOUND  ·  {found.Count}", new Vector2(leftX, -430f));
            if (found.Count == 0)
            {
                var none = UiKit.Label(_root, "Nothing", "Nothing this time. The next world owes you one.", 28, UiKit.Dim);
                UiKit.Place(none.rectTransform, new Vector2(0.5f, 1f), new Vector2(leftX, -505f), new Vector2(820f, 40f));
            }
            else
            {
                int shown = Mathf.Min(found.Count, 6);
                float step = 136f;
                for (int i = 0; i < shown; i++)
                {
                    float x = leftX + (i - (shown - 1) * 0.5f) * step;
                    ItemTile(found[i], new Vector2(x, -520f), i == shown - 1 && found.Count > shown ? found.Count - shown : 0);
                }
            }
            if (news)
            {
                Section("New", "NEW", new Vector2(470f, -430f));
                for (int i = 0; i < _news.Count && i < 4; i++)
                {
                    var star = UiKit.Image(_root, "circle", UiKit.Gold, "Bullet");
                    UiKit.Place(star.rectTransform, new Vector2(0.5f, 1f), new Vector2(150f, -488f - i * 44f), new Vector2(12f, 12f));
                    var line = UiKit.Label(_root, "News", _news[i], 27, UiKit.Cream, TextAnchor.MiddleLeft);
                    line.horizontalOverflow = HorizontalWrapMode.Overflow;
                    UiKit.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(175f, -488f - i * 44f), new Vector2(640f, 40f), new Vector2(0f, 0.5f));
                }
            }

            // Where next.
            Section("Next", "WHERE NEXT?", new Vector2(0f, -640f));
            float width = _choices.Count >= 4 ? 380f : 420f, gap = 28f;
            for (int i = 0; i < _choices.Count; i++)
            {
                float x = (i - (_choices.Count - 1) * 0.5f) * (width + gap);
                ChoiceCard(_choices[i], new Vector2(x, -812f), new Vector2(width, 236f));
            }

            bool touch = Touchscreen.current != null && Application.isMobilePlatform;
            var hint = UiKit.Label(_root, "Hint", touch ? "Tap a card" : "Click a card  ·  or ← →  and  Enter", 24, new Color(1f, 1f, 1f, 0.45f));
            UiKit.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(1200f, 34f));
        }

        static string Spaced(string text) => string.Join(" ", text.ToCharArray()).Replace("   ", "     ");

        void Stat(int index, string value, string label)
        {
            const float w = 250f, gap = 24f;
            float x = (index - 1.5f) * (w + gap);
            var tile = UiKit.Panel(_root, "Stat " + label, UiKit.InkRaised, shadow: true);
            UiKit.Place(tile, new Vector2(0.5f, 1f), new Vector2(x, -318f), new Vector2(w, 118f));
            var big = UiKit.Label(tile, "Value", value, 50, UiKit.Cream, TextAnchor.MiddleCenter, true);
            UiKit.Place(big.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(w, 60f));
            var small = UiKit.Label(tile, "Label", label, 20, UiKit.Dim, TextAnchor.MiddleCenter, true);
            UiKit.Place(small.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -33f), new Vector2(w, 30f));
        }

        void Section(string name, string words, Vector2 at)
        {
            var label = UiKit.Label(_root, name, words, 24, UiKit.Dim, TextAnchor.MiddleCenter, true);
            UiKit.Place(label.rectTransform, new Vector2(0.5f, 1f), at, new Vector2(820f, 34f));
            var rule = UiKit.Image(_root, "pill", new Color(1f, 1f, 1f, 0.08f), name + " Rule");
            UiKit.Place(rule.rectTransform, new Vector2(0.5f, 1f), at + new Vector2(0f, -24f), new Vector2(160f, 4f));
        }

        void ItemTile(ItemDefinition item, Vector2 at, int more)
        {
            Color colour = item.material != null ? item.material.color : Color.white;
            colour.a = 1f;
            var back = UiKit.Image(_root, "circle", new Color(1f, 1f, 1f, 0.06f), "Item " + item.displayName);
            UiKit.Place(back.rectTransform, new Vector2(0.5f, 1f), at, new Vector2(96f, 96f));
            bool boss = BossEnemy.Trophies != null && BossEnemy.Trophies.ContainsValue(item.type);
            var ring = UiKit.Image(back.rectTransform, "ring_thick", boss ? UiKit.Gold : new Color(colour.r, colour.g, colour.b, 0.9f), "Ring");
            UiKit.Fill(ring.rectTransform);
            var dot = UiKit.Image(back.rectTransform, "circle", colour, "Colour");
            UiKit.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f));
            string name = more > 0 ? $"+{more} more" : item.displayName;
            var label = UiKit.Label(_root, "Name", name, 20, boss ? UiKit.Gold : UiKit.Cream);
            UiKit.Place(label.rectTransform, new Vector2(0.5f, 1f), at + new Vector2(0f, -70f), new Vector2(130f, 50f));
        }

        void ChoiceCard(Choice choice, Vector2 at, Vector2 size)
        {
            var card = UiKit.Panel(_root, "Card " + choice.title, UiKit.InkRaised, shadow: true);
            UiKit.Place(card, new Vector2(0.5f, 1f), at, size);
            choice.card = card;
            // A band of its colour along the top.
            var band = UiKit.Image(card, "pill", choice.tint, "Band");
            UiKit.Place(band.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(size.x - 60f, 8f));
            var title = UiKit.Label(card, "Title", choice.title, choice.title.Length > 14 ? 34 : 40, UiKit.Cream, TextAnchor.MiddleCenter, true);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(size.x - 30f, 54f));
            var line = UiKit.Label(card, "Line", choice.line, 25, UiKit.Dim, TextAnchor.UpperCenter);
            UiKit.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(size.x - 40f, 110f));
            // The ring that marks the one picked.
            choice.ring = UiKit.Image(card, "panel_edge", choice.tint, "Selected");
            UiKit.Fill(choice.ring.rectTransform, -6f);
        }

        void Highlight()
        {
            for (int i = 0; i < _choices.Count; i++)
            {
                var c = _choices[i];
                if (c.card == null) continue;
                bool on = i == _selected;
                c.ring.enabled = on;
                c.card.localScale = Vector3.one * (on ? 1.05f : 1f);
            }
        }

        // ---------- input ----------

        void Update()
        {
            if (!_open || _root == null) return;
            // Fade in over a quarter of a second, in real time: the world is stopped.
            if (_group != null) _group.alpha = Mathf.Clamp01((Time.unscaledTime - _openedAt) / 0.25f);
            Time.timeScale = 0f;
            // A short pause before anything counts, so a held button from the fight does not pick for you.
            if (Time.unscaledTime - _openedAt < 0.6f) return;

            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            int move = 0;
            bool confirm = false;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) move--;
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) move++;
                confirm |= keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame;
            }
            if (pad != null)
            {
                if (pad.dpad.left.wasPressedThisFrame) move--;
                if (pad.dpad.right.wasPressedThisFrame) move++;
                confirm |= pad.buttonSouth.wasPressedThisFrame;
            }
            if (move != 0)
            {
                _selected = (_selected + move + _choices.Count) % _choices.Count;
                Sfx.Play(SfxId.Blocked);
                Highlight();
            }
            if (confirm)
            {
                Choose(_selected);
                return;
            }

            Vector2? press = null;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) press = mouse.position.ReadValue();
            var screen = Touchscreen.current;
            if (screen != null && screen.primaryTouch.press.wasPressedThisFrame) press = screen.primaryTouch.position.ReadValue();
            Vector2? hover = mouse != null ? mouse.position.ReadValue() : (Vector2?)null;
            for (int i = 0; i < _choices.Count; i++)
            {
                var card = _choices[i].card;
                if (card == null) continue;
                if (press.HasValue && RectTransformUtility.RectangleContainsScreenPoint(card, press.Value, null))
                {
                    Choose(i);
                    return;
                }
                if (hover.HasValue && i != _selected && mouse.delta.ReadValue().sqrMagnitude > 0.5f && RectTransformUtility.RectangleContainsScreenPoint(card, hover.Value, null))
                {
                    _selected = i;
                    Highlight();
                }
            }
        }
    }
}
