using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The jukebox in the lab: use it to play any world's music you have been to, here at home. The lab's own tune is
    /// first on the list.
    /// </summary>
    public class Jukebox : MonoBehaviour
    {
        static readonly string[] Worlds = { "lab", "ants", "cake", "sofa", "fridge", "library", "forest", "clouds", "console", "aquarium", "toybox", "bathroom", "attic", "shed", "bookshelf", "sink" };
        readonly StepPad _pad = new StepPad { radius = 0.9f };
        public static Jukebox Current { get; private set; }
        /// <summary>The tune chosen at the jukebox, for the lab (null: the lab's own).</summary>
        public static string Playing { get; private set; }
        Transform _record;

        static bool Heard(string world) => world == "lab" || world == "ants" || Achievements.EverythingUnlocked || Achievements.BiomeUnlocked(world);

        public static Jukebox Place(Vector3 position)
        {
            var go = new GameObject("Jukebox");
            go.transform.position = position;
            Part.Make(PrimitiveType.Cube, "Cabinet", go.transform, new Vector3(0f, 0.75f, 0f), new Vector3(1f, 1.5f, 0.6f), new Color(0.55f, 0.2f, 0.15f));
            Part.Make(PrimitiveType.Sphere, "Dome", go.transform, new Vector3(0f, 1.5f, 0f), new Vector3(1f, 0.6f, 0.6f), new Color(1f, 0.75f, 0.3f), true);
            var record = Part.Make(PrimitiveType.Cylinder, "Record", go.transform, new Vector3(0f, 0.9f, -0.31f), new Vector3(0.6f, 0.02f, 0.6f), new Color(0.08f, 0.08f, 0.08f));
            record.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part.Make(PrimitiveType.Cylinder, "Mat", go.transform, new Vector3(0f, 0.02f, -0.6f), new Vector3(1.4f, 0.02f, 1.4f), new Color(1f, 0.6f, 0.3f));
            var box = go.AddComponent<Jukebox>();
            box._record = record.transform;
            UseMarkers.Rescan();
            return box;
        }

        void OnEnable()
        {
            Current = this;
            WorldPrompt.Add(Prompt, transform);
            Playing = null;
        }

        void OnDisable()
        {
            if (Current == this) Current = null;
            WorldPrompt.Remove(Prompt);
        }

        void Update()
        {
            if (_record != null) _record.Rotate(0f, 120f * Time.deltaTime, 0f, Space.Self);
            var player = PlayerController.Instance;
            if (player == null || !_pad.SteppedOn(transform.position + transform.rotation * new Vector3(0f, 0f, -0.6f), player.transform.position)) return;
            Next();
        }

        /// <summary>Puts on the next tune you have heard out there.</summary>
        public string Next()
        {
            int at = System.Array.IndexOf(Worlds, Playing ?? "lab");
            for (int i = 1; i <= Worlds.Length; i++)
            {
                string next = Worlds[(at + i) % Worlds.Length];
                if (!Heard(next)) continue;
                Playing = next == "lab" ? null : next;
                break;
            }
            Music.Refresh();
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast("Now playing: " + (Playing ?? "the lab's own"), 2f);
            return Playing ?? "lab";
        }

        string Prompt(Vector3 playerPosition)
        {
            Vector3 to = playerPosition - transform.position;
            to.y = 0f;
            if (to.magnitude > 2.4f) return null;
            return $"The jukebox: any world's tune you have been to. Playing: {Playing ?? "the lab's own"}.\n{Interact.Verb} for the next.";
        }
    }

    /// <summary>
    /// The lab cat: not the one in the shop. It is asleep somewhere different every time you come home. You can't
    /// do anything with it. It is a cat.
    /// </summary>
    public class LabCat : MonoBehaviour
    {
        public static LabCat Place(Vector3 position)
        {
            var go = new GameObject("Lab Cat");
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var fur = new Color(0.95f, 0.6f, 0.25f);
            Part.Make(PrimitiveType.Sphere, "Body", go.transform, new Vector3(0f, 0.3f, 0f), new Vector3(1.1f, 0.6f, 0.8f), fur);
            Part.Make(PrimitiveType.Sphere, "Head", go.transform, new Vector3(0.55f, 0.35f, 0f), new Vector3(0.5f, 0.45f, 0.5f), fur);
            Part.Make(PrimitiveType.Cube, "Ear L", go.transform, new Vector3(0.6f, 0.6f, -0.15f), new Vector3(0.12f, 0.15f, 0.06f), fur).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Part.Make(PrimitiveType.Cube, "Ear R", go.transform, new Vector3(0.6f, 0.6f, 0.15f), new Vector3(0.12f, 0.15f, 0.06f), fur).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Part.Make(PrimitiveType.Capsule, "Tail", go.transform, new Vector3(-0.55f, 0.15f, 0.25f), new Vector3(0.12f, 0.45f, 0.12f), fur).transform.localRotation = Quaternion.Euler(70f, 30f, 0f);
            var cat = go.AddComponent<LabCat>();
            return cat;
        }

        void Update()
        {
            // Breathing.
            transform.localScale = new Vector3(1f, 1f + Mathf.Sin(Time.time * 1.5f) * 0.04f, 1f);
        }
    }

    /// <summary>A fish tank in the lab: one fish for every five achievements.</summary>
    public class FishTank : MonoBehaviour
    {
        public int Fish { get; private set; }

        public static FishTank Place(Vector3 position)
        {
            var go = new GameObject("Fish Tank");
            go.transform.position = position;
            Part.Make(PrimitiveType.Cube, "Stand", go.transform, new Vector3(0f, 0.4f, 0f), new Vector3(1.6f, 0.8f, 0.8f), new Color(0.35f, 0.25f, 0.15f));
            Part.Make(PrimitiveType.Cube, "Water", go.transform, new Vector3(0f, 1.3f, 0f), new Vector3(1.5f, 1f, 0.7f), new Color(0.4f, 0.7f, 0.9f), true);
            var tank = go.AddComponent<FishTank>();
            tank.Fish = Mathf.Min(12, Achievements.Count / 5);
            Color[] colours = { new Color(1f, 0.6f, 0.1f), new Color(0.3f, 0.6f, 1f), new Color(1f, 0.3f, 0.5f), new Color(1f, 0.9f, 0.3f) };
            for (int i = 0; i < tank.Fish; i++)
            {
                var fish = Part.Make(PrimitiveType.Sphere, "Fish " + i, go.transform, new Vector3(Random.Range(-0.55f, 0.55f), Random.Range(1.0f, 1.6f), Random.Range(-0.2f, 0.2f)), new Vector3(0.18f, 0.1f, 0.06f), colours[i % colours.Length], true);
                fish.AddComponent<Swim>();
            }
            return tank;
        }
    }

    /// <summary>A fish going back and forth.</summary>
    public class Swim : MonoBehaviour
    {
        Vector3 _home;
        float _phase, _speed;

        void Start()
        {
            _home = transform.localPosition;
            _phase = Random.value * 10f;
            _speed = Random.Range(0.6f, 1.2f);
        }

        void Update()
        {
            float x = Mathf.Sin(Time.time * _speed + _phase) * 0.5f;
            transform.localPosition = new Vector3(x, _home.y + Mathf.Sin(Time.time * 2f + _phase) * 0.03f, _home.z);
            transform.localRotation = Quaternion.Euler(0f, Mathf.Cos(Time.time * _speed + _phase) > 0 ? 0f : 180f, 0f);
        }
    }
}
