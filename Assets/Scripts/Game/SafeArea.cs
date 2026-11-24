using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Keeps the HUD clear of a phone's notch and rounded corners. The world still fills the whole screen; anything
    /// pinned to an edge of a screen canvas is nudged in by that edge's unsafe strip. Full-screen panels and fades are
    /// left alone. Nothing moves on a screen that has no unsafe strips, like a PC monitor.
    /// </summary>
    public class SafeArea : MonoBehaviour
    {
        /// <summary>For tests: pretend the safe area is this, in pixels. Null = the real one.</summary>
        public static Rect? Override;

        static SafeArea _instance;
        readonly Dictionary<RectTransform, (Vector2 pos, Vector2 min, Vector2 max)> _home = new Dictionary<RectTransform, (Vector2, Vector2, Vector2)>();
        Rect _lastSafe;
        Vector2Int _lastSize;
        float _next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("Safe Area");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SafeArea>();
            // Before Start runs in the new scene, so nothing reads a position that is about to move.
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => { if (_instance != null) _instance.Fit(true); };
            _instance.Fit(true);
        }

        public static Rect Safe => Override ?? Screen.safeArea;

        /// <summary>After anything on a screen canvas was moved to fit.</summary>
        public static event System.Action Moved;

        /// <summary>Fit every screen canvas now, instead of on the next check.</summary>
        public static void FitNow()
        {
            if (_instance == null) Boot();
            else _instance.Fit(true);
        }

        void LateUpdate()
        {
            // Canvases come and go with scenes and cards, so look twice a second; real time, so a pause still fits.
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            Fit(false);
        }

        void Fit(bool force)
        {
            var safe = Safe;
            var size = new Vector2Int(Screen.width, Screen.height);
            bool changed = force || safe != _lastSafe || size != _lastSize;
            _lastSafe = safe;
            _lastSize = size;
            // The unsafe strip along each edge, in pixels.
            float left = Mathf.Max(0f, safe.xMin), right = Mathf.Max(0f, size.x - safe.xMax);
            float bottom = Mathf.Max(0f, safe.yMin), top = Mathf.Max(0f, size.y - safe.yMax);

            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
                float scale = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
                var root = canvas.transform;
                for (int i = 0; i < root.childCount; i++)
                {
                    if (!(root.GetChild(i) is RectTransform rect)) continue;
                    bool known = _home.TryGetValue(rect, out var home);
                    if (known && !changed) continue;
                    if (!known)
                    {
                        home = (rect.anchoredPosition, rect.offsetMin, rect.offsetMax);
                        _home[rect] = home;
                    }
                    Place(rect, home, left / scale, right / scale, bottom / scale, top / scale);
                }
            }
            if (changed)
            {
                Prune();
                Moved?.Invoke();
            }
        }

        void Prune()
        {
            var dead = new List<RectTransform>();
            foreach (var rect in _home.Keys) if (rect == null) dead.Add(rect);
            foreach (var rect in dead) _home.Remove(rect);
        }

        static void Place(RectTransform rect, (Vector2 pos, Vector2 min, Vector2 max) home, float left, float right, float bottom, float top)
        {
            // Anything held to one side of the screen slides in by that side's strip. Something that spans an axis, like
            // a banner across the screen or a fade, keeps spanning it: its background should still reach the edges.
            Vector2 aMin = rect.anchorMin, aMax = rect.anchorMax;
            Vector2 shift = Vector2.zero;
            if (aMax.x < 0.5f) shift.x = left;
            else if (aMin.x > 0.5f) shift.x = -right;
            if (aMax.y < 0.5f) shift.y = bottom;
            else if (aMin.y > 0.5f) shift.y = -top;
            rect.offsetMin = home.min + shift;
            rect.offsetMax = home.max + shift;
        }
    }
}
