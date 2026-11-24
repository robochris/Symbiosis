using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Shrunk
{
    /// <summary>
    /// The end of a floor, on screens of its own. It opens when the player uses the hole or a portal, so the room can be
    /// walked round first; "Not yet" goes back to it. The world freezes behind it.
    /// Page one is how it went: four numbers, what was found, and the XP bar filling. After a world's boss, page two is
    /// where next: a big card for each world on offer, and the way home under them. After an ordinary floor, page one's
    /// button drops straight down the hole.
    /// </summary>
    public class FloorResults : MonoBehaviour
    {
        /// <summary>Off in headless test runs unless a test asks for it, like the other full-screen cards.</summary>
        public static bool ShowInTests;
        public static bool WillShow => !Application.isBatchMode || ShowInTests;
        public static bool IsOpen => _instance != null && _instance._open;
        public static FloorResults Instance => _instance;
        public static int ClosedFrame = -1;

        static FloorResults _instance;

        public class Choice
        {
            public string title, line;
            public Color tint;
            public bool home, down;
            public FloorManager.BiomePortal portal;
            public RectTransform card;
            public Image ring;
        }

        public enum Page { Stats, WhereNext }

        // The ways on, the worlds first and home last (home is the button under the cards).
        readonly List<Choice> _choices = new List<Choice>();
        readonly List<string> _news = new List<string>();
        public IReadOnlyList<Choice> Choices => _choices;
        public IReadOnlyList<string> News => _news;
        public string Heading { get; private set; } = "";
        public int Selected => _selected;
        public Page Showing { get; private set; }
        /// <summary>The XP the stats page shows as earned (this floor's, or this world's after a boss).</summary>
        public int XpShown { get; private set; }

        Canvas _canvas;
        RectTransform _root, _back, _continue;
        CanvasGroup _group;
        FloorManager _floors;
        bool _open;
        int _selected;
        int _lastFloor;
        float _openedAt, _pageAt;
        // Where this floor (and this world) started, for their own numbers.
        float _floorTime;
        int _floorKills, _floorSamples, _floorHits, _floorItems, _floorXp, _worldXp;
        // The bar.
        Image _xpFill;
        Text _xpLevel, _xpGain;
        int _xpFrom, _xpTo, _barLevel;

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
            FloorManager.FloorBuilt += OnFloorBuilt;
            Achievements.Unlocked += OnAchievement;
            Progress.LevelledUp += OnLevel;
        }

        void OnDisable()
        {
            FloorManager.FloorBuilt -= OnFloorBuilt;
            Achievements.Unlocked -= OnAchievement;
            Progress.LevelledUp -= OnLevel;
        }

        // ---------- keeping track of what is new ----------

        void OnFloorBuilt(FloorManager floors)
        {
            if (_open) Close();
            var cards = ScreenCards.Instance;
            var player = PlayerController.Instance;
            _floorTime = Time.time;
            _floorKills = RunState.Kills;
            _floorSamples = RunState.Samples;
            _floorHits = cards != null ? cards.WorldHits : 0;
            _floorItems = player != null && player.Items != null ? player.Items.Collected.Count : 0;
            bool arrived = floors.FloorInBiome == 1 || floors.FloorNumber < _lastFloor || _lastFloor == 0;
            _lastFloor = floors.FloorNumber;
            if (arrived) _news.Clear();
            // The XP for reaching a floor is handed out as it is built: count from after that.
            StartCoroutine(MarkXp(arrived));
        }

        IEnumerator MarkXp(bool arrived)
        {
            yield return null;
            _floorXp = Progress.Xp;
            if (arrived) _worldXp = Progress.Xp;
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

        /// <summary>
        /// The player used a portal (null: the way home): shows the screens, with that way picked on the second, and
        /// says true; or says false when the screens are off and the portal should just go.
        /// </summary>
        public static bool Offer(FloorManager floors, FloorManager.BiomePortal portal)
        {
            if (!WillShow || _instance == null || floors == null) return false;
            if (_instance._open) return true;
            _instance.Open(floors);
            for (int i = 0; i < _instance._choices.Count; i++)
                if (portal == null ? _instance._choices[i].home : _instance._choices[i].portal == portal) _instance._selected = i;
            return true;
        }

        /// <summary>The player used the hole: the stats, and a button down.</summary>
        public static bool OfferDown(FloorManager floors)
        {
            if (!WillShow || _instance == null || floors == null) return false;
            if (_instance._open) return true;
            var (title, line) = floors.DescribeDown();
            _instance._floors = floors;
            _instance._choices.Clear();
            _instance._choices.Add(new Choice { title = title, line = line, tint = UiKit.Accent, down = true });
            _instance._selected = 0;
            _instance.Show();
            return true;
        }

        /// <summary>Shows the screens for this floor's ways on.</summary>
        public void Open(FloorManager floors)
        {
            _floors = floors;
            _choices.Clear();
            foreach (var portal in floors.Portals)
            {
                var (title, line) = floors.Describe(portal);
                Color tint = portal.leadsTo == floors.biome ? UiKit.Coral : Tint(portal.leadsTo);
                _choices.Add(new Choice { title = title, line = line, tint = tint, portal = portal });
            }
            var (homeTitle, homeLine) = floors.DescribeHome();
            _choices.Add(new Choice { title = homeTitle, line = homeLine, tint = UiKit.Accent, home = true });
            // Start on the way on, not the way home: one more world is the fun answer.
            _selected = 0;
            Show();
        }

        void Show()
        {
            EnsureCanvas();
            _open = true;
            _openedAt = Time.unscaledTime;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Sfx.Play(SfxId.Item);
            ShowPage(Page.Stats);
        }

        /// <summary>From the stats on: to the worlds, or straight through when there is only one way on.</summary>
        public void Continue()
        {
            if (!_open) return;
            if (Showing == Page.Stats && WorldChoices > 0)
            {
                Sfx.Play(SfxId.Blocked);
                ShowPage(Page.WhereNext);
                return;
            }
            Choose(Showing == Page.Stats ? OnlyWay : _selected);
        }

        // The ways on that are worlds (not home, not the hole).
        int WorldChoices
        {
            get
            {
                int count = 0;
                foreach (var c in _choices) if (!c.home && !c.down) count++;
                return count;
            }
        }

        int OnlyWay => _choices.Count > 0 ? 0 : -1;

        /// <summary>Goes the way of one of the cards.</summary>
        public void Choose(int index)
        {
            if (!_open || index < 0 || index >= _choices.Count) return;
            var choice = _choices[index];
            var floors = _floors;
            Close();
            _news.Clear();
            if (floors == null) return;
            // Nothing left lying on the floor: the boss's drop and the last coins come along.
            var player = PlayerController.Instance;
            if (player != null && !choice.down) Pickup.GatherForLeaving(player.transform.position, 40f);
            if (choice.home) floors.ChooseHome();
            else if (choice.down) floors.DropDown();
            else floors.UsePortal(choice.portal);
        }

        /// <summary>Back to the room, with nothing chosen.</summary>
        public void NotYet()
        {
            if (!_open) return;
            Close();
            Sfx.Play(SfxId.Blocked);
        }

        void Close()
        {
            _open = false;
            ClosedFrame = Time.frameCount;
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

        // ---------- the screens ----------

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

        void ShowPage(Page page)
        {
            Showing = page;
            _pageAt = Time.unscaledTime;
            if (_root != null) Destroy(_root.gameObject);
            _root = UiKit.Fill(new GameObject("Results", typeof(RectTransform)).GetComponent<RectTransform>());
            _root.SetParent(_canvas.transform, false);
            UiKit.Fill(_root);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _xpFill = null;
            foreach (var c in _choices) { c.card = null; c.ring = null; }

            var floors = _floors;
            Color tint = Tint(floors.biome);
            // Behind it all: the world, darkened almost away, a glow of the world's colour at the top, dark corners.
            UiKit.Fill(UiKit.Image(_root, "panel", new Color(0.03f, 0.04f, 0.07f, 0.97f), "Dim").rectTransform, -60f);
            var glow = UiKit.Image(_root, "glow", new Color(tint.r, tint.g, tint.b, 0.22f), "Glow");
            UiKit.Place(glow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1600f, 700f));
            UiKit.Fill(UiKit.Image(_root, "vignette", new Color(1f, 1f, 1f, 0.9f), "Vignette").rectTransform);

            if (page == Page.Stats) BuildStats(tint);
            else BuildWhereNext();

            // Back to the room: to pick up the boss's drop, shop, or look round once more.
            _back = UiKit.Panel(_root, "Not Yet", new Color(1f, 1f, 1f, 0.1f));
            var backImage = _back.GetComponent<Image>();
            if (backImage != null) { backImage.sprite = UiKit.Art("pill"); backImage.type = Image.Type.Sliced; }
            UiKit.Place(_back, new Vector2(0f, 0f), new Vector2(190f, 64f), new Vector2(280f, 76f));
            var backLabel = UiKit.Label(_back, "Label", "NOT YET", 26, UiKit.Cream, TextAnchor.MiddleCenter, true);
            UiKit.Fill(backLabel.rectTransform);
            Highlight();
        }

        void BuildStats(Color tint)
        {
            var floors = _floors;
            var cards = ScreenCards.Instance;
            var player = PlayerController.Instance;
            var biome = floors.biome;
            bool worldEnd = WorldChoices > 0 || _choices.Exists(c => c.home);

            // The heading.
            string kicker = !worldEnd ? $"FLOOR {floors.FloorInBiome} CLEARED" : floors.InGauntlet ? "GAUNTLET" : biome.isBottom ? "THE BOTTOM" : floors.FloorInBiome == 10 ? "WORLD CLEARED" : "CLEARED";
            var k = UiKit.Label(_root, "Kicker", Spaced(kicker), 28, tint, TextAnchor.MiddleCenter, true);
            UiKit.Place(k.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1200f, 40f));
            Heading = biome.isBottom ? "DR. FINCH, MISPLACED" : biome.displayName.ToUpperInvariant();
            var h = UiKit.Label(_root, "Heading", Heading, 88, UiKit.Cream, TextAnchor.MiddleCenter, true);
            UiKit.Place(h.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -188f), new Vector2(1700f, 104f));
            int worldHits = cards != null ? cards.WorldHits : 0;
            int hits = worldEnd ? worldHits : worldHits >= _floorHits ? worldHits - _floorHits : worldHits;
            var v = UiKit.Label(_root, "Verdict", ScreenCards.Verdict(hits), 30, UiKit.Dim, TextAnchor.MiddleCenter);
            v.fontStyle = FontStyle.Italic;
            UiKit.Place(v.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -262f), new Vector2(1400f, 40f));

            // Four numbers.
            int seconds = worldEnd ? (cards != null ? Mathf.RoundToInt(cards.WorldSeconds) : 0) : Mathf.RoundToInt(Time.time - _floorTime);
            int kills = worldEnd ? (cards != null ? cards.WorldKills : RunState.Kills) : Mathf.Max(0, RunState.Kills - _floorKills);
            int samples = worldEnd ? (cards != null ? cards.WorldSamples : RunState.Samples) : Mathf.Max(0, RunState.Samples - _floorSamples);
            Stat(0, $"{seconds / 60}:{seconds % 60:00}", "TIME");
            Stat(1, kills.ToString(), "KILLS");
            Stat(2, hits.ToString(), "HITS TAKEN");
            Stat(3, "+" + samples, "SAMPLES");

            // What was found.
            var found = new List<ItemDefinition>();
            if (player != null && player.Items != null)
            {
                var all = player.Items.Collected;
                int from = worldEnd ? (cards != null ? cards.WorldItemsFrom : 0) : Mathf.Min(_floorItems, all.Count);
                for (int i = from; i < all.Count; i++) found.Add(all[i]);
            }
            Section("Found", found.Count > 0 ? $"FOUND  ·  {found.Count}" : "FOUND", new Vector2(0f, -470f));
            if (found.Count == 0)
            {
                var none = UiKit.Label(_root, "Nothing", worldEnd ? "Nothing this time. The next world owes you one." : "Nothing on this floor.", 28, UiKit.Dim);
                UiKit.Place(none.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -545f), new Vector2(900f, 40f));
            }
            else
            {
                int shown = Mathf.Min(found.Count, 7);
                for (int i = 0; i < shown; i++)
                {
                    float x = (i - (shown - 1) * 0.5f) * 140f;
                    ItemTile(found[i], new Vector2(x, -565f), i == shown - 1 && found.Count > shown ? found.Count - shown : 0);
                }
            }

            // The XP bar, filling up from where this floor (or world) started.
            int earned = Mathf.Max(0, Progress.Xp - (worldEnd ? _worldXp : _floorXp));
            if (!worldEnd) earned += Progress.FloorXp; // given on the way down
            XpShown = earned;
            _xpTo = Progress.Xp + (worldEnd ? 0 : Progress.FloorXp);
            _xpFrom = Mathf.Max(0, _xpTo - earned);
            _barLevel = Progress.LevelFor(_xpFrom);
            BuildXpBar(tint);

            // News, a line or two.
            if (_news.Count > 0)
            {
                string words = string.Join("     ·     ", _news.GetRange(0, Mathf.Min(2, _news.Count)));
                var star = UiKit.Label(_root, "New", "NEW  " + words, 26, UiKit.Gold);
                UiKit.Place(star.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -895f), new Vector2(1500f, 40f));
            }

            // On.
            string go = WorldChoices > 0 ? "CONTINUE" : _choices.Count > 0 ? (_choices[0].down ? "DOWN TO " + _choices[0].title : _choices[0].title) : "CONTINUE";
            _continue = Pill("Continue", go, UiKit.Accent, new Vector2(0f, 100f), new Vector2(520f, 100f), 40, true);
            Hint(Touch ? "Tap to go on" : "Enter / Space to go on  ·  Esc: not yet");
        }

        void BuildXpBar(Color tint)
        {
            const float width = 1000f;
            var track = UiKit.Image(_root, "pill", new Color(1f, 1f, 1f, 0.08f), "XP Track");
            track.type = Image.Type.Sliced;
            UiKit.Place(track.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -800f), new Vector2(width, 30f));
            _xpFill = UiKit.Image(track.rectTransform, "pill", UiKit.Gold, "XP Fill");
            _xpFill.type = Image.Type.Sliced;
            var fillRect = _xpFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            _xpLevel = UiKit.Label(_root, "XP Level", "", 30, UiKit.Cream, TextAnchor.MiddleLeft, true);
            UiKit.Place(_xpLevel.rectTransform, new Vector2(0.5f, 1f), new Vector2(-width * 0.5f, -755f), new Vector2(width * 0.7f, 40f), new Vector2(0f, 0.5f));
            _xpGain = UiKit.Label(_root, "XP Gain", $"+{XpShown} XP", 30, UiKit.Gold, TextAnchor.MiddleRight, true);
            UiKit.Place(_xpGain.rectTransform, new Vector2(0.5f, 1f), new Vector2(width * 0.5f, -755f), new Vector2(width * 0.4f, 40f), new Vector2(1f, 0.5f));
            PaintXp(_xpFrom);
        }

        // The bar at an amount of XP: the level it is in and how far through.
        void PaintXp(float xp)
        {
            if (_xpFill == null) return;
            int whole = Mathf.FloorToInt(xp);
            int level = Progress.LevelFor(whole);
            float start = Progress.XpFor(level);
            float through = Mathf.Clamp01((xp - start) / Progress.ToNext(level));
            float width = ((RectTransform)_xpFill.rectTransform.parent).rect.width;
            if (width <= 0f) width = 1000f;
            _xpFill.rectTransform.sizeDelta = new Vector2(Mathf.Max(30f, width * through), 0f);
            _xpFill.enabled = through > 0.001f;
            if (level != _barLevel)
            {
                // Over the top: a level gained, right here.
                _barLevel = level;
                Sfx.Play(SfxId.Item);
                _xpLevel.color = UiKit.Gold;
            }
            _xpLevel.text = $"LEVEL {level}  ·  {Progress.Rank(level)}";
        }

        void BuildWhereNext()
        {
            var floors = _floors;
            var k = UiKit.Label(_root, "Kicker", Spaced("WHERE NEXT?"), 28, UiKit.Dim, TextAnchor.MiddleCenter, true);
            UiKit.Place(k.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(1200f, 40f));
            Heading = "CHOOSE A WORLD";
            var h = UiKit.Label(_root, "Heading", floors.InGauntlet ? "THE GAUNTLET" : Heading, 76, UiKit.Cream, TextAnchor.MiddleCenter, true);
            UiKit.Place(h.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -205f), new Vector2(1700f, 90f));

            // A big card for each world.
            var worlds = new List<Choice>();
            foreach (var c in _choices) if (!c.home && !c.down) worlds.Add(c);
            float width = 470f, gap = 40f;
            for (int i = 0; i < worlds.Count; i++)
            {
                float x = (i - (worlds.Count - 1) * 0.5f) * (width + gap);
                ChoiceCard(worlds[i], new Vector2(x, -555f), new Vector2(width, 420f));
            }

            // Home, under them.
            var home = _choices.Find(c => c.home);
            if (home != null)
            {
                home.card = Pill("Home", $"{home.title}  ·  {home.line}", new Color(1f, 1f, 1f, 0.12f), new Vector2(0f, 215f), new Vector2(620f, 92f), 32, false);
                home.ring = UiKit.Image(home.card, "panel_edge", UiKit.Accent, "Selected");
                home.ring.type = Image.Type.Sliced;
                UiKit.Fill(home.ring.rectTransform, -6f);
            }
            Hint(Touch ? "Tap a world, or go home" : "Click a card  ·  or ← →  and  Enter  ·  Esc: not yet");
        }

        static bool Touch => Touchscreen.current != null && Application.isMobilePlatform;

        RectTransform Pill(string name, string words, Color colour, Vector2 fromBottom, Vector2 size, int textSize, bool dark)
        {
            var pill = UiKit.Panel(_root, name, colour);
            var image = pill.GetComponent<Image>();
            if (image != null) { image.sprite = UiKit.Art("pill"); image.type = Image.Type.Sliced; }
            UiKit.Place(pill, new Vector2(0.5f, 0f), fromBottom, size);
            var label = UiKit.Label(pill, "Label", words, textSize, dark ? new Color(0.02f, 0.05f, 0.08f) : UiKit.Cream, TextAnchor.MiddleCenter, true);
            UiKit.Fill(label.rectTransform);
            if (dark) { var shadow = label.GetComponent<Shadow>(); if (shadow != null) shadow.enabled = false; }
            return pill;
        }

        void Hint(string words)
        {
            var hint = UiKit.Label(_root, "Hint", words, 24, new Color(1f, 1f, 1f, 0.45f));
            UiKit.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(1200f, 34f));
        }

        static string Spaced(string text) => string.Join(" ", text.ToCharArray()).Replace("   ", "     ");

        void Stat(int index, string value, string label)
        {
            const float w = 260f, gap = 28f;
            float x = (index - 1.5f) * (w + gap);
            var tile = UiKit.Panel(_root, "Stat " + label, UiKit.InkRaised, shadow: true);
            UiKit.Place(tile, new Vector2(0.5f, 1f), new Vector2(x, -365f), new Vector2(w, 124f));
            var big = UiKit.Label(tile, "Value", value, 52, UiKit.Cream, TextAnchor.MiddleCenter, true);
            UiKit.Place(big.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(w, 62f));
            var small = UiKit.Label(tile, "Label", label, 20, UiKit.Dim, TextAnchor.MiddleCenter, true);
            UiKit.Place(small.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -35f), new Vector2(w, 30f));
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
            UiKit.Place(label.rectTransform, new Vector2(0.5f, 1f), at + new Vector2(0f, -70f), new Vector2(134f, 50f));
        }

        void ChoiceCard(Choice choice, Vector2 at, Vector2 size)
        {
            var card = UiKit.Panel(_root, "Card " + choice.title, UiKit.InkRaised, shadow: true);
            UiKit.Place(card, new Vector2(0.5f, 1f), at, size);
            choice.card = card;
            // A wash of the world's colour at the top, a disc of it, the name and what it is.
            var glow = UiKit.Image(card, "glow", new Color(choice.tint.r, choice.tint.g, choice.tint.b, 0.35f), "Glow");
            UiKit.Place(glow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(size.x * 1.1f, 260f));
            var band = UiKit.Image(card, "pill", choice.tint, "Band");
            UiKit.Place(band.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(size.x - 70f, 8f));
            var disc = UiKit.Image(card, "circle", choice.tint, "Disc");
            UiKit.Place(disc.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(110f, 110f));
            var ring = UiKit.Image(disc.rectTransform, "ring", new Color(1f, 1f, 1f, 0.5f), "Disc Ring");
            UiKit.Fill(ring.rectTransform, -10f);
            var title = UiKit.Label(card, "Title", choice.title, choice.title.Length > 14 ? 36 : 44, UiKit.Cream, TextAnchor.MiddleCenter, true);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -232f), new Vector2(size.x - 30f, 58f));
            var line = UiKit.Label(card, "Line", choice.line, 27, UiKit.Dim, TextAnchor.UpperCenter);
            UiKit.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -345f), new Vector2(size.x - 50f, 130f));
            // The ring that marks the one picked.
            choice.ring = UiKit.Image(card, "panel_edge", choice.tint, "Selected");
            choice.ring.type = Image.Type.Sliced;
            UiKit.Fill(choice.ring.rectTransform, -6f);
        }

        void Highlight()
        {
            for (int i = 0; i < _choices.Count; i++)
            {
                var c = _choices[i];
                if (c.card == null) continue;
                bool on = i == _selected;
                if (c.ring != null) c.ring.enabled = on;
                c.card.localScale = Vector3.one * (on ? 1.04f : 1f);
            }
        }

        // ---------- input ----------

        void Update()
        {
            if (!_open || _root == null) return;
            // Fade in over a quarter of a second, in real time: the world is stopped.
            if (_group != null) _group.alpha = Mathf.Clamp01((Time.unscaledTime - _pageAt) / 0.25f);
            Time.timeScale = 0f;

            // The bar fills after a beat, over a second and a half.
            if (Showing == Page.Stats && _xpFill != null)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - _pageAt - 0.5f) / 1.5f);
                t = 1f - (1f - t) * (1f - t);
                PaintXp(Mathf.Lerp(_xpFrom, _xpTo, t));
            }

            // A short pause before anything counts, so a held button from the fight does not pick for you.
            if (Time.unscaledTime - _pageAt < 0.5f || Time.unscaledTime - _openedAt < 0.6f) return;

            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            int move = 0;
            bool confirm = false;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) move--;
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) move++;
                confirm |= keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame;
                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame) { NotYet(); return; }
            }
            if (pad != null)
            {
                if (pad.dpad.left.wasPressedThisFrame) move--;
                if (pad.dpad.right.wasPressedThisFrame) move++;
                confirm |= pad.buttonSouth.wasPressedThisFrame;
                if (pad.buttonEast.wasPressedThisFrame || pad.startButton.wasPressedThisFrame) { NotYet(); return; }
            }

            Vector2? press = null;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) press = mouse.position.ReadValue();
            var screen = Touchscreen.current;
            if (screen != null && screen.primaryTouch.press.wasPressedThisFrame) press = screen.primaryTouch.position.ReadValue();
            if (press.HasValue && _back != null && RectTransformUtility.RectangleContainsScreenPoint(_back, press.Value, null))
            {
                NotYet();
                return;
            }

            if (Showing == Page.Stats)
            {
                // Anywhere goes on: the whole screen is the button.
                if (confirm || press.HasValue) Continue();
                return;
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
