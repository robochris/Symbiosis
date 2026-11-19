using System;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The words floating over a portal: where it goes and what going there is worth, readable from across the room
    /// so the choice can be made without walking up to each one. Always faces the camera.
    /// </summary>
    public class PortalTag : MonoBehaviour
    {
        public Func<string> text;

        TextMesh _mesh;
        float _nextRefresh;

        public string Shown => _mesh != null ? _mesh.text : "";

        public static PortalTag Create(Transform portal, Func<string> text, Color color, float height = 4.1f)
        {
            var go = new GameObject("Portal Tag");
            go.transform.SetParent(portal, false);
            go.transform.localPosition = new Vector3(0f, height, 0f);

            var mesh = go.AddComponent<TextMesh>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            mesh.fontSize = 64;
            mesh.characterSize = 0.06f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;

            var tag = go.AddComponent<PortalTag>();
            tag.text = text;
            tag._mesh = mesh;
            tag.Refresh();
            return tag;
        }

        void Refresh()
        {
            if (_mesh != null && text != null) _mesh.text = text();
        }

        void LateUpdate()
        {
            // What a portal is worth changes as samples are picked up.
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 0.25f;
                Refresh();
            }

            var cam = Camera.main;
            if (cam == null) return;
            // Flat to the screen, so the words are never skewed.
            transform.rotation = cam.transform.rotation;
        }
    }
}
