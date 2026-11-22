using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Shrunk
{
    /// <summary>
    /// The build at a glance: a small square in each carried item's colour, under the run's counters, boss items in a
    /// gold frame. It is only there during a run.
    /// </summary>
    public class ItemStrip : MonoBehaviour
    {
        const int PerRow = 12;
        const float Size = 16f, Gap = 4f;
        static ItemStrip _instance;
        RectTransform _root;
        readonly List<GameObject> _squares = new List<GameObject>();
        int _shown = -1;
        float _next;

        /// <summary>How many squares are up, for tests.</summary>
        public static int Shown => _instance != null ? _instance._shown : 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (_instance != null) return;
            var go = new GameObject("Item strip");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ItemStrip>();
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;
            var root = new GameObject("Row", typeof(RectTransform));
            root.transform.SetParent(go.transform, false);
            _instance._root = (RectTransform)root.transform;
            _instance._root.anchorMin = _instance._root.anchorMax = _instance._root.pivot = new Vector2(0f, 1f);
            _instance._root.anchoredPosition = new Vector2(38f, -188f);
        }

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.25f;
            var player = PlayerController.Instance;
            bool inRun = player != null && player.Items != null && FloorManager.Instance != null;
            _root.gameObject.SetActive(inRun);
            if (!inRun) { _shown = -1; return; }
            var items = player.Items.Collected;
            if (items.Count == _shown) return;
            _shown = items.Count;
            foreach (var square in _squares) Destroy(square);
            _squares.Clear();
            var bossItems = new HashSet<ItemType>(BossEnemy.Trophies.Values);
            for (int i = 0; i < items.Count && i < PerRow * 3; i++)
            {
                var item = items[i];
                Vector2 at = new Vector2((i % PerRow) * (Size + Gap), -(i / PerRow) * (Size + Gap));
                if (bossItems.Contains(item.type)) _squares.Add(Square(at, Size + 4f, new Color(1f, 0.8f, 0.25f)));
                _squares.Add(Square(at, Size, item.material != null ? item.material.color : Color.white));
            }
        }

        GameObject Square(Vector2 at, float size, Color colour)
        {
            var go = new GameObject("Item", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_root, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * size;
            rect.anchoredPosition = at + new Vector2(Size * 0.5f, -Size * 0.5f);
            var image = go.GetComponent<Image>();
            colour.a = 1f;
            image.color = colour;
            image.raycastTarget = false;
            return go;
        }
    }
}
