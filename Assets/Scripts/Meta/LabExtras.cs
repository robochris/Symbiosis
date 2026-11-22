using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The item wall, beside the filing cabinet: a plaque for every item there is. Found ones are in their colour,
    /// boss items with a gold frame; the rest are dark, with a question mark, so it is plain how much is still out there.
    /// </summary>
    public class ItemWall : MonoBehaviour
    {
        public const int Columns = 8;
        public static ItemWall Current { get; private set; }
        public int Shown { get; private set; }

        public static ItemWall Place(Vector3 position, Quaternion facing)
        {
            var library = GameAssets.Instance != null ? GameAssets.Instance.items : null;
            if (library == null || library.items == null) return null;
            var go = new GameObject("Item Wall");
            go.transform.SetPositionAndRotation(position, facing);
            var wall = go.AddComponent<ItemWall>();
            int count = library.items.Length;
            int rows = Mathf.CeilToInt(count / (float)Columns);
            float cell = 0.42f;
            float width = Columns * cell + 0.3f, height = rows * cell + 0.3f;
            Part.Make(PrimitiveType.Cube, "Board", go.transform, new Vector3(0f, 0.6f + height * 0.5f, 0.06f), new Vector3(width, height, 0.08f), new Color(0.3f, 0.22f, 0.15f));
            Part.Make(PrimitiveType.Cube, "Foot", go.transform, new Vector3(0f, 0.3f, 0.06f), new Vector3(0.15f, 0.6f, 0.15f), new Color(0.3f, 0.22f, 0.15f));
            var found = SaveSystem.Data.meta.itemsFound;
            var bossItems = new HashSet<ItemType>(BossEnemy.Trophies.Values);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < count; i++)
            {
                var item = library.items[i];
                if (item == null) continue;
                int column = i % Columns, row = i / Columns;
                Vector3 at = new Vector3((column - (Columns - 1) * 0.5f) * cell, 0.6f + height - 0.15f - (row + 0.5f) * cell, 0f);
                bool have = found.Contains((int)item.type);
                if (bossItems.Contains(item.type))
                    Part.Make(PrimitiveType.Cube, "Frame", go.transform, at + new Vector3(0f, 0f, 0.01f), new Vector3(0.36f, 0.36f, 0.02f), new Color(1f, 0.8f, 0.25f), true);
                Color colour = have && item.material != null ? item.material.color : new Color(0.1f, 0.1f, 0.12f);
                Part.Make(PrimitiveType.Cube, item.displayName, go.transform, at, new Vector3(0.3f, 0.3f, 0.04f), colour, have);
                if (have) { wall.Shown++; continue; }
                var mark = new GameObject("?");
                mark.transform.SetParent(go.transform, false);
                mark.transform.localPosition = at + new Vector3(0f, 0f, -0.03f);
                var text = mark.AddComponent<TextMesh>();
                text.font = font;
                mark.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                text.text = "?";
                text.fontSize = 48;
                text.characterSize = 0.05f;
                text.anchor = TextAnchor.MiddleCenter;
                text.color = new Color(0.6f, 0.6f, 0.65f);
            }
            return wall;
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

        string Prompt(Vector3 playerPosition)
        {
            Vector3 to = playerPosition - transform.position;
            to.y = 0f;
            if (to.magnitude > 3f) return null;
            var library = GameAssets.Instance != null ? GameAssets.Instance.items : null;
            int total = library != null && library.items != null ? library.items.Length : 0;
            return $"The item wall: {Shown} of {total} found. Gold frames are boss items: each boss leaves its own the first time it is beaten.";
        }
    }

    /// <summary>One letter for the lab's mailbox: who from, what it says, when it arrives, and a small gift.</summary>
    public class Letter
    {
        public string id, from, text;
        public int gift;
        public Func<MetaSave, bool> arrives;
    }

    /// <summary>
    /// The mailbox by the lab door. Letters turn up as things happen (a boss beaten, a vault cracked, a hundred
    /// runs), from the creatures concerned, each with a few samples folded inside. The flag is up while there is
    /// something unread; use it to read the next.
    /// </summary>
    public class Mailbox : MonoBehaviour
    {
        public static readonly Letter[] All =
        {
            new Letter { id = "welcome", from = "Dr. Finch", gift = 10, arrives = m => m.runsStarted >= 1, text = "You went out and you came back. That is the whole job. Enclosed: something for the trouble." },
            new Letter { id = "queen", from = "The Ant Queen", gift = 15, arrives = m => m.specimens.Contains("AntQueen"), text = "Dear tiny thing. The colony has voted to forgive you. The vote was close. Do not visit." },
            new Letter { id = "candle", from = "The Trick Candle", gift = 15, arrives = m => m.specimens.Contains("TrickCandle"), text = "I am writing this by my own light. Happy birthday. It is not your birthday. Happy birthday anyway." },
            new Letter { id = "vacuum", from = "The Robot Vacuum", gift = 15, arrives = m => m.specimens.Contains("RobotVacuum"), text = "BEEP. Your room has been added to my schedule. BEEP. You will not know when." },
            new Letter { id = "librarian", from = "The Librarian", gift = 15, arrives = m => m.specimens.Contains("TheLibrarian"), text = "Your fine stands at four samples. It is enclosed, with interest, because I am not a monster." },
            new Letter { id = "toad", from = "The Toad", gift = 15, arrives = m => m.specimens.Contains("TheToad"), text = "Croak. (He means well. He only knows the one word. — the Froglet)" },
            new Letter { id = "goldfish", from = "The Goldfish", gift = 15, arrives = m => m.specimens.Contains("TheGoldfish"), text = "Hello! Who are you? Hello! Who are you? Hello! I have forgotten why I wrote." },
            new Letter { id = "duck", from = "The Rubber Duck", gift = 15, arrives = m => m.specimens.Contains("TheRubberDuck"), text = "Squeak. (Translated: well played. Bring a towel next time.)" },
            new Letter { id = "spider", from = "The Spider", gift = 15, arrives = m => m.specimens.Contains("TheSpider"), text = "I have started on a web in your lab. You will not find it. Look up. No, further up." },
            new Letter { id = "mower", from = "The Lawnmower", gift = 15, arrives = m => m.specimens.Contains("TheLawnmower"), text = "Cut you a lawn. It is outside. You are inside. This is how it goes, every Sunday." },
            new Letter { id = "book", from = "The Encyclopedia", gift = 15, arrives = m => m.specimens.Contains("TheEncyclopedia"), text = "See: VOID, THE. Small, dark, persistent. Further reading: you, apparently." },
            new Letter { id = "secret", from = "Somebody", gift = 20, arrives = m => m.secretsFound >= 1, text = "You found the room I walled up. Fine. There are more. There are always more." },
            new Letter { id = "vault", from = "The Vault Keeper", gift = 25, arrives = m => m.vaultsCleared >= 1, text = "The plates remember every foot. Yours were in the right order, eventually. Come back with friends one day." },
            new Letter { id = "ten", from = "Dr. Finch", gift = 20, arrives = m => m.runsStarted >= 10, text = "Ten runs. I have started a chart. The chart is mostly you falling over, and then not." },
            new Letter { id = "fifty", from = "Dr. Finch", gift = 50, arrives = m => m.runsStarted >= 50, text = "Fifty. I have stopped the chart. It was making me proud and I do not care for it." },
            new Letter { id = "hats", from = "The Hat Stand", gift = 20, arrives = m => Hats.EarnedCount >= 5, text = "Five hats. I am honoured to hold them. Please stop putting them on me all at once." },
            new Letter { id = "pressure", from = "The Pressure Gauge", gift = 30, arrives = m => m.pressureBest >= 1, text = "Hsssss. (A compliment.)" },
        };

        public static Mailbox Current { get; private set; }
        readonly StepPad _pad = new StepPad { radius = 0.9f };
        Transform _flag;

        static bool Read(Letter letter) => SaveSystem.Data.meta.lettersRead.Contains(letter.id);

        /// <summary>Letters that have arrived and not been read, oldest first.</summary>
        public static List<Letter> Waiting()
        {
            var waiting = new List<Letter>();
            MetaSave meta = SaveSystem.Data.meta;
            foreach (var letter in All) if (!Read(letter) && letter.arrives(meta)) waiting.Add(letter);
            return waiting;
        }

        public static Mailbox Place(Vector3 position)
        {
            var go = new GameObject("Mailbox");
            go.transform.position = position;
            Part.Make(PrimitiveType.Cylinder, "Post", go.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.12f, 0.55f, 0.12f), new Color(0.35f, 0.25f, 0.15f));
            Part.Make(PrimitiveType.Cube, "Box", go.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.55f, 0.45f, 0.8f), new Color(0.2f, 0.35f, 0.75f));
            var flag = new GameObject("Flag").transform;
            flag.SetParent(go.transform, false);
            flag.localPosition = new Vector3(0.3f, 1.25f, -0.2f);
            Part.Make(PrimitiveType.Cube, "Stick", flag, new Vector3(0f, 0.2f, 0f), new Vector3(0.04f, 0.4f, 0.04f), new Color(0.9f, 0.15f, 0.15f));
            Part.Make(PrimitiveType.Cube, "Pennant", flag, new Vector3(0f, 0.35f, 0.1f), new Vector3(0.03f, 0.14f, 0.2f), new Color(0.9f, 0.15f, 0.15f));
            Part.Make(PrimitiveType.Cylinder, "Mat", go.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.4f, 0.02f, 1.4f), new Color(0.4f, 0.6f, 1f));
            var box = go.AddComponent<Mailbox>();
            box._flag = flag;
            UseMarkers.Rescan();
            return box;
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
            bool mail = Waiting().Count > 0;
            if (_flag != null) _flag.localRotation = Quaternion.Slerp(_flag.localRotation, Quaternion.Euler(mail ? 0f : -90f, 0f, 0f), Time.deltaTime * 6f);
            var player = PlayerController.Instance;
            if (player != null && _pad.SteppedOn(transform.position, player.transform.position)) ReadNext();
        }

        /// <summary>Reads the oldest unread letter, banks its gift, and shows it. Returns it, or null with none.</summary>
        public Letter ReadNext()
        {
            var waiting = Waiting();
            if (waiting.Count == 0) return null;
            var letter = waiting[0];
            MetaSave meta = SaveSystem.Data.meta;
            meta.lettersRead.Add(letter.id);
            meta.samples += letter.gift;
            SaveSystem.Save();
            Sfx.Play(SfxId.Item, 0.5f);
            if (ScreenCards.Instance != null) ScreenCards.Instance.ShowCard("A LETTER FROM " + letter.from.ToUpperInvariant(), $"\"{letter.text}\"\n(+{letter.gift} samples, folded inside)", 6f);
            return letter;
        }

        string Prompt(Vector3 playerPosition)
        {
            if (_pad.Distance(transform.position, playerPosition) > 2f) return null;
            int waiting = Waiting().Count;
            return waiting > 0 ? $"The mailbox: {waiting} letter{(waiting == 1 ? "" : "s")}.\n{Interact.Verb} to read." : "The mailbox: nothing yet. Things write to you when you do things to them.";
        }
    }

    /// <summary>
    /// The quick-run sign by the portal: a short run for a short sitting. Every world is two floors and then its
    /// boss. Everything else is as usual.
    /// </summary>
    public class QuickRunSign : MonoBehaviour
    {
        readonly StepPad _pad = new StepPad { radius = 0.9f };
        public static QuickRunSign Current { get; private set; }

        public static QuickRunSign Place(Vector3 position)
        {
            var go = new GameObject("Quick Run Sign");
            go.transform.position = position;
            Part.Make(PrimitiveType.Cylinder, "Post", go.transform, new Vector3(0f, 0.7f, 0f), new Vector3(0.1f, 0.7f, 0.1f), new Color(0.35f, 0.35f, 0.38f));
            Part.Make(PrimitiveType.Cube, "Sign", go.transform, new Vector3(0f, 1.6f, 0f), new Vector3(1.2f, 0.5f, 0.06f), new Color(1f, 0.75f, 0.15f));
            var label = new GameObject("Label");
            label.transform.SetParent(go.transform, false);
            label.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var text = label.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font;
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            text.text = "QUICK RUN";
            text.fontSize = 48;
            text.characterSize = 0.04f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(0.15f, 0.1f, 0.05f);
            label.AddComponent<FaceCamera>();
            Part.Make(PrimitiveType.Cylinder, "Mat", go.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.4f, 0.02f, 1.4f), new Color(1f, 0.75f, 0.15f));
            var sign = go.AddComponent<QuickRunSign>();
            UseMarkers.Rescan();
            return sign;
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
            Begin();
        }

        public bool Begin()
        {
            if (SaveSystem.Data.run.active)
            {
                Sfx.Play(SfxId.Blocked);
                var hud = FindAnyObjectByType<Hud>();
                if (hud != null) hud.Toast("There is a run already going: carry on with it through the portal.", 3f);
                return false;
            }
            RunDirector.Quick = true;
            Fade.LoadScene("Run");
            return true;
        }

        string Prompt(Vector3 playerPosition)
        {
            if (_pad.Distance(transform.position, playerPosition) > 2f) return null;
            return $"A quick run: every world is two floors and then its boss. About ten minutes.\n{Interact.Verb} to go.";
        }
    }
}
