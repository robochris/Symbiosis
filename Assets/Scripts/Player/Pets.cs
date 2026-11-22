using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shrunk
{
    /// <summary>One pet: a little flying thing that keeps the player company. For the look only.</summary>
    public class Pet
    {
        public string name;
        public string how;
        public Func<MetaSave, bool> earned;
        public Action<Transform> build;
    }

    /// <summary>
    /// Pets: a small flier that follows the player about, in the lab and on runs. Earned by playing, chosen at the
    /// jar of pets by the lab door. Nothing but company.
    /// </summary>
    public static class Pets
    {
        public static readonly Pet[] All =
        {
            new Pet { name = "No pet", how = "", earned = m => true, build = t => { } },
            new Pet { name = "Ladybird", how = "Get home with your samples once.", earned = m => m.extractions >= 1, build = t =>
            {
                Part.Make(PrimitiveType.Sphere, "Shell", t, Vector3.zero, new Vector3(0.32f, 0.2f, 0.36f), new Color(0.9f, 0.12f, 0.1f));
                Part.Make(PrimitiveType.Sphere, "Head", t, new Vector3(0f, 0f, 0.17f), Vector3.one * 0.14f, new Color(0.08f, 0.08f, 0.08f));
                for (int i = 0; i < 4; i++)
                    Part.Make(PrimitiveType.Sphere, "Spot " + i, t, new Vector3(i % 2 == 0 ? -0.08f : 0.08f, 0.09f, i < 2 ? 0.05f : -0.08f), Vector3.one * 0.06f, new Color(0.05f, 0.05f, 0.05f));
            } },
            new Pet { name = "Firefly", how = "Open the daily parcel 5 days in a row.", earned = m => m.parcelStreak >= 5, build = t =>
            {
                Part.Make(PrimitiveType.Sphere, "Body", t, Vector3.zero, new Vector3(0.14f, 0.12f, 0.3f), new Color(0.2f, 0.18f, 0.1f));
                Part.Make(PrimitiveType.Sphere, "Glow", t, new Vector3(0f, -0.02f, -0.15f), Vector3.one * 0.17f, new Color(0.85f, 1f, 0.35f), true);
                Wings(t, new Color(0.85f, 0.9f, 1f));
            } },
            new Pet { name = "Bumblebee", how = "Chain a combo of 10.", earned = m => m.bestCombo >= 10, build = t =>
            {
                Part.Make(PrimitiveType.Sphere, "Body", t, Vector3.zero, new Vector3(0.26f, 0.24f, 0.32f), new Color(1f, 0.8f, 0.15f));
                Part.Make(PrimitiveType.Cylinder, "Stripe", t, new Vector3(0f, 0f, -0.04f), new Vector3(0.27f, 0.04f, 0.27f), new Color(0.1f, 0.08f, 0.05f)).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Wings(t, new Color(0.9f, 0.95f, 1f));
            } },
            new Pet { name = "Moth", how = "Beat 6 bosses.", earned = m => m.bossesBeaten >= 6, build = t =>
            {
                Part.Make(PrimitiveType.Sphere, "Body", t, Vector3.zero, new Vector3(0.12f, 0.12f, 0.3f), new Color(0.55f, 0.45f, 0.35f));
                Wings(t, new Color(0.8f, 0.72f, 0.6f), 0.36f);
            } },
            new Pet { name = "Golden Moth", how = "???", earned = m => m.petJarOpens >= 100, build = t =>
            {
                Part.Make(PrimitiveType.Sphere, "Body", t, Vector3.zero, new Vector3(0.12f, 0.12f, 0.3f), new Color(1f, 0.8f, 0.2f), true);
                Wings(t, new Color(1f, 0.9f, 0.4f), 0.36f);
            } },
        };

        static void Wings(Transform t, Color colour, float size = 0.26f)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var wing = Part.Make(PrimitiveType.Sphere, side < 0 ? "Wing L" : "Wing R", t, new Vector3(side * size * 0.55f, 0.08f, 0f), new Vector3(size, 0.02f, size * 0.6f), colour);
                wing.AddComponent<Flap>().side = side;
            }
        }

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

        public static int Chosen
        {
            get
            {
                int saved = SaveSystem.Data.meta.pet;
                return Earned(saved) ? saved : 0;
            }
        }

        public static Pet NextToEarn()
        {
            // A secret one is never mentioned.
            for (int i = 0; i < All.Length; i++) if (!Earned(i) && All[i].how != "???") return All[i];
            return null;
        }

        /// <summary>Pets earned since last mentioned; marks them as mentioned.</summary>
        public static System.Collections.Generic.List<Pet> TakeNew()
        {
            var fresh = new System.Collections.Generic.List<Pet>();
            MetaSave meta = SaveSystem.Data.meta;
            for (int i = 1; i < All.Length && i < 31; i++)
            {
                int bit = 1 << i;
                if (!Earned(i) || (meta.petsSeen & bit) != 0) continue;
                meta.petsSeen |= bit;
                fresh.Add(All[i]);
            }
            if (fresh.Count > 0) SaveSystem.Save();
            return fresh;
        }

        public static Pet ChooseNext()
        {
            SaveSystem.Data.meta.petJarOpens++;
            int next = Chosen;
            for (int i = 0; i < All.Length; i++)
            {
                next = (next + 1) % All.Length;
                if (Earned(next)) break;
            }
            SaveSystem.Data.meta.pet = next;
            SaveSystem.Save();
            PetFollower.Refresh();
            return All[next];
        }
    }

    /// <summary>The pet in play: follows a little behind and above the player's shoulder, bobbing.</summary>
    public class PetFollower : MonoBehaviour
    {
        static PetFollower _current;
        int _showing = -1;
        Transform _body;
        Vector3 _velocity;
        float _phase;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook() => SceneManager.sceneLoaded += (scene, mode) => Spawn();

        static void Spawn()
        {
            if (PlayerController.Instance == null || _current != null) return;
            var go = new GameObject("Pet");
            _current = go.AddComponent<PetFollower>();
            go.transform.position = PlayerController.Instance.transform.position + new Vector3(-0.8f, 1.6f, -0.6f);
        }

        public static void Refresh()
        {
            if (_current != null) _current.Show(Pets.Chosen);
        }

        /// <summary>The pet showing now, for tests: null with none.</summary>
        public static Transform Body => _current != null ? _current._body : null;

        void Start() => Show(Pets.Chosen);

        void OnDestroy()
        {
            if (_current == this) _current = null;
        }

        void Show(int index)
        {
            if (index == _showing) return;
            _showing = index;
            if (_body != null) Destroy(_body.gameObject);
            _body = null;
            if (index <= 0) return;
            _body = new GameObject(Pets.All[index].name).transform;
            _body.SetParent(transform, false);
            Pets.All[index].build(_body);
        }

        void LateUpdate()
        {
            var player = PlayerController.Instance;
            if (player == null || _body == null) return;
            _phase += Time.deltaTime;
            Transform p = player.transform;
            Vector3 home = p.position - p.forward * 0.9f + p.right * 0.7f + Vector3.up * (1.7f + Mathf.Sin(_phase * 3f) * 0.12f);
            transform.position = Vector3.SmoothDamp(transform.position, home, ref _velocity, 0.25f);
            Vector3 heading = _velocity.sqrMagnitude > 0.05f ? _velocity : p.forward;
            heading.y = 0f;
            if (heading.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(heading), Time.deltaTime * 6f);
        }
    }

    /// <summary>A wing beating up and down.</summary>
    public class Flap : MonoBehaviour
    {
        public int side = 1;
        void Update() => transform.localRotation = Quaternion.Euler(0f, 0f, side * Mathf.Sin(Time.time * 30f) * 35f);
    }
}

