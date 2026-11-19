using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Shrunk
{
    /// <summary>
    /// Arrows at the edge of the screen pointing at enemies the camera can't see, so nothing hits the player from
    /// nowhere. Kept modest on purpose: only an enemy that is close, one winding up a charge, or a boss gets one.
    /// Holding a Compound Eye shows every enemy in reach. A setting turns the lot off. It makes its own arrows, so it
    /// needs nothing set up in the scene beyond sitting on the HUD's canvas.
    /// </summary>
    public class EnemyArrows : MonoBehaviour
    {
        public static EnemyArrows Instance { get; private set; }

        [Tooltip("Most arrows shown at once; the nearest enemies win.")]
        public int maxArrows = 10;
        [Tooltip("How far in from the screen edge the arrows sit, in canvas units.")]
        public float margin = 70f;
        [Tooltip("An unseen enemy this close gets an arrow.")]
        public float nearRange = 10f;
        [Tooltip("With a Compound Eye: every unseen enemy this close.")]
        public float eyeRange = 45f;
        public Color color = new Color(1f, 0.6f, 0.2f, 0.8f);
        [Tooltip("An enemy winding up a charge.")]
        public Color dangerColor = new Color(1f, 0.2f, 0.15f, 0.95f);
        public Color bossColor = new Color(1f, 0.8f, 0.2f, 1f);

        readonly List<RectTransform> _arrows = new List<RectTransform>();
        readonly List<Enemy> _offscreen = new List<Enemy>();
        RectTransform _canvas;
        Sprite _sprite;

        public int Showing { get; private set; }

        void Awake()
        {
            Instance = this;
            _canvas = (RectTransform)GetComponentInParent<Canvas>().transform;
            _sprite = MakeArrow();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // A filled triangle pointing up, drawn once: there is no art for it yet.
        static Sprite MakeArrow()
        {
            const int size = 48;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                // Widest at the bottom, a point at the top.
                float half = (1f - y / (float)(size - 1)) * size * 0.5f;
                for (int x = 0; x < size; x++)
                {
                    float off = Mathf.Abs(x + 0.5f - size * 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(half - off) * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        RectTransform Arrow(int index)
        {
            while (_arrows.Count <= index)
            {
                var go = new GameObject("Enemy Arrow", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)go.transform;
                rect.SetParent(transform, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                var image = go.GetComponent<Image>();
                image.sprite = _sprite;
                image.raycastTarget = false;
                _arrows.Add(rect);
            }
            return _arrows[index];
        }

        /// <summary>Whether this enemy is one the player should be warned about when it is out of view.</summary>
        public bool Matters(Enemy enemy, PlayerController player)
        {
            if (enemy.Health.IsDead || !enemy.Targetable || enemy is EggEnemy) return false;
            if (enemy is BossEnemy || enemy.ChargeLineShowing) return true;
            Vector3 to = enemy.transform.position - player.transform.position;
            to.y = 0f;
            bool eye = player.Items != null && player.Items.Count(ItemType.CompoundEye) > 0;
            return to.magnitude <= (eye ? eyeRange : nearRange);
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            var player = PlayerController.Instance;
            int shown = 0;

            if (GameSettings.EnemyArrows && cam != null && player != null && !PauseMenu.IsOpen)
            {
                _offscreen.Clear();
                foreach (var enemy in Enemy.Active)
                {
                    if (!Matters(enemy, player)) continue;
                    Vector3 view = cam.WorldToViewportPoint(enemy.transform.position + Vector3.up * 0.7f);
                    bool visible = view.z > 0f && view.x > 0.02f && view.x < 0.98f && view.y > 0.02f && view.y < 0.98f;
                    if (!visible) _offscreen.Add(enemy);
                }

                Vector3 from = player.transform.position;
                _offscreen.Sort((a, b) => (a.transform.position - from).sqrMagnitude.CompareTo((b.transform.position - from).sqrMagnitude));

                Vector2 half = _canvas.rect.size * 0.5f - Vector2.one * margin;
                for (int i = 0; i < _offscreen.Count && shown < maxArrows; i++)
                {
                    Enemy enemy = _offscreen[i];

                    // Direction from the player to the enemy as the camera sees it: right is right, ahead is up.
                    Vector3 local = cam.transform.InverseTransformDirection(enemy.transform.position - from);
                    var direction = new Vector2(local.x, local.z);
                    if (direction.sqrMagnitude < 0.0001f) continue;
                    direction.Normalize();

                    // Push the arrow out along that direction until it meets the screen edge.
                    float scale = Mathf.Min(half.x / Mathf.Max(0.001f, Mathf.Abs(direction.x)), half.y / Mathf.Max(0.001f, Mathf.Abs(direction.y)));

                    RectTransform arrow = Arrow(shown++);
                    bool boss = enemy is BossEnemy;
                    bool danger = enemy.ChargeLineShowing;
                    arrow.gameObject.SetActive(true);
                    arrow.anchoredPosition = direction * scale;
                    // A charge being wound up is bigger, red and pulsing.
                    float size = boss ? 70f : danger ? 56f + 8f * Mathf.Sin(Time.unscaledTime * 14f) : 40f;
                    arrow.sizeDelta = new Vector2(size, size);
                    arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
                    arrow.GetComponent<Image>().color = boss ? bossColor : danger ? dangerColor : color;
                }
            }

            for (int i = shown; i < _arrows.Count; i++) _arrows[i].gameObject.SetActive(false);
            Showing = shown;
        }
    }
}
