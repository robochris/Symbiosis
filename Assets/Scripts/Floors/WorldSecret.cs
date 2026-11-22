using System;
using UnityEngine;

namespace Shrunk
{
    /// <summary>One world's own secret: what it is, what it says, how it looks, and what it does when used.</summary>
    public class SecretKind
    {
        public string world, name, line, done;
        public Color colour;
        public Action<FloorManager, PlayerController, Vector3> use;
    }

    /// <summary>
    /// Every world has a secret of its own: something odd tucked away in a room on some of its floors (an unlit
    /// candle on the cake, a lost remote under the sofa, a cheat-code panel in the console...). Use it and it does
    /// its own thing, from a shower of coins to a shortcut down. Each one found goes in the collection; eight is a hat.
    /// </summary>
    public class WorldSecret : MonoBehaviour
    {
        public const float Chance = 0.12f;
        public const int ForTheHat = 8;
        public static float? ChanceForAll;

        public SecretKind kind;
        public bool Used { get; private set; }
        readonly StepPad _pad = new StepPad { radius = 0.9f };
        Transform _look;

        public static int Found => SaveSystem.Data.meta.worldSecrets.Count;

        public static readonly SecretKind[] All =
        {
            new SecretKind { world = "ants", name = "a drop of royal jelly", colour = new Color(1f, 0.85f, 0.4f), line = "A drop of royal jelly, behind a pebble. The queen's own.", done = "Royal jelly: all of your health back, and one more.",
                use = (f, p, at) => { p.Health.SetMaxHealth(p.Health.maxHealth + 1, true); } },
            new SecretKind { world = "cake", name = "an unlit candle", colour = new Color(0.55f, 0.8f, 0.95f), line = "One candle nobody lit. It looks like it wants to be.", done = "The candle lights, and coins rain down.",
                use = (f, p, at) => { for (int i = 0; i < 12; i++) Pickup.Coin(at + new Vector3(UnityEngine.Random.Range(-2f, 2f), 0f, UnityEngine.Random.Range(-2f, 2f)), 1); } },
            new SecretKind { world = "sofa", name = "a lost remote", colour = new Color(0.2f, 0.2f, 0.22f), line = "The TV remote. Everybody has been looking for this.", done = "Click. The channel changes: the next floor has a twist.",
                use = (f, p, at) => { f.twistOverride = FloorTwists.All[UnityEngine.Random.Range(0, FloorTwists.All.Length)]; f.twistOneFloor = true; } },
            new SecretKind { world = "fridge", name = "an ice cube tray", colour = new Color(0.7f, 0.9f, 1f), line = "An ice cube tray, still full.", done = "Everything in sight is frozen stiff for a while.",
                use = (f, p, at) => { foreach (var e in Enemy.Active.ToArray()) if (!e.Health.IsDead) e.Chill(8f); } },
            new SecretKind { world = "library", name = "a book that sticks out", colour = new Color(0.6f, 0.15f, 0.15f), line = "One book sticks out further than the others.", done = "Pull it: the shelf swings round, and you are by the way down.",
                use = (f, p, at) => { f.TeleportToExit(); } },
            new SecretKind { world = "forest", name = "a fairy ring", colour = new Color(0.9f, 0.3f, 0.3f), line = "A ring of red mushrooms. The air hums in the middle.", done = "The ring hums louder: one more health, for good this run.",
                use = (f, p, at) => { p.Health.SetMaxHealth(p.Health.maxHealth + 1, false); p.Health.Heal(1); } },
            new SecretKind { world = "clouds", name = "the end of a rainbow", colour = new Color(1f, 0.6f, 0.9f), line = "The end of a rainbow. There is supposed to be something here.", done = "There is: a pot of gold.",
                use = (f, p, at) => { for (int i = 0; i < 20; i++) Pickup.Coin(at + new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), 0f, UnityEngine.Random.Range(-1.5f, 1.5f)), 1); } },
            new SecretKind { world = "console", name = "a cheat-code panel", colour = new Color(0.3f, 1f, 0.5f), line = "A panel with arrows on it. Up, up, down, down...", done = "CHEAT ACCEPTED. 40 samples.",
                use = (f, p, at) => { RunState.AddSamples(40); } },
            new SecretKind { world = "aquarium", name = "a treasure chest", colour = new Color(0.8f, 0.6f, 0.2f), line = "The little treasure chest from the tank. The lid lifts with a bubble.", done = "Inside: an item.",
                use = (f, p, at) => { var items = GameAssets.Instance?.items; if (items != null) { var picks = items.RandomCommon(new System.Random(UnityEngine.Random.Range(0, int.MaxValue)), 1); if (picks.Count > 0) Pickup.Item(picks[0], at + Vector3.back * 1.4f, 0); } } },
            new SecretKind { world = "toybox", name = "a wind-up key", colour = new Color(0.85f, 0.75f, 0.3f), line = "A big wind-up key, for something.", done = "Wound up: everything in sight dances (and fights each other) for a while.",
                use = (f, p, at) => { foreach (var e in Enemy.Active.ToArray()) if (!e.Health.IsDead && !(e is BossEnemy)) e.Confuse(10f); } },
            new SecretKind { world = "bathroom", name = "a soap bubble", colour = new Color(0.9f, 0.95f, 1f), line = "A perfect soap bubble, just hanging there.", done = "Pop: all of your health back, and squeaky clean.",
                use = (f, p, at) => { p.Health.Heal(99); } },
            new SecretKind { world = "attic", name = "an old photo album", colour = new Color(0.5f, 0.35f, 0.25f), line = "A photo album. Somebody very small is in a lot of these.", done = "You keep a photo. Finch will want to see it: 30 samples.",
                use = (f, p, at) => { RunState.AddSamples(30); } },
            new SecretKind { world = "shed", name = "a packet of seeds", colour = new Color(0.4f, 0.75f, 0.3f), line = "A packet of seeds. Magic beans, the label says.", done = "They sprout at once: the power is full.",
                use = (f, p, at) => { if (p.Power != null) p.Power.SetMeter(1f); } },
            new SecretKind { world = "bookshelf", name = "a hollow book", colour = new Color(0.15f, 0.4f, 0.25f), line = "A book that is lighter than it should be.", done = "Hollow, with a shortcut inside: you are by the way down.",
                use = (f, p, at) => { f.TeleportToExit(); } },
            new SecretKind { world = "sink", name = "the plughole", colour = new Color(0.1f, 0.1f, 0.12f), line = "The plughole. It gurgles, invitingly.", done = "Down you go: straight to the next floor.",
                use = (f, p, at) => { f.DropToNextFloor(); } },
        };

        public static SecretKind For(string world)
        {
            foreach (var kind in All) if (kind.world == world) return kind;
            return null;
        }

        public static WorldSecret Place(Room room, Vector3 at, SecretKind kind)
        {
            var go = new GameObject("World Secret");
            go.transform.SetParent(room.transform, false);
            go.transform.position = at;
            var secret = go.AddComponent<WorldSecret>();
            secret.kind = kind;
            // A small odd thing, a little glow to it, so a sharp eye picks it out.
            var look = Part.Make(PrimitiveType.Cube, "Look", go.transform, new Vector3(0f, 0.35f, 0f), new Vector3(0.45f, 0.45f, 0.45f), kind.colour);
            look.transform.localRotation = Quaternion.Euler(20f, 30f, 10f);
            secret._look = look.transform;
            Part.Make(PrimitiveType.Sphere, "Glint", go.transform, new Vector3(0.15f, 0.65f, 0f), Vector3.one * 0.1f, Color.white, true);
            return secret;
        }

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            if (Used) return;
            if (_look != null) _look.localPosition = new Vector3(0f, 0.35f + Mathf.Sin(Time.time * 2f) * 0.05f, 0f);
            var player = PlayerController.Instance;
            if (player != null && _pad.SteppedOn(transform.position, player.transform.position)) Use();
        }

        public void Use()
        {
            if (Used) return;
            Used = true;
            var floors = FloorManager.Instance;
            var player = PlayerController.Instance;
            var found = SaveSystem.Data.meta.worldSecrets;
            bool first = !found.Contains(kind.world);
            if (first) found.Add(kind.world);
            SaveSystem.Save();
            Sfx.Play(SfxId.PowerOn, 0.8f);
            Fx.Burst(transform.position + Vector3.up * 0.5f, kind.colour, 24, 6f, 0.15f, 0.6f);
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast($"{kind.done}" + (first ? $" (World secret: {Found} of {All.Length} found.)" : ""), 4f);
            if (floors != null && player != null) kind.use(floors, player, transform.position);
            if (this != null && gameObject != null) gameObject.SetActive(false);
        }

        string Prompt(Vector3 playerPosition)
        {
            if (Used || _pad.Distance(transform.position, playerPosition) > 2f) return null;
            return $"{kind.line}\n{Interact.Verb} to see.";
        }
    }
}
