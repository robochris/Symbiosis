using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// A dare: the bell in the middle of the dare room, a quiet dead end marked in red on the map. Stand on its mat
    /// for a moment and the doors shut on a wave bigger than an ordinary room's, with an elite in it; beat that and
    /// an item is left where the bell was. It is never rung by walking past: it takes standing still. Nothing is
    /// asked of the player first: any health, any time, or never. Made by the floor.
    /// </summary>
    public class DareBell : MonoBehaviour
    {
        /// <summary>How long the player must stand on the mat. Long enough that crossing the room never rings it.</summary>
        public const float HoldTime = 1.1f;

        public Room room;
        public float radius = 1.1f;

        float _held;
        Transform _bell;

        /// <summary>How far through ringing it the player is, 0 to 1.</summary>
        public float Progress => Mathf.Clamp01(_held / HoldTime);

        public static DareBell Create(Room room, Vector3 position, Material matMaterial, Material bellMaterial)
        {
            var go = new GameObject("Dare Bell");
            go.transform.SetParent(room.transform, false);
            go.transform.position = position;
            Part(PrimitiveType.Cylinder, "Mat", go.transform, new Vector3(0f, 0.02f, 0f), new Vector3(2.2f, 0.02f, 2.2f), matMaterial);
            var bell = Part(PrimitiveType.Sphere, "Bell", go.transform, new Vector3(0f, 2.3f, 0f), new Vector3(0.9f, 0.7f, 0.9f), bellMaterial);
            Part(PrimitiveType.Cylinder, "Clapper", bell, new Vector3(0f, -0.6f, 0f), new Vector3(0.2f, 0.25f, 0.2f), matMaterial);

            var dare = go.AddComponent<DareBell>();
            dare.room = room;
            dare._bell = bell;
            PortalTag.Create(go.transform, () => "DARE", new Color(1f, 0.45f, 0.35f), 3.4f);
            return dare;
        }

        static Transform Part(PrimitiveType shape, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var renderer = part.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (material != null) renderer.sharedMaterial = material;
            return part.transform;
        }

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var player = PlayerController.Instance;
            var floors = FloorManager.Instance;
            if (player == null || floors == null) return;

            Vector3 to = player.transform.position - transform.position;
            to.y = 0f;
            bool on = to.magnitude <= radius && !floors.IsLocked;
            _held = on ? _held + Time.deltaTime : 0f;
            // The bell swings harder the closer it is to ringing.
            if (_bell != null) _bell.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 14f) * 22f * Progress, 0f, 0f);
            if (_held >= HoldTime) Ring();
        }

        /// <summary>Rings it: the second wave comes. Returns false if the floor would not start one.</summary>
        public bool Ring()
        {
            var floors = FloorManager.Instance;
            _held = 0f;
            return floors != null && floors.StartDare(this);
        }

        string Prompt(Vector3 playerPosition)
        {
            Vector3 to = playerPosition - transform.position;
            to.y = 0f;
            if (to.magnitude > 3.2f) return null;
            return "A dare: ring it and the doors shut on a fight worse than most.\nBeat that and there is a prize. Stand still on the mat to ring it.";
        }
    }
}
