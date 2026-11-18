using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Shrunk
{
    /// <summary>
    /// Markers at the edge of the screen pointing at enemies the camera can't see, so a swarm can't sneak up from behind.
    /// Bosses get a bigger marker. It makes its own markers, so it needs nothing set up in the scene.
    /// </summary>
    public class EnemyArrows : MonoBehaviour
    {
        [Tooltip("Most markers shown at once; the nearest enemies win.")]
        public int maxArrows = 10;
        [Tooltip("How far in from the screen edge the markers sit, in canvas units.")]
        public float margin = 70f;
        public Color color = new Color(1f, 0.3f, 0.25f, 0.9f);
        public Color bossColor = new Color(1f, 0.8f, 0.2f, 1f);

        readonly List<RectTransform> _arrows = new List<RectTransform>();
        readonly List<Enemy> _offscreen = new List<Enemy>();
        RectTransform _canvas;

        public int Showing { get; private set; }

        void Awake()
        {
            _canvas = (RectTransform)GetComponentInParent<Canvas>().transform;
        }

        RectTransform Arrow(int index)
        {
            while (_arrows.Count <= index)
            {
                var go = new GameObject("Enemy Arrow", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)go.transform;
                rect.SetParent(transform, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                go.GetComponent<Image>().raycastTarget = false;
                _arrows.Add(rect);
            }
            return _arrows[index];
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            var player = PlayerController.Instance;
            int shown = 0;

            if (cam != null && player != null && !PauseMenu.IsOpen)
            {
                _offscreen.Clear();
                foreach (var enemy in Enemy.Active)
                {
                    if (enemy.Health.IsDead || !enemy.Targetable || enemy is EggEnemy) continue;
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

                    // Push the marker out along that direction until it meets the screen edge.
                    float scale = Mathf.Min(half.x / Mathf.Max(0.001f, Mathf.Abs(direction.x)), half.y / Mathf.Max(0.001f, Mathf.Abs(direction.y)));

                    RectTransform arrow = Arrow(shown++);
                    bool boss = enemy is BossEnemy;
                    arrow.gameObject.SetActive(true);
                    arrow.anchoredPosition = direction * scale;
                    arrow.sizeDelta = boss ? new Vector2(26f, 70f) : new Vector2(16f, 44f);
                    arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
                    arrow.GetComponent<Image>().color = boss ? bossColor : color;
                }
            }

            for (int i = shown; i < _arrows.Count; i++) _arrows[i].gameObject.SetActive(false);
            Showing = shown;
        }
    }
}
