using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shrunk
{
    /// <summary>
    /// What marks a thing you can use. The coloured mats on the floor are hidden, and in their place a small diamond
    /// in the mat's colour floats over the thing, turning slowly. Walk up to it and it grows, brightens and shows the
    /// key to press (E, or USE on a phone). Nothing has to be built differently: anything with a "Mat" under it, or a
    /// "Pad" under a lab station, gets one, in the lab and on every floor of a run.
    /// </summary>
    public class UseMarkers : MonoBehaviour
    {
        static UseMarkers _instance;
        readonly HashSet<int> _done = new HashSet<int>();
        int _scanAt = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (_instance != null) return;
            var go = new GameObject("Use markers");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<UseMarkers>();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            FloorManager.FloorBuilt += OnFloorBuilt;
            _scanAt = Time.frameCount + 1;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            FloorManager.FloorBuilt -= OnFloorBuilt;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _done.Clear();
            _scanAt = Time.frameCount + 1;
        }

        // Shops, bells and claw machines are put down as the floor is built: look again a frame later.
        void OnFloorBuilt(FloorManager floors) => _scanAt = Time.frameCount + 1;

        /// <summary>Looks again for mats without a marker: for things that turn up in the middle of a floor.</summary>
        public static void Rescan()
        {
            if (_instance != null) _instance._scanAt = Time.frameCount + 1;
        }

        void Update()
        {
            if (_scanAt < 0 || Time.frameCount < _scanAt) return;
            _scanAt = -1;
            Scan();
        }

        void Scan()
        {
            foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (renderer == null || !_done.Add(renderer.GetInstanceID())) continue;
                string name = renderer.gameObject.name;
                Transform owner = renderer.transform.parent;
                if (owner == null) continue;
                if (name == "Mat Rim")
                {
                    renderer.enabled = false;
                    continue;
                }
                bool mat = name == "Mat" || (name == "Pad" && owner.name.EndsWith(" Pad"));
                if (!mat) continue;
                renderer.enabled = false;
                UseMarker.Add(owner, renderer);
            }
        }
    }

    /// <summary>One floating diamond over one usable thing: a flat glowing icon with a white edge, always turned to the camera.</summary>
    public class UseMarker : MonoBehaviour
    {
        /// <summary>Every marker, for tests.</summary>
        public static readonly List<UseMarker> All = new List<UseMarker>();

        Transform _icon;
        TextMesh _key;
        Material _material;
        Color _colour;
        float _reach, _height, _phase, _near;

        public bool InReach { get; private set; }

        public static UseMarker Add(Transform owner, Renderer mat)
        {
            var go = new GameObject("Use marker");
            go.transform.SetParent(owner, false);
            go.transform.position = new Vector3(mat.transform.position.x, owner.position.y, mat.transform.position.z);
            var marker = go.AddComponent<UseMarker>();
            marker.Build(owner, mat);
            return marker;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Build(Transform owner, Renderer mat)
        {
            _reach = mat.transform.lossyScale.x * 0.5f + Interact.Reach;
            _phase = Random.value * 10f;

            // Over the top of whatever the mat was in front of, but never up in the rafters.
            float top = 1.2f;
            foreach (var other in owner.GetComponentsInChildren<Renderer>())
            {
                if (other == mat || !other.enabled || other is ParticleSystemRenderer) continue;
                top = Mathf.Max(top, other.bounds.max.y - owner.position.y);
            }
            _height = Mathf.Min(top + 0.6f, 4.2f);

            _colour = mat.sharedMaterial != null && mat.sharedMaterial.HasProperty("_BaseColor") ? mat.sharedMaterial.GetColor("_BaseColor")
                : mat.sharedMaterial != null ? mat.sharedMaterial.color : Color.white;
            // Black (the hidden door's mat) would be invisible: a pale grey instead.
            if (_colour.maxColorComponent < 0.25f) _colour = new Color(0.8f, 0.8f, 0.85f);
            // Unlit, so it glows the same in a dark attic as in a bright kitchen.
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            _material = unlit != null ? new Material(unlit) : new Material(mat.sharedMaterial);
            SetColour(0.75f);
            var edge = unlit != null ? new Material(unlit) : new Material(mat.sharedMaterial);
            if (edge.HasProperty("_BaseColor")) edge.SetColor("_BaseColor", Color.white);
            edge.color = Color.white;

            // The icon: turned to the camera, a white diamond with the coloured one just in front of it.
            var icon = new GameObject("Icon");
            icon.transform.SetParent(transform, false);
            icon.AddComponent<FaceCamera>();
            _icon = icon.transform;
            Diamond("Edge", icon.transform, 0.36f, 0f, edge);
            Diamond("Fill", icon.transform, 0.27f, -0.02f, _material);

            var label = new GameObject("Key");
            label.transform.SetParent(transform, false);
            _key = label.AddComponent<TextMesh>();
            _key.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<MeshRenderer>().sharedMaterial = _key.font.material;
            _key.text = "E";
            _key.anchor = TextAnchor.MiddleCenter;
            _key.alignment = TextAlignment.Center;
            _key.fontSize = 64;
            _key.characterSize = 0.05f;
            _key.fontStyle = FontStyle.Bold;
            _key.color = Color.white;
            label.AddComponent<FaceCamera>();
            label.SetActive(false);
            Place(0f);
        }

        static void Diamond(string name, Transform parent, float size, float forward, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = new Vector3(0f, 0f, forward);
            part.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            part.transform.localScale = new Vector3(size, size, 0.02f);
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        void SetColour(float brightness)
        {
            Color c = Color.Lerp(_colour, Color.white, Mathf.Clamp01(brightness - 0.75f) * 0.9f);
            c.a = 1f;
            if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", c);
            else _material.color = c;
        }

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            Vector3 to = player.transform.position - transform.position;
            to.y = 0f;
            InReach = to.magnitude <= _reach;
            _near = Mathf.MoveTowards(_near, InReach ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            Place(_near);
            SetColour(0.75f + 0.5f * _near);
            if (_key.gameObject.activeSelf != InReach)
            {
                _key.gameObject.SetActive(InReach);
                _key.text = Interact.OnTouch ? "USE" : "E";
            }
        }

        void Place(float near)
        {
            float t = Time.time + _phase;
            float bob = Mathf.Sin(t * 2.2f) * 0.12f;
            _icon.localPosition = new Vector3(0f, _height + bob, 0f);
            // A slow breath while waiting; bigger once in reach.
            float pulse = 1f + Mathf.Sin(t * 3f) * 0.06f * (1f - near);
            _icon.localScale = Vector3.one * Mathf.Lerp(1f, 1.35f, near) * pulse;
            _key.transform.localPosition = new Vector3(0f, _height + 0.55f, 0f);
        }
    }
}
