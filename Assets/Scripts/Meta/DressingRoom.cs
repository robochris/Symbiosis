using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The dressing room, through the door at the back of the office: every hat, pet and style there is, out on
    /// display. The ones earned stand there as they are; the ones still out there are under a sheet, and say how
    /// to earn them. Walk up to one and use it to put it on. Its door stays shut until the first run is over.
    /// </summary>
    public class DressingRoom : MonoBehaviour
    {
        [Tooltip("Shut until the room opens.")]
        public GameObject door;

        public static DressingRoom Current { get; private set; }
        public bool IsOpen { get; private set; }

        readonly List<DressingPeg> _pegs = new List<DressingPeg>();

        /// <summary>How many of one kind are out, for tests.</summary>
        public int Count(DressingPeg.Kind kind)
        {
            int count = 0;
            foreach (var peg in _pegs) if (peg.kind == kind) count++;
            return count;
        }

        /// <summary>The one standing for this hat, pet or style, for tests.</summary>
        public DressingPeg Find(DressingPeg.Kind kind, int index)
        {
            foreach (var peg in _pegs) if (peg.kind == kind && peg.index == index) return peg;
            return null;
        }

        void OnEnable() => Current = this;

        void OnDisable()
        {
            if (Current == this) Current = null;
        }

        void Start()
        {
            IsOpen = Unlocks.Open(Unlocks.Feature.HatStand);
            if (door != null) door.SetActive(!IsOpen);
            if (!IsOpen) return;

            var player = PlayerController.Instance;
            var style = player != null ? player.GetComponent<PlayerStyle>() : null;
            Material head = style != null && style.styles != null && style.styles.Length > 0 ? style.styles[0].body : null;

            // Hats along the back wall, facing into the room.
            int hats = Hats.All.Length;
            for (int i = 0; i < hats; i++)
            {
                float x = Mathf.Lerp(-10f, 10f, hats > 1 ? i / (float)(hats - 1) : 0.5f);
                Add(DressingPeg.Kind.Hat, i, new Vector3(x, 0f, 6.2f), 180f, head, null);
            }

            // Styles on two rows of stands down the middle.
            if (style != null && style.styles != null)
            {
                int styles = style.styles.Length, perRow = Mathf.CeilToInt(styles / 2f);
                for (int i = 0; i < styles; i++)
                {
                    int row = i / perRow, at = i % perRow;
                    float x = Mathf.Lerp(-8f, 9f, perRow > 1 ? at / (float)(perRow - 1) : 0.5f);
                    Add(DressingPeg.Kind.Style, i, new Vector3(x, 0f, row == 0 ? 2.2f : -1.6f), 180f, null, style.styles[i]);
                }
            }

            // Pets on perches by the west wall, once there are pets.
            if (Unlocks.Open(Unlocks.Feature.PetJar))
            {
                int pets = Pets.All.Length;
                for (int i = 0; i < pets; i++)
                    Add(DressingPeg.Kind.Pet, i, new Vector3(-10.2f, 0f, Mathf.Lerp(-5.2f, 3.4f, pets > 1 ? i / (float)(pets - 1) : 0.5f)), 90f, null, null);
            }
            Refresh();
        }

        void Add(DressingPeg.Kind kind, int index, Vector3 local, float yaw, Material head, VoidStyle look)
        {
            var go = new GameObject(kind + " " + index);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var peg = go.AddComponent<DressingPeg>();
            peg.kind = kind;
            peg.index = index;
            peg.room = this;
            peg.Build(head, look);
            _pegs.Add(peg);
        }

        /// <summary>Puts the glow under whatever is being worn now, and the sheet over whatever is still to earn.</summary>
        public void Refresh()
        {
            foreach (var peg in _pegs) peg.Show();
        }
    }

    /// <summary>One thing on display in the dressing room: a hat on a head, a pet on a perch, or a style on a stand.</summary>
    public class DressingPeg : MonoBehaviour
    {
        public enum Kind { Hat, Pet, Style }

        public Kind kind;
        public int index;
        public DressingRoom room;
        public readonly StepPad pad = new StepPad { radius = 0.45f };

        GameObject _shown, _sheet, _glow;

        public bool Earned
        {
            get
            {
                switch (kind)
                {
                    case Kind.Hat: return Hats.Earned(index);
                    case Kind.Pet: return Pets.Earned(index);
                    default:
                        var style = Style;
                        return style != null && style.Unlocked(index);
                }
            }
        }

        public bool Worn
        {
            get
            {
                switch (kind)
                {
                    case Kind.Hat: return Hats.Worn == index;
                    case Kind.Pet: return Pets.Chosen == index;
                    default:
                        var style = Style;
                        return style != null && style.Current == index;
                }
            }
        }

        static PlayerStyle Style => PlayerController.Instance != null ? PlayerController.Instance.GetComponent<PlayerStyle>() : null;

        string Name
        {
            get
            {
                switch (kind)
                {
                    case Kind.Hat: return Hats.All[index].name;
                    case Kind.Pet: return Pets.All[index].name;
                    default:
                        var style = Style;
                        return style != null ? style.styles[index].displayName : "a style";
                }
            }
        }

        string How
        {
            get
            {
                switch (kind)
                {
                    case Kind.Hat: return Hats.All[index].how;
                    case Kind.Pet: return Pets.All[index].how;
                    default:
                        var style = Style;
                        if (style == null) return "";
                        string name = style.styles[index].displayName;
                        int level = Progress.LevelForStyle(name);
                        if (level > 0) return $"Reach level {level}.";
                        var needs = Achievements.ForStyle(name);
                        return needs != null ? needs.how : "";
                }
            }
        }

        internal void Build(Material head, VoidStyle look)
        {
            var wood = new Color(0.42f, 0.27f, 0.2f);
            _glow = Part.Make(PrimitiveType.Cylinder, "Worn", transform, new Vector3(0f, 0.015f, 0f), new Vector3(0.95f, 0.01f, 0.95f), new Color(1f, 0.82f, 0.35f), true);
            _shown = new GameObject("Shown");
            _shown.transform.SetParent(transform, false);

            switch (kind)
            {
                case Kind.Hat:
                {
                    Part.Make(PrimitiveType.Cylinder, "Stand", transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.36f, 0.3f, 0.36f), wood);
                    var on = new GameObject("On").transform;
                    on.SetParent(_shown.transform, false);
                    on.localPosition = new Vector3(0f, 0.46f, 0f);
                    var bust = Part.Make(PrimitiveType.Sphere, "Head", on, new Vector3(0f, 0.52f, 0f), Vector3.one * 0.76f, new Color(0.05f, 0.05f, 0.08f));
                    if (head != null) bust.GetComponent<Renderer>().sharedMaterial = head;
                    Hats.All[index].build(on);
                    on.gameObject.AddComponent<Spin>().degreesPerSecond = 25f;
                    break;
                }
                case Kind.Pet:
                {
                    Part.Make(PrimitiveType.Cylinder, "Perch", transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.06f, 0.6f, 0.06f), wood);
                    Part.Make(PrimitiveType.Cube, "Bar", transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.5f, 0.05f, 0.05f), wood);
                    var on = new GameObject("On").transform;
                    on.SetParent(_shown.transform, false);
                    on.localPosition = new Vector3(0f, 1.55f, 0f);
                    on.localScale = Vector3.one * 1.4f;
                    Pets.All[index].build(on);
                    break;
                }
                default:
                {
                    Part.Make(PrimitiveType.Cylinder, "Stand", transform, new Vector3(0f, 0.12f, 0f), new Vector3(0.6f, 0.12f, 0.6f), wood);
                    var body = Part.Make(PrimitiveType.Sphere, "Body", _shown.transform, new Vector3(0f, 0.62f, 0f), new Vector3(0.7f, 0.66f, 0.7f), Color.black);
                    if (look != null && look.body != null) body.GetComponent<Renderer>().sharedMaterial = look.body;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        var eye = Part.Make(PrimitiveType.Sphere, "Eye", _shown.transform, new Vector3(side * 0.13f, 0.72f, 0.3f), new Vector3(0.12f, 0.16f, 0.08f), Color.white, true);
                        if (look != null && look.eyes != null) eye.GetComponent<Renderer>().sharedMaterial = look.eyes;
                    }
                    break;
                }
            }

            // The sheet over one still to earn, with a question mark pinned to it.
            float sheetY = kind == Kind.Pet ? 1.5f : kind == Kind.Hat ? 1.15f : 0.62f;
            _sheet = new GameObject("Sheet");
            _sheet.transform.SetParent(transform, false);
            Part.Make(PrimitiveType.Sphere, "Cloth", _sheet.transform, new Vector3(0f, sheetY, 0f), new Vector3(0.82f, 0.75f, 0.82f), new Color(0.82f, 0.8f, 0.76f));
            // A blocky question mark, made of little squares (world text would show through the walls).
            var ink = new Color(0.3f, 0.26f, 0.28f);
            const float px = 0.055f;
            int[,] cells = { { -1, 2 }, { 0, 2 }, { 1, 2 }, { 1, 1 }, { 0, 0 }, { 1, 0 }, { 0, -2 } };
            for (int i = 0; i < cells.GetLength(0); i++)
                Part.Make(PrimitiveType.Cube, "Mark", _sheet.transform, new Vector3(-cells[i, 0] * px, sheetY + 0.04f + cells[i, 1] * px, 0.39f), new Vector3(px, px, 0.06f), ink);
        }

        internal void Show()
        {
            bool earned = Earned;
            if (_shown != null) _shown.SetActive(earned);
            if (_sheet != null) _sheet.SetActive(!earned);
            if (_glow != null) _glow.SetActive(earned && Worn);
        }

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || !pad.SteppedOn(transform.position, player.transform.position)) return;
            Use();
        }

        /// <summary>Puts it on (or, if it is on already, takes it off).</summary>
        public void Use()
        {
            var hud = FindAnyObjectByType<Hud>();
            if (!Earned)
            {
                Sfx.Play(SfxId.Blocked, 0.25f);
                if (hud != null) hud.Toast(Locked(), 3f);
                return;
            }
            MetaSave meta = SaveSystem.Data.meta;
            bool off = Worn && index != 0;
            string said;
            switch (kind)
            {
                case Kind.Hat:
                    meta.hat = off ? 0 : index;
                    PlayerHat.Refresh();
                    said = off || index == 0 ? "Hat off." : $"Wearing the {Name}.";
                    break;
                case Kind.Pet:
                    meta.petJarOpens++;
                    meta.pet = off ? 0 : index;
                    PetFollower.Refresh();
                    said = off || index == 0 ? "No pet for now." : $"The {Name} comes along.";
                    break;
                default:
                    var style = Style;
                    if (style == null) return;
                    // A style is never taken off, only changed: there is always a body.
                    style.Apply(index);
                    meta.style = style.Current;
                    said = $"Wearing {Name}.";
                    break;
            }
            SaveSystem.Save();
            Sfx.Play(SfxId.Item, 0.6f);
            if (hud != null) hud.Toast(said, 2.5f);
            if (room != null) room.Refresh();
        }

        string Locked()
        {
            string how = How;
            if (string.IsNullOrEmpty(how) || how == "???") return "Under a sheet: something nobody has found yet.";
            return $"Under a sheet: the {Name}. {how}";
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 1.2f) return null;
            if (!Earned) return Locked();
            string what = kind == Kind.Hat ? "hat" : kind == Kind.Pet ? "pet" : "style";
            if (index == 0 && kind != Kind.Style) return kind == Kind.Hat ? $"No hat. {Interact.Verb} to go bare-headed." : $"No pet. {Interact.Verb} to go alone.";
            if (Worn) return kind == Kind.Style ? $"{Name}: you are wearing it." : $"The {Name}: you have it on. {Interact.Verb} to take it off.";
            return kind == Kind.Style ? $"The {Name} {what}. {Interact.Verb} to wear it." : $"The {Name}. {Interact.Verb} to " + (kind == Kind.Pet ? "bring it along." : "wear it.");
        }
    }
}
