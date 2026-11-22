using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shrunk
{
    /// <summary>One hat: what it is called, how it is earned, and how it is made.</summary>
    public class Hat
    {
        public string name;
        public string how;
        public Func<MetaSave, bool> earned;
        public Action<Transform> build;
    }

    /// <summary>
    /// Hats: worn for the look and nothing else, chosen at the hat stand in the lab. Each is earned by playing
    /// (a level, some bosses, a week of parcels...). An item that sits on the head hides the hat while it is worn.
    /// </summary>
    public static class Hats
    {
        // Where the top of the head is, in the player's visual.
        const float Top = 0.9f;

        public static readonly Hat[] All =
        {
            new Hat { name = "No hat", how = "", earned = m => true, build = t => { } },
            new Hat { name = "Beanie", how = "", earned = m => true, build = t =>
            {
                Part.Make(PrimitiveType.Sphere, "Knit", t, new Vector3(0f, Top, 0f), new Vector3(0.62f, 0.34f, 0.62f), new Color(0.85f, 0.2f, 0.2f));
                Part.Make(PrimitiveType.Sphere, "Bobble", t, new Vector3(0f, Top + 0.2f, 0f), Vector3.one * 0.15f, new Color(0.95f, 0.95f, 0.95f));
            } },
            new Hat { name = "Party Hat", how = "Start 3 runs.", earned = m => m.runsStarted >= 3, build = t =>
            {
                for (int i = 0; i < 5; i++)
                {
                    float r = Mathf.Lerp(0.36f, 0.06f, i / 4f);
                    Part.Make(PrimitiveType.Cylinder, "Tier " + i, t, new Vector3(0f, Top + 0.04f + i * 0.08f, 0f), new Vector3(r, 0.04f, r), i % 2 == 0 ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.85f, 0.25f));
                }
                Part.Make(PrimitiveType.Sphere, "Pom", t, new Vector3(0f, Top + 0.44f, 0f), Vector3.one * 0.1f, new Color(1f, 0.35f, 0.7f));
            } },
            new Hat { name = "Propeller Cap", how = "Reach level 5.", earned = m => Progress.LevelFor(m.xp) >= 5, build = t =>
            {
                Part.Make(PrimitiveType.Sphere, "Cap", t, new Vector3(0f, Top - 0.02f, 0f), new Vector3(0.62f, 0.3f, 0.62f), new Color(0.25f, 0.45f, 0.95f));
                Part.Make(PrimitiveType.Cylinder, "Stem", t, new Vector3(0f, Top + 0.17f, 0f), new Vector3(0.04f, 0.05f, 0.04f), new Color(0.9f, 0.9f, 0.2f));
                var rotor = new GameObject("Rotor").transform;
                rotor.SetParent(t, false);
                rotor.localPosition = new Vector3(0f, Top + 0.22f, 0f);
                Part.Make(PrimitiveType.Cube, "Blade A", rotor, Vector3.zero, new Vector3(0.62f, 0.02f, 0.09f), new Color(1f, 0.3f, 0.3f));
                Part.Make(PrimitiveType.Cube, "Blade B", rotor, Vector3.zero, new Vector3(0.09f, 0.02f, 0.62f), new Color(0.3f, 0.85f, 0.4f));
                rotor.gameObject.AddComponent<Spin>().degreesPerSecond = 540f;
            } },
            new Hat { name = "Chef's Hat", how = "Get home with your samples 3 times.", earned = m => m.extractions >= 3, build = t =>
            {
                Part.Make(PrimitiveType.Cylinder, "Band", t, new Vector3(0f, Top + 0.06f, 0f), new Vector3(0.5f, 0.1f, 0.5f), Color.white);
                Part.Make(PrimitiveType.Sphere, "Puff", t, new Vector3(0f, Top + 0.28f, 0f), new Vector3(0.66f, 0.38f, 0.66f), Color.white);
            } },
            new Hat { name = "Top Hat", how = "Beat 3 bosses.", earned = m => m.bossesBeaten >= 3, build = t =>
            {
                var black = new Color(0.08f, 0.08f, 0.1f);
                Part.Make(PrimitiveType.Cylinder, "Brim", t, new Vector3(0f, Top, 0f), new Vector3(0.72f, 0.02f, 0.72f), black);
                Part.Make(PrimitiveType.Cylinder, "Crown", t, new Vector3(0f, Top + 0.22f, 0f), new Vector3(0.46f, 0.22f, 0.46f), black);
                Part.Make(PrimitiveType.Cylinder, "Ribbon", t, new Vector3(0f, Top + 0.07f, 0f), new Vector3(0.47f, 0.04f, 0.47f), new Color(0.75f, 0.1f, 0.15f));
            } },
            new Hat { name = "Antenna", how = "Open the daily parcel 3 days in a row.", earned = m => m.parcelStreak >= 3, build = t =>
            {
                Part.Make(PrimitiveType.Cylinder, "Wire", t, new Vector3(0f, Top + 0.14f, 0f), new Vector3(0.035f, 0.16f, 0.035f), new Color(0.6f, 0.6f, 0.65f));
                Part.Make(PrimitiveType.Sphere, "Bulb", t, new Vector3(0f, Top + 0.34f, 0f), Vector3.one * 0.14f, new Color(0.4f, 1f, 0.6f), true);
            } },
            new Hat { name = "Cowboy Hat", how = "Reach floor 12.", earned = m => m.deepestFloor >= 12, build = t =>
            {
                var leather = new Color(0.5f, 0.32f, 0.16f);
                Part.Make(PrimitiveType.Cylinder, "Brim", t, new Vector3(0f, Top, 0f), new Vector3(0.9f, 0.02f, 0.9f), leather);
                Part.Make(PrimitiveType.Cylinder, "Crown", t, new Vector3(0f, Top + 0.12f, 0f), new Vector3(0.44f, 0.13f, 0.44f), leather);
                Part.Make(PrimitiveType.Cylinder, "Band", t, new Vector3(0f, Top + 0.04f, 0f), new Vector3(0.45f, 0.03f, 0.45f), new Color(0.2f, 0.12f, 0.08f));
            } },
            new Hat { name = "Leaf", how = "Find a secret room.", earned = m => m.secretsFound >= 1, build = t =>
            {
                var leaf = Part.Make(PrimitiveType.Sphere, "Leaf", t, new Vector3(0.06f, Top + 0.06f, 0f), new Vector3(0.5f, 0.05f, 0.24f), new Color(0.3f, 0.75f, 0.25f));
                leaf.transform.localRotation = Quaternion.Euler(0f, 30f, 18f);
                Part.Make(PrimitiveType.Cylinder, "Stalk", t, new Vector3(-0.1f, Top + 0.05f, 0f), new Vector3(0.03f, 0.06f, 0.03f), new Color(0.35f, 0.25f, 0.1f));
            } },
            new Hat { name = "Crown", how = "Beat 10 bosses.", earned = m => m.bossesBeaten >= 10, build = t =>
            {
                var gold = new Color(1f, 0.8f, 0.2f);
                Part.Make(PrimitiveType.Cylinder, "Band", t, new Vector3(0f, Top + 0.05f, 0f), new Vector3(0.5f, 0.07f, 0.5f), gold);
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * 2f / 5f;
                    var point = Part.Make(PrimitiveType.Cube, "Point " + i, t, new Vector3(Mathf.Sin(a) * 0.22f, Top + 0.16f, Mathf.Cos(a) * 0.22f), Vector3.one * 0.1f, gold);
                    point.transform.localRotation = Quaternion.Euler(45f, a * Mathf.Rad2Deg, 45f);
                }
                Part.Make(PrimitiveType.Sphere, "Jewel", t, new Vector3(0f, Top + 0.06f, 0.25f), Vector3.one * 0.08f, new Color(0.9f, 0.1f, 0.3f), true);
            } },
            new Hat { name = "Rival's Bandana", how = "Beat the rival Void for good.", earned = m => m.rivalBeaten > 0, build = t =>
            {
                var red = new Color(0.8f, 0.1f, 0.12f);
                Part.Make(PrimitiveType.Cylinder, "Band", t, new Vector3(0f, Top - 0.12f, 0f), new Vector3(0.66f, 0.06f, 0.66f), red);
                Part.Make(PrimitiveType.Cube, "Tail", t, new Vector3(0f, Top - 0.14f, -0.38f), new Vector3(0.08f, 0.05f, 0.32f), red).transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
            } },
            new Hat { name = "Season Star", how = "Finish a season's track (30 levels).", earned = m => m.seasonStars > 0, build = t =>
            {
                var gold = new Color(1f, 0.85f, 0.25f);
                Part.Make(PrimitiveType.Cylinder, "Stalk", t, new Vector3(0f, Top + 0.12f, 0f), new Vector3(0.03f, 0.12f, 0.03f), gold);
                var star = Part.Make(PrimitiveType.Cube, "Star A", t, new Vector3(0f, Top + 0.32f, 0f), new Vector3(0.2f, 0.2f, 0.05f), gold, true);
                star.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                Part.Make(PrimitiveType.Cube, "Star B", t, new Vector3(0f, Top + 0.32f, 0f), new Vector3(0.2f, 0.2f, 0.05f), gold, true);
            } },
            new Hat { name = "Hard Hat", how = "Beat a world's boss at pressure 3.", earned = m => m.pressureBest >= 3, build = t =>
            {
                var yellow = new Color(1f, 0.82f, 0.1f);
                Part.Make(PrimitiveType.Sphere, "Shell", t, new Vector3(0f, Top - 0.02f, 0f), new Vector3(0.64f, 0.36f, 0.66f), yellow);
                Part.Make(PrimitiveType.Cylinder, "Brim", t, new Vector3(0f, Top - 0.04f, 0.06f), new Vector3(0.72f, 0.015f, 0.78f), yellow);
                Part.Make(PrimitiveType.Cube, "Ridge", t, new Vector3(0f, Top + 0.16f, 0f), new Vector3(0.08f, 0.05f, 0.5f), new Color(0.9f, 0.72f, 0.05f));
            } },
        };

        public static bool Earned(int index)
        {
            if (index < 0 || index >= All.Length) return false;
            if (Achievements.EverythingUnlocked) return true;
            return All[index].earned(SaveSystem.Data.meta);
        }

        public static int EarnedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < All.Length; i++) if (Earned(i)) count++;
                return count;
            }
        }

        /// <summary>The hat being worn (0 = none).</summary>
        public static int Worn
        {
            get
            {
                int saved = SaveSystem.Data.meta.hat;
                return Earned(saved) ? saved : 0;
            }
        }

        /// <summary>Hats earned since the hat stand last mentioned them; marks them as mentioned.</summary>
        public static System.Collections.Generic.List<Hat> TakeNew()
        {
            var fresh = new System.Collections.Generic.List<Hat>();
            MetaSave meta = SaveSystem.Data.meta;
            for (int i = 1; i < All.Length && i < 31; i++)
            {
                int bit = 1 << i;
                if (!Earned(i) || (meta.hatsSeen & bit) != 0) continue;
                meta.hatsSeen |= bit;
                // The free ones are not news.
                if (!string.IsNullOrEmpty(All[i].how)) fresh.Add(All[i]);
            }
            if (fresh.Count > 0) SaveSystem.Save();
            return fresh;
        }

        /// <summary>The first hat not yet earned, for the stand to say what is next.</summary>
        public static Hat NextToEarn()
        {
            for (int i = 0; i < All.Length; i++) if (!Earned(i)) return All[i];
            return null;
        }

        /// <summary>Wears the next earned hat, remembers it, and returns it.</summary>
        public static Hat WearNext()
        {
            int next = Worn;
            for (int i = 0; i < All.Length; i++)
            {
                next = (next + 1) % All.Length;
                if (Earned(next)) break;
            }
            SaveSystem.Data.meta.hat = next;
            SaveSystem.Save();
            PlayerHat.Refresh();
            return All[next];
        }
    }

    /// <summary>The hat on the player's head. Put on the player whenever a scene with one loads.</summary>
    public class PlayerHat : MonoBehaviour
    {
        static PlayerHat _current;
        Transform _hat;
        int _wearing = -1;
        PlayerController _player;
        Vector3 _last;
        float _wobble;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook() => SceneManager.sceneLoaded += (scene, mode) => Attach();

        static void Attach()
        {
            var player = PlayerController.Instance;
            if (player == null || player.GetComponent<PlayerHat>() != null) return;
            player.gameObject.AddComponent<PlayerHat>();
        }

        /// <summary>Puts on whatever is now chosen.</summary>
        public static void Refresh()
        {
            if (_current != null) _current.Wear(Hats.Worn);
        }

        void Awake()
        {
            _current = this;
            _player = GetComponent<PlayerController>();
        }

        void Start() => Wear(Hats.Worn);

        void Wear(int index)
        {
            if (_player == null || _player.visual == null || index == _wearing) return;
            _wearing = index;
            if (_hat != null) Destroy(_hat.gameObject);
            _hat = new GameObject("Hat").transform;
            _hat.SetParent(_player.visual, false);
            Hats.All[index].build(_hat);
            if (CameraFollow.Instance != null) CameraFollow.Instance.RefreshTargetRenderers();
        }

        void LateUpdate()
        {
            if (_hat == null) return;
            // An item on the head (a paper hat, say) is worn over it: the hat comes off meanwhile.
            bool covered = _player.Items != null && _player.Items.LookIn(ItemSlot.Head) != null;
            if (_hat.gameObject.activeSelf == covered) _hat.gameObject.SetActive(!covered);
            // A wobble while running, leaning back against the way it is going.
            Vector3 moved = (transform.position - _last) / Mathf.Max(Time.deltaTime, 0.0001f);
            _last = transform.position;
            moved.y = 0f;
            float speed = Mathf.Min(moved.magnitude, 10f);
            _wobble += Time.deltaTime * (6f + speed);
            Vector3 back = transform.InverseTransformDirection(-moved.normalized);
            float lean = speed * 1.2f;
            Quaternion target = Quaternion.Euler(back.z * -lean + Mathf.Sin(_wobble) * speed * 0.6f, 0f, back.x * lean);
            _hat.localRotation = Quaternion.Slerp(_hat.localRotation, target, Time.deltaTime * 10f);
        }
    }

    /// <summary>Turns about its own up axis, for propellers and the like.</summary>
    public class Spin : MonoBehaviour
    {
        public float degreesPerSecond = 90f;
        void Update() => transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self);
    }

    /// <summary>Primitives made at runtime, with a plain coloured material and no collider.</summary>
    public static class Part
    {
        static Shader _lit, _unlit;

        public static GameObject Make(PrimitiveType type, string name, Transform parent, Vector3 at, Vector3 scale, Color colour, bool glow = false)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            UnityEngine.Object.Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = at;
            part.transform.localScale = scale;
            if (_lit == null) _lit = Shader.Find("Universal Render Pipeline/Lit");
            if (_unlit == null) _unlit = Shader.Find("Universal Render Pipeline/Unlit");
            Shader shader = glow && _unlit != null ? _unlit : _lit;
            var material = new Material(shader != null ? shader : Shader.Find("Standard"));
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
            material.color = colour;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }
    }
}
