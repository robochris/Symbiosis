using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Shrunk
{
    /// <summary>
    /// The game's look for menus and the HUD, in one place: the colours, the drawn art in Resources/UI (rounded panels,
    /// soft shadows, rings, hearts, icons, the logo), the font, and helpers that put them together. Anything built
    /// with these matches everything else.
    /// </summary>
    public static class UiKit
    {
        // ----- colours -----
        /// <summary>Deep blue-black for panels.</summary>
        public static readonly Color Ink = new Color(0.055f, 0.075f, 0.12f, 0.86f);
        public static readonly Color InkSolid = new Color(0.055f, 0.075f, 0.12f, 1f);
        /// <summary>A lighter panel, for a row or a card inside a panel.</summary>
        public static readonly Color InkRaised = new Color(0.13f, 0.17f, 0.25f, 0.92f);
        /// <summary>Warm white, for words.</summary>
        public static readonly Color Cream = new Color(1f, 0.97f, 0.9f, 1f);
        public static readonly Color Dim = new Color(0.72f, 0.76f, 0.84f, 1f);
        /// <summary>The game's own cyan.</summary>
        public static readonly Color Accent = new Color(0.42f, 0.9f, 1f, 1f);
        public static readonly Color Gold = new Color(1f, 0.79f, 0.28f, 1f);
        public static readonly Color Coral = new Color(1f, 0.38f, 0.34f, 1f);
        public static readonly Color Leaf = new Color(0.5f, 0.9f, 0.45f, 1f);
        /// <summary>A faint light edge round a panel.</summary>
        public static readonly Color Edge = new Color(1f, 1f, 1f, 0.09f);

        // ----- art -----
        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        /// <summary>One of the drawn pieces in Resources/UI, by file name; null if it is missing.</summary>
        public static Sprite Art(string name)
        {
            if (Sprites.TryGetValue(name, out var sprite) && sprite != null) return sprite;
            sprite = Resources.Load<Sprite>("UI/" + name);
            Sprites[name] = sprite;
            return sprite;
        }

        // ----- the font -----
        static Font _font, _legacy;

        /// <summary>The built-in font, which the game's text was made with.</summary>
        public static Font Legacy => _legacy != null ? _legacy : (_legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        /// <summary>
        /// A cleaner font than the built-in one: the phone's own (Roboto on Android) or Segoe UI on Windows. Falls back to
        /// the built-in font if neither is there.
        /// </summary>
        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                _font = Legacy;
                try
                {
                    var installed = new HashSet<string>(Font.GetOSInstalledFontNames());
                    foreach (string name in new[] { "Roboto", "Segoe UI", "Helvetica Neue", "Noto Sans" })
                    {
                        if (!installed.Contains(name)) continue;
                        var os = Font.CreateDynamicFontFromOSFont(name, 32);
                        if (os != null) { _font = os; break; }
                    }
                }
                catch (System.Exception)
                {
                    // No list of fonts on this device: the built-in one is fine.
                }
                return _font;
            }
        }

        /// <summary>For tests: use only the built-in font, so text measures the same everywhere.</summary>
        public static bool LegacyOnly;

        /// <summary>The font new text should use.</summary>
        public static Font TextFont => LegacyOnly ? Legacy : Font;

        // ----- building -----
        static RectTransform Child(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Fills its parent, with a margin (positive = inside, negative = outside).</summary>
        public static RectTransform Fill(RectTransform rect, float margin = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
            return rect;
        }

        /// <summary>An image of one of the drawn pieces, sliced if it is a panel.</summary>
        public static Image Image(Transform parent, string art, Color color, string name = null)
        {
            var rect = Child(name ?? art, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Art(art);
            image.color = color;
            image.raycastTarget = false;
            if (image.sprite != null && image.sprite.border != Vector4.zero) image.type = UnityEngine.UI.Image.Type.Sliced;
            return image;
        }

        /// <summary>
        /// Dresses a rect as a rounded panel: a soft shadow under it, the fill, and a faint light edge. The pieces are
        /// its first children, so anything added after them sits on top.
        /// </summary>
        public static Image Dress(RectTransform rect, Color fill, bool shadow = true, bool edge = true, float corner = 1f)
        {
            if (shadow)
            {
                var s = Image(rect, "shadow", new Color(0f, 0f, 0f, 0.55f), "Shadow");
                Fill(s.rectTransform, -38f);
                s.rectTransform.anchoredPosition = new Vector2(0f, -10f);
                s.pixelsPerUnitMultiplier = 1f;
            }
            var body = Image(rect, "panel", fill, "Fill");
            Fill(body.rectTransform);
            body.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.2f, corner);
            if (edge)
            {
                var e = Image(rect, "panel_edge", Edge, "Edge");
                Fill(e.rectTransform);
                e.pixelsPerUnitMultiplier = body.pixelsPerUnitMultiplier;
            }
            return body;
        }

        /// <summary>A new rounded panel.</summary>
        public static RectTransform Panel(Transform parent, string name, Color fill, bool shadow = true)
        {
            var rect = Child(name, parent);
            Dress(rect, fill, shadow);
            return rect;
        }

        /// <summary>A line of text in the game's style: the clean font and a soft shadow under it.</summary>
        public static Text Label(Transform parent, string name, string content, int size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter, bool bold = false)
        {
            var rect = Child(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = TextFont;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            SoftShadow(text);
            return text;
        }

        /// <summary>A soft dark shadow under text, instead of a hard outline round it.</summary>
        public static void SoftShadow(Graphic graphic, float alpha = 0.55f)
        {
            var go = graphic.gameObject;
            var hard = go.GetComponent<Outline>();
            if (hard != null) Object.Destroy(hard);
            var shadow = go.GetComponent<Shadow>();
            if (shadow == null || shadow is Outline) shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
            shadow.effectDistance = new Vector2(0f, -3f);
        }

        /// <summary>Puts a rect at an anchor, with a size and an offset from it.</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>Text that was made with the built-in font gets the clean one. Safe to call again.</summary>
        public static void Adopt(Text text)
        {
            if (text == null || LegacyOnly) return;
            var font = Font;
            if (text.font == Legacy && font != Legacy) text.font = font;
        }
    }
}
