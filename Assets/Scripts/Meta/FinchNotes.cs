using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>One page of Dr. Finch's notebook, left somewhere in a biome.</summary>
    public class FinchNote
    {
        public int id;
        /// <summary>The biome it is found in.</summary>
        public string biome;
        public string text;
    }

    /// <summary>
    /// The trail Dr. Finch left: pages of their notebook, a few hidden in each biome and found in order. They are the
    /// traces the secret ending is built on, and where the story stops being a joke. Found pages are kept in the save
    /// and pinned to the evidence board in the lab.
    /// </summary>
    public static class FinchNotes
    {
        public static readonly FinchNote[] All =
        {
            new FinchNote { id = 1, biome = "ants", text = "Day 1. Shrinking worked. The ants are larger than expected, which I should have expected. Told nobody. Nobody to tell." },
            new FinchNote { id = 2, biome = "ants", text = "Day 9. The colony tolerates me if I carry something. I carry a crumb everywhere. It is the best disguise I have ever worn." },
            new FinchNote { id = 3, biome = "ants", text = "Day 30. There is a floor under the Queen. There is a floor under that. Nobody has asked what is at the bottom. I am asking." },
            new FinchNote { id = 4, biome = "cake", text = "Day 52. The cake is for somebody's eighth birthday. I counted the candles. I did not stay for the singing." },
            new FinchNote { id = 5, biome = "cake", text = "Day 61. Second dose. Smaller again. The intern's contract reached the lab today. I signed it for them. It seemed quicker." },
            new FinchNote { id = 6, biome = "cake", text = "Day 80. Far enough down there is no more down, only me. I would like to see that. Somebody will have to keep the way open." },
            new FinchNote { id = 7, biome = "sofa", text = "Day 96. Passed under the sofa on the way down. Found my good pen. It is a bridge now. I crossed it and felt nothing." },
            new FinchNote { id = 8, biome = "sofa", text = "Day 104. The vacuum runs at nine. I set my watch by it, and then lost the watch. It is the size of a town square." },
            new FinchNote { id = 9, biome = "sofa", text = "Day 131. Third dose is ready. If anyone is reading these in order: I am sorry about the door. There is a door." },
        };

        /// <summary>A page has just been picked up.</summary>
        public static event Action<FinchNote> Found;

        public static int Count => SaveSystem.Data.meta.notes.Count;
        public static bool Has(int id) => SaveSystem.Data.meta.notes.Contains(id);

        /// <summary>The next page still to be found in a biome, or null when it has given up all of them.</summary>
        public static FinchNote NextFor(string biome)
        {
            foreach (var note in All) if (note.biome == biome && !Has(note.id)) return note;
            return null;
        }

        public static FinchNote Get(int id)
        {
            foreach (var note in All) if (note.id == id) return note;
            return null;
        }

        /// <summary>Pages found so far, in the order they were written.</summary>
        public static List<FinchNote> Collected()
        {
            var list = new List<FinchNote>();
            foreach (var note in All) if (Has(note.id)) list.Add(note);
            return list;
        }

        public static bool Collect(FinchNote note)
        {
            if (note == null || Has(note.id)) return false;
            SaveSystem.Data.meta.notes.Add(note.id);
            SaveSystem.Save();
            Found?.Invoke(note);
            return true;
        }
    }

    /// <summary>A page lying in a room: a pale sheet that turns slowly. Walk onto it to take it.</summary>
    public class NotePickup : MonoBehaviour
    {
        public FinchNote note;
        public float takeDistance = 1.3f;
        public float noticeDistance = 4.5f;

        Transform _page;

        public static NotePickup Create(FinchNote note, Vector3 position, Transform parent)
        {
            var go = new GameObject("Finch's Note");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var pickup = go.AddComponent<NotePickup>();
            pickup.note = note;

            var page = GameObject.CreatePrimitive(PrimitiveType.Cube);
            page.name = "Page";
            Destroy(page.GetComponent<Collider>());
            page.transform.SetParent(go.transform, false);
            page.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            page.transform.localScale = new Vector3(0.5f, 0.66f, 0.03f);
            page.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            var assets = GameAssets.Instance;
            if (assets != null && assets.note != null) page.GetComponent<Renderer>().sharedMaterial = assets.note;
            page.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pickup._page = page.transform;
            return pickup;
        }

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        string Prompt(Vector3 playerPosition)
        {
            Vector3 to = playerPosition - transform.position;
            to.y = 0f;
            return to.magnitude <= noticeDistance ? "A page, in somebody's very tidy handwriting." : null;
        }

        void Update()
        {
            if (_page != null) _page.Rotate(0f, 70f * Time.deltaTime, 0f, Space.World);

            var player = PlayerController.Instance;
            if (player == null) return;
            Vector3 to = player.transform.position - transform.position;
            to.y = 0f;
            if (to.magnitude > takeDistance) return;

            if (FinchNotes.Collect(note))
            {
                Sfx.Play(SfxId.Item);
                var hud = FindAnyObjectByType<Hud>();
                if (hud != null) hud.Toast($"Finch's notebook, page {note.id} of {FinchNotes.All.Length}.  \"{note.text}\"", 9f);
            }
            Destroy(gameObject);
        }
    }
}
