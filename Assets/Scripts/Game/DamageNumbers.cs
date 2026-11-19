using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// A number that floats up off an enemy when a hit does more than the ordinary one point: a critical hit, a
    /// charged shot, a weak point, Overcharge. Ordinary hits show nothing, so the screen stays quiet and a number
    /// means something. It is how a player learns that the back of a boss is the place to shoot. One per scene, on
    /// the Game object; a setting turns it off.
    /// </summary>
    public class DamageNumbers : MonoBehaviour
    {
        public static DamageNumbers Instance { get; private set; }

        [Tooltip("Hits smaller than this show nothing.")]
        public int smallest = 2;
        [Tooltip("Hits this big or bigger are drawn larger and in gold.")]
        public int big = 4;
        public float lifetime = 0.7f;
        public float rise = 1.6f;
        public int most = 24;

        /// <summary>How many have been shown, for tests.</summary>
        public int Shown { get; private set; }
        /// <summary>The last one shown, for tests.</summary>
        public int LastAmount { get; private set; }

        class Number
        {
            public TextMesh mesh;
            public float age;
            public Vector3 from;
            public bool big;
        }

        readonly List<Number> _live = new List<Number>();
        readonly Stack<Number> _spare = new Stack<Number>();
        Font _font;

        void Awake()
        {
            Instance = this;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable() => Enemy.Hurt += OnHurt;
        void OnDisable() => Enemy.Hurt -= OnHurt;

        void OnHurt(Enemy enemy, int amount)
        {
            if (amount < smallest || !GameSettings.DamageNumbers) return;
            Shown++;
            LastAmount = amount;
            if (_live.Count >= most) Retire(0);

            Number number = _spare.Count > 0 ? _spare.Pop() : Make();
            number.age = 0f;
            number.big = amount >= big;
            // Over the enemy's head, a little to one side so two in a row do not sit on top of each other.
            float height = enemy.Mutations != null ? enemy.Mutations.badgeHeight : 2.4f;
            number.from = enemy.transform.position + new Vector3(Random.Range(-0.5f, 0.5f), height * 0.8f, Random.Range(-0.3f, 0.3f));
            number.mesh.text = amount.ToString();
            number.mesh.characterSize = number.big ? 0.11f : 0.075f;
            number.mesh.gameObject.SetActive(true);
            _live.Add(number);
        }

        Number Make()
        {
            var go = new GameObject("Damage Number");
            go.transform.SetParent(transform, false);
            var mesh = go.AddComponent<TextMesh>();
            mesh.font = _font;
            go.GetComponent<MeshRenderer>().sharedMaterial = _font.material;
            mesh.fontSize = 64;
            mesh.fontStyle = FontStyle.Bold;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            return new Number { mesh = mesh };
        }

        void Retire(int index)
        {
            Number number = _live[index];
            _live.RemoveAt(index);
            number.mesh.gameObject.SetActive(false);
            _spare.Push(number);
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                Number number = _live[i];
                number.age += Time.deltaTime;
                float t = number.age / lifetime;
                if (t >= 1f)
                {
                    Retire(i);
                    continue;
                }

                // Up quickly, then slowing; fading out over the second half.
                Transform at = number.mesh.transform;
                at.position = number.from + Vector3.up * (rise * (1f - (1f - t) * (1f - t)));
                if (cam != null) at.rotation = cam.transform.rotation;
                Color colour = number.big ? new Color(1f, 0.82f, 0.2f) : Color.white;
                colour.a = Mathf.Clamp01((1f - t) * 2f);
                number.mesh.color = colour;
            }
        }
    }
}
