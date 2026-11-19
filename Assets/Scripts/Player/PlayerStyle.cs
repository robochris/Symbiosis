using System;
using UnityEngine;

namespace Shrunk
{
    /// <summary>One look for the Void: what its body and its eyes are made of.</summary>
    [Serializable]
    public class VoidStyle
    {
        public string displayName = "Shadow";
        public Material body;
        public Material eyes;
    }

    /// <summary>
    /// The Void's character style: a full swap of what the body and eyes are made of, chosen at the mirror in the lab
    /// and remembered between runs. Items are separate attachments, so every item look works on every style.
    /// </summary>
    public class PlayerStyle : MonoBehaviour
    {
        [Tooltip("The first one is the look the body is built with.")]
        public VoidStyle[] styles;

        public int Current { get; private set; }
        public VoidStyle CurrentStyle => styles != null && styles.Length > 0 ? styles[Current] : null;

        Renderer[] _body = new Renderer[0];
        Renderer[] _eyes = new Renderer[0];

        void Awake()
        {
            if (styles == null || styles.Length == 0) return;

            // The body is whatever was built from the first style's materials. Anything added later (item looks) is left alone.
            var body = new System.Collections.Generic.List<Renderer>();
            var eyes = new System.Collections.Generic.List<Renderer>();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial == styles[0].body) body.Add(renderer);
                else if (renderer.sharedMaterial == styles[0].eyes) eyes.Add(renderer);
            }
            _body = body.ToArray();
            _eyes = eyes.ToArray();
        }

        void Start()
        {
            int saved = SaveSystem.Data.meta.style;
            Apply(Unlocked(saved) ? saved : 0);
        }

        /// <summary>Wears a style. Out-of-range numbers wrap round.</summary>
        public void Apply(int index)
        {
            if (styles == null || styles.Length == 0) return;
            Current = ((index % styles.Length) + styles.Length) % styles.Length;
            VoidStyle style = styles[Current];
            foreach (var renderer in _body) if (renderer != null && style.body != null) renderer.sharedMaterial = style.body;
            foreach (var renderer in _eyes) if (renderer != null && style.eyes != null) renderer.sharedMaterial = style.eyes;
        }

        public bool Unlocked(int index) => styles != null && index >= 0 && index < styles.Length && Achievements.StyleUnlocked(styles[index].displayName);

        public int UnlockedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; styles != null && i < styles.Length; i++) if (Unlocked(i)) count++;
                return count;
            }
        }

        /// <summary>Steps to the next style the player has unlocked, and remembers it.</summary>
        public void Next()
        {
            int next = Current;
            for (int i = 0; i < styles.Length; i++)
            {
                next = (next + 1) % styles.Length;
                if (Unlocked(next)) break;
            }
            Apply(next);
            SaveSystem.Data.meta.style = Current;
            SaveSystem.Save();
        }
    }
}