namespace Shrunk
{
    /// <summary>The jar of pets, by the lab door: use it to bring out the next pet you have earned.</summary>
    public class PetJar : UnityEngine.MonoBehaviour
    {
        readonly StepPad _pad = new StepPad { radius = 1f };
        public static PetJar Current { get; private set; }

        public static PetJar Place(UnityEngine.Vector3 position)
        {
            var go = new UnityEngine.GameObject("Pet Jar");
            go.transform.position = position;
            var glass = new UnityEngine.Color(0.75f, 0.9f, 0.95f);
            Part.Make(UnityEngine.PrimitiveType.Cylinder, "Table", go.transform, new UnityEngine.Vector3(0f, 0.45f, 0f), new UnityEngine.Vector3(0.9f, 0.45f, 0.9f), new UnityEngine.Color(0.45f, 0.3f, 0.18f));
            Part.Make(UnityEngine.PrimitiveType.Cylinder, "Jar", go.transform, new UnityEngine.Vector3(0f, 1.25f, 0f), new UnityEngine.Vector3(0.55f, 0.35f, 0.55f), glass);
            Part.Make(UnityEngine.PrimitiveType.Cylinder, "Lid", go.transform, new UnityEngine.Vector3(0f, 1.63f, 0f), new UnityEngine.Vector3(0.6f, 0.04f, 0.6f), new UnityEngine.Color(0.8f, 0.6f, 0.2f));
            Part.Make(UnityEngine.PrimitiveType.Sphere, "Glow", go.transform, new UnityEngine.Vector3(0f, 1.2f, 0f), UnityEngine.Vector3.one * 0.18f, new UnityEngine.Color(0.85f, 1f, 0.35f), true);
            Part.Make(UnityEngine.PrimitiveType.Cylinder, "Mat", go.transform, new UnityEngine.Vector3(0f, 0.02f, 0f), new UnityEngine.Vector3(1.6f, 0.02f, 1.6f), new UnityEngine.Color(0.85f, 1f, 0.4f));
            var jar = go.AddComponent<PetJar>();
            UseMarkers.Rescan();
            return jar;
        }

        void OnEnable()
        {
            Current = this;
            WorldPrompt.Add(Prompt, transform);
        }

        void OnDisable()
        {
            if (Current == this) Current = null;
            WorldPrompt.Remove(Prompt);
        }

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || !_pad.SteppedOn(transform.position, player.transform.position)) return;
            Pet pet = Pets.ChooseNext();
            Sfx.Play(SfxId.Item, 0.6f);
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast(Pets.Chosen == 0 ? "Back in the jar." : $"The {pet.name} comes along.", 2.5f);
        }

        string Prompt(UnityEngine.Vector3 playerPosition)
        {
            if (_pad.Distance(transform.position, playerPosition) > 2f) return null;
            Pet next = Pets.NextToEarn();
            string still = next != null ? $" Still to find: the {next.name}. {next.how}" : " Every pet found.";
            if (Pets.EarnedCount <= 1) return $"A jar for pets. It is empty.{still}";
            return $"The pet jar: {Pets.EarnedCount - 1} of {Pets.All.Length - 1} pets.{still}\n{Interact.Verb} to bring out the next one.";
        }
    }
}
