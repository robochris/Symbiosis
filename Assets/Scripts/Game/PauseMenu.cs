using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Shrunk
{
    /// <summary>
    /// The pause menu: resume, the options, and the way back to the lab. Esc, P or the Start button opens it;
    /// on a phone, the pause button in the top-left corner. It builds its own panel, so it needs nothing set up in the scene.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance._open;

        public string labScene = "Lab";

        class Row
        {
            /// <summary>0 the first page, 1 "More options".</summary>
            public int page;
            public string label;
            public Func<string> value;
            public Action use;
            public RectTransform rect;
            public Text text;
            public Image background;
        }

        readonly List<Row> _rows = new List<Row>();
        GameObject _panel;
        RectTransform _pauseButton;
        Font _font;
        int _selected;
        bool _open;

        public int RowCount => _rows.Count;
        public string RowText(int index) => _rows[index].text.text;

        void Awake()
        {
            Instance = this;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            Close();
            Apply();
            StartCoroutine(FitTextSoon());
        }

        void OnEnable() => GameSettings.Changed += Apply;

        void OnDisable() => GameSettings.Changed -= Apply;

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Time.timeScale = 1f;
            }
        }

        // ---------- the rows ----------

        void Build()
        {
            _panel = new GameObject("Pause Menu", typeof(RectTransform), typeof(Image));
            var panel = (RectTransform)_panel.transform;
            panel.SetParent(transform, false);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            _panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

            MakeText(panel, "PAUSED", 56, new Vector2(0f, 445f), new Vector2(900f, 70f));
            // Down the left, clear of the options: the level, and in a run what is being carried and what it does.
            // Below the counts in the top corner, so the two never print over each other.
            _carrying = MakeText(panel, "", 24, new Vector2(-712f, -170f), new Vector2(460f, 860f));
            _carrying.alignment = TextAnchor.UpperLeft;
            _carrying.horizontalOverflow = HorizontalWrapMode.Wrap;
            _carrying.verticalOverflow = VerticalWrapMode.Truncate;

            // The first page: what gets changed mid-run.
            Add("Resume", null, Close);
            Add("Camera", () => CameraName(GameSettings.Camera), () => GameSettings.Camera = CameraFollow.After(GameSettings.Camera));
            Add("Aim assist", () => Level(GameSettings.AimAssist, "Off", "Low", "Medium", "Full"), () => GameSettings.AimAssist = Next(GameSettings.AimAssist, 0f, 0.35f, 0.7f, 1f));
            Add("Look speed", () => Level((GameSettings.LookSpeed - 0.4f) / 1.1f, "Slowest", "Slow", "Normal", "Fast"), () => GameSettings.LookSpeed = Next(GameSettings.LookSpeed, 0.4f, 0.6f, 1f, 1.5f));
            Add("Looking up and down", () => GameSettings.LockVertical ? "Locked" : "Free", () => GameSettings.LockVertical = !GameSettings.LockVertical);
            Add("Aim stick", () => GameSettings.FloatingAim ? "Follows your thumb" : "Stays put", () => GameSettings.FloatingAim = !GameSettings.FloatingAim);
            Add("Difficulty", () => GameSettings.Relaxed ? "Relaxed (+2 health)" : "As intended", () => GameSettings.Relaxed = !GameSettings.Relaxed);
            Add("Sound", () => Level(GameSettings.Volume, "Off", "Quiet", "Full"), () => GameSettings.Volume = Next(GameSettings.Volume, 0f, 0.5f, 1f));
            Add("More options", null, () => ShowPage(1));

            // The second page: set once and left.
            _adding = 1;
            Add("Back", null, () => ShowPage(0));
            Add("Text size", () => Level((GameSettings.TextSize - 0.85f) / 0.3f, "Smaller", "Normal", "Larger"), () => GameSettings.TextSize = Next(GameSettings.TextSize, 0.85f, 1f, 1.15f));
            Add("Screen shake", () => GameSettings.ScreenShake ? "On" : "Off", () => GameSettings.ScreenShake = !GameSettings.ScreenShake);
            Add("Vibration (phone)", () => GameSettings.Vibration ? "On" : "Off", () => GameSettings.Vibration = !GameSettings.Vibration);
            Add("Touch controls", () => GameSettings.LeftHanded ? "Left-handed" : "Right-handed", () => GameSettings.LeftHanded = !GameSettings.LeftHanded);
            Add("Enemy arrows", () => GameSettings.EnemyArrows ? "On" : "Off", () => GameSettings.EnemyArrows = !GameSettings.EnemyArrows);
            Add("Damage numbers", () => GameSettings.DamageNumbers ? "On" : "Off", () => GameSettings.DamageNumbers = !GameSettings.DamageNumbers);
            Add("Radio chatter", () => new[] { "Off", "Less", "Normal" }[GameSettings.Radio], () => GameSettings.Radio = (GameSettings.Radio + 1) % 3);
            _adding = 0;

            // In a run: leave without losing it. In the lab: close the game.
            if (FloorManager.Instance != null || FindAnyObjectByType<RunDirector>() != null)
            {
                Add("Save and go to the lab", null, GoToLab);
            }
            else
            {
                Add("Quit game", null, Application.Quit);
            }

            var onPage = new int[2];
            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                int slot = onPage[row.page]++;
                var go = new GameObject(row.label, typeof(RectTransform), typeof(Image));
                row.rect = (RectTransform)go.transform;
                row.rect.SetParent(panel, false);
                row.rect.anchorMin = row.rect.anchorMax = row.rect.pivot = new Vector2(0.5f, 0.5f);
                // Fourteen rows and the heading, inside the 1080 the canvas is laid out for.
                row.rect.anchoredPosition = new Vector2(0f, 370f - slot * 63f);
                row.rect.sizeDelta = new Vector2(900f, 57f);
                row.background = go.GetComponent<Image>();
                row.text = MakeText(row.rect, "", 33, Vector2.zero, new Vector2(860f, 55f));
            }

            // Pause button, top of the screen just right of the camera label.
            var button = new GameObject("Pause Button", typeof(RectTransform), typeof(Image));
            _pauseButton = (RectTransform)button.transform;
            _pauseButton.SetParent(transform, false);
            _pauseButton.anchorMin = _pauseButton.anchorMax = new Vector2(0.5f, 1f);
            _pauseButton.pivot = new Vector2(0.5f, 0.5f);
            _pauseButton.anchoredPosition = new Vector2(265f, -60f);
            _pauseButton.sizeDelta = new Vector2(70f, 70f);
            button.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
            MakeText(_pauseButton, "II", 40, Vector2.zero, new Vector2(70f, 70f));
        }

        int _adding, _page;

        void Add(string label, Func<string> value, Action use)
        {
            _rows.Add(new Row { page = _adding, label = label, value = value, use = use });
        }

        /// <summary>Which page of options is showing: 0 the first, 1 "More options".</summary>
        public int Page => _page;

        public void ShowPage(int page)
        {
            _page = page;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].page != page) continue;
                _selected = i;
                break;
            }
            Refresh();
        }

        Text MakeText(Transform parent, string content, int size, Vector2 position, Vector2 box)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = box;
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = content;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        static string CameraName(CameraMode mode) => CameraFollow.NameOf(mode);

        // Picks the name for a 0-1 value from evenly spaced names.
        static string Level(float value01, params string[] names) =>
            names[Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(value01) * (names.Length - 1)), 0, names.Length - 1)];

        // The step after the current one, wrapping round.
        static float Next(float current, params float[] steps)
        {
            int nearest = 0;
            for (int i = 1; i < steps.Length; i++)
            {
                if (Mathf.Abs(steps[i] - current) < Mathf.Abs(steps[nearest] - current)) nearest = i;
            }
            return steps[(nearest + 1) % steps.Length];
        }

        // ---------- open, close, use ----------

        public void Open()
        {
            _open = true;
            _page = 0;
            _selected = 0;
            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Refresh();
        }

        public void Close()
        {
            _open = false;
            if (_panel != null) _panel.SetActive(false);
            Time.timeScale = 1f;
        }

        /// <summary>Does what clicking that row does.</summary>
        public void Use(int index)
        {
            if (index < 0 || index >= _rows.Count) return;
            _rows[index].use?.Invoke();
            if (_open) Refresh();
        }

        public int RowNamed(string label)
        {
            for (int i = 0; i < _rows.Count; i++) if (_rows[i].label == label) return i;
            return -1;
        }

        void GoToLab()
        {
            // The run was saved at the start of this floor; it carries on from there next time.
            SaveSystem.Save();
            RunSave run = SaveSystem.Data.run;
            if (run.active) RunDirector.LastRunMessage = $"Run saved at the start of floor {run.floorNumber}. The portal will take you back to it.";
            Close();
            Fade.LoadScene(labScene);
        }

        Text _carrying;

        /// <summary>The words down the left of the pause menu: the level, then each item carried and what it does.</summary>
        public static string CarryingText()
        {
            var lines = new System.Text.StringBuilder();
            lines.Append(Progress.Summary()).Append("\n\n");
            var player = PlayerController.Instance;
            var items = player != null ? player.Items : null;
            if (FloorManager.Instance == null || items == null) return lines.ToString().TrimEnd();
            if (items.Collected.Count == 0) return lines.Append("CARRYING\nNothing yet.").ToString();
            lines.Append("CARRYING\n");
            var seen = new System.Collections.Generic.List<ItemType>();
            foreach (var item in items.Collected)
            {
                if (seen.Contains(item.type)) continue;
                seen.Add(item.type);
                int count = items.Count(item.type);
                lines.Append(count > 1 ? $"{item.displayName} ×{count}" : item.displayName).Append(": ").Append(item.description).Append("\n");
            }
            return lines.ToString().TrimEnd();
        }

        void Refresh()
        {
            if (_carrying != null) _carrying.text = CarryingText();
            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                if (row.rect != null && row.rect.gameObject.activeSelf != (row.page == _page)) row.rect.gameObject.SetActive(row.page == _page);
                row.text.text = row.value != null ? $"{row.label}:  {row.value()}" : row.label;
                row.background.color = i == _selected ? new Color(0.45f, 0.95f, 1f, 0.35f) : new Color(1f, 1f, 1f, 0.1f);
            }
        }

        // Pushes the options onto the things they control. Runs at start and whenever one changes.
        // Each scaler's own size as built, so the text size setting always works from the same starting point.
        static readonly Dictionary<CanvasScaler, Vector2> BuiltSizes = new Dictionary<CanvasScaler, Vector2>();

        /// <summary>Applies the text size to every canvas on screen.</summary>
        public static void FitText()
        {
            float size = GameSettings.TextSize;
            foreach (var scaler in FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None))
            {
                if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
                if (!BuiltSizes.TryGetValue(scaler, out var built))
                {
                    built = scaler.referenceResolution;
                    BuiltSizes[scaler] = built;
                }
                scaler.referenceResolution = built / size;
            }
        }

        System.Collections.IEnumerator FitTextSoon()
        {
            // The radio and the cards build their own canvases as the scene starts.
            yield return new WaitForSecondsRealtime(0.5f);
            FitText();
        }

        void Apply()
        {
            AudioListener.volume = GameSettings.Volume;
            FitText();

            var follow = CameraFollow.Instance != null ? CameraFollow.Instance : FindAnyObjectByType<CameraFollow>();
            if (follow != null)
            {
                follow.lockVertical = GameSettings.LockVertical;
                if (follow.mode != GameSettings.Camera) follow.SetMode(GameSettings.Camera);
            }

            var touch = FindAnyObjectByType<TouchControls>();
            if (touch != null) touch.leftHanded = GameSettings.LeftHanded;

            var player = PlayerController.Instance != null ? PlayerController.Instance : FindAnyObjectByType<PlayerController>();
            if (player != null && player.weapon != null) player.weapon.aimAssistStrength = GameSettings.AimAssist;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            bool toggle = (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame))
                          || (pad != null && pad.startButton.wasPressedThisFrame);

            // Where the pointer went down this frame, if it did.
            Vector2? press = null;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) press = mouse.position.ReadValue();
            var screen = Touchscreen.current;
            if (screen != null && screen.primaryTouch.press.wasPressedThisFrame) press = screen.primaryTouch.position.ReadValue();

            if (!_open)
            {
                bool tapped = press.HasValue && _pauseButton.gameObject.activeInHierarchy
                              && RectTransformUtility.RectangleContainsScreenPoint(_pauseButton, press.Value, null);
                if (toggle || tapped) Open();
                return;
            }

            if (toggle)
            {
                Close();
                return;
            }

            if (press.HasValue)
            {
                for (int i = 0; i < _rows.Count; i++)
                {
                    if (_rows[i].page != _page) continue;
                    if (RectTransformUtility.RectangleContainsScreenPoint(_rows[i].rect, press.Value, null))
                    {
                        _selected = i;
                        Use(i);
                        return;
                    }
                }
            }

            // Keyboard and gamepad: up and down to choose, Enter or the south button to use.
            int move = 0;
            if (keyboard != null)
            {
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) move++;
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) move--;
            }
            if (pad != null)
            {
                if (pad.dpad.down.wasPressedThisFrame) move++;
                if (pad.dpad.up.wasPressedThisFrame) move--;
            }
            if (move != 0)
            {
                // Round the rows of the page that is showing.
                for (int step = 0; step < _rows.Count; step++)
                {
                    _selected = (_selected + move + _rows.Count) % _rows.Count;
                    if (_rows[_selected].page == _page) break;
                }
                Refresh();
            }

            bool confirm = (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                           || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            if (confirm) Use(_selected);
        }
    }
}
