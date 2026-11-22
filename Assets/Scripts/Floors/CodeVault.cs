using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Vault: now and then a floor has a dead end shut behind a heavy door with three coloured dials beside it.
    /// The code (three colours, in order) is chalked on a slate in another room of the same floor. Set the dials to it
    /// and the door opens on a trial: three plates on the floor and a beacon in the middle that shows an order; step
    /// on the plates in that order, three rounds, each longer, while things come at you. A wrong plate shows the
    /// order again and brings more company. Done, it pays well: something from the boss pool, samples and coins.
    ///
    /// Built to grow into a co-op encounter one day: each plate is a place to stand and each round a call-out, so with
    /// more players they can split the plates between them.
    /// </summary>
    public class CodeVault : MonoBehaviour
    {
        public static readonly Color[] Colours = { new Color(0.95f, 0.25f, 0.25f), new Color(0.3f, 0.55f, 1f), new Color(1f, 0.85f, 0.2f), new Color(0.35f, 0.85f, 0.35f) };
        public static readonly string[] ColourNames = { "red", "blue", "yellow", "green" };
        public const float Chance = 0.25f;
        /// <summary>Tests pin it: 0 never, 1 always (where the floor has a dead end to spare).</summary>
        public static float? ChanceForAll;
        public const int Rounds = 3, Samples = 30, Coins = 10;

        public Room room;
        public RoomNode node;
        public int[] code = new int[3];
        public bool Open { get; private set; }
        public bool Cleared { get; private set; }
        public int Round { get; private set; }
        /// <summary>The order to step in for this round (plate numbers 0-2).</summary>
        public List<int> Order { get; } = new List<int>();
        public int Stepped { get; private set; }
        public int Mistakes { get; private set; }

        GameObject _door;
        readonly Dial[] _dials = new Dial[3];
        readonly Plate[] _plates = new Plate[3];
        Renderer _beacon;
        float _showUntil, _showStarted;
        bool _started;

        class Dial
        {
            public Transform at;
            public Renderer lamp;
            public int value;
            public readonly StepPad pad = new StepPad { radius = 0.55f };
        }

        class Plate
        {
            public Vector3 at;
            public Renderer ring;
            public bool on;
        }

        /// <summary>Shuts this dead end behind a coded door, and chalks the code up in another room.</summary>
        public static CodeVault Build(Room room, RoomNode node, Room clueRoom, int seed)
        {
            GameObject door = null;
            for (int side = 0; side < 4 && door == null; side++) door = room.DoorObject(side);
            if (door == null) return null;

            var rng = new System.Random(seed);
            var go = new GameObject("Code Vault");
            go.transform.SetParent(room.transform, false);
            go.transform.position = room.Centre;
            var vault = go.AddComponent<CodeVault>();
            vault.room = room;
            vault.node = node;
            for (int i = 0; i < 3; i++) vault.code[i] = rng.Next(Colours.Length);
            // Never all three the same, or it is too easy to stumble on.
            if (vault.code[0] == vault.code[1] && vault.code[1] == vault.code[2]) vault.code[2] = (vault.code[2] + 1) % Colours.Length;

            // The door: heavy, dark, banded.
            door.name = "Vault Door";
            door.SetActive(true);
            vault._door = door;
            door.GetComponent<Renderer>().sharedMaterial = Flat(new Color(0.22f, 0.22f, 0.26f));

            // Outside it, three dials in a row.
            Vector3 outward = door.transform.position - room.Centre;
            outward.y = 0f;
            outward.Normalize();
            Vector3 across = Vector3.Cross(Vector3.up, outward);
            for (int i = 0; i < 3; i++)
            {
                var dial = new Dial();
                var post = new GameObject("Dial " + (i + 1)).transform;
                post.SetParent(room.transform, false);
                post.position = new Vector3(door.transform.position.x, 0f, door.transform.position.z) + outward * 1.9f + across * ((i - 1) * 1.5f);
                Part.Make(PrimitiveType.Cylinder, "Post", post, new Vector3(0f, 0.5f, 0f), new Vector3(0.18f, 0.5f, 0.18f), new Color(0.3f, 0.3f, 0.33f));
                dial.lamp = Part.Make(PrimitiveType.Sphere, "Lamp", post, new Vector3(0f, 1.15f, 0f), Vector3.one * 0.38f, Colours[0], true).GetComponent<Renderer>();
                Label(post, (i + 1).ToString(), new Vector3(0f, 1.6f, 0f), Color.white);
                dial.at = post;
                vault._dials[i] = dial;
            }
            vault.ShowDials();

            // The code, chalked on a slate somewhere else on the floor.
            if (clueRoom != null)
            {
                Vector3 spot = clueRoom.Centre + new Vector3(2.5f, 0f, 2.5f);
                if (clueRoom.Shape.free.Count > 0)
                {
                    var tile = clueRoom.Shape.free[rng.Next(clueRoom.Shape.free.Count)];
                    spot = clueRoom.TileWorld(tile);
                }
                var slate = new GameObject("Vault Code Slate");
                slate.transform.SetParent(clueRoom.transform, false);
                slate.transform.position = spot;
                Part.Make(PrimitiveType.Cube, "Slate", slate.transform, new Vector3(0f, 0.9f, 0f), new Vector3(1.5f, 1f, 0.1f), new Color(0.12f, 0.14f, 0.13f));
                Part.Make(PrimitiveType.Cube, "Stand", slate.transform, new Vector3(0f, 0.2f, 0.25f), new Vector3(0.1f, 0.4f, 0.5f), new Color(0.4f, 0.3f, 0.2f));
                for (int i = 0; i < 3; i++)
                {
                    Part.Make(PrimitiveType.Sphere, "Mark " + (i + 1), slate.transform, new Vector3((i - 1) * 0.45f, 0.95f, -0.07f), new Vector3(0.3f, 0.3f, 0.05f), Colours[vault.code[i]], true);
                    Label(slate.transform, (i + 1).ToString(), new Vector3((i - 1) * 0.45f, 1.3f, -0.07f), Color.white, false);
                }
                vault.Clue = slate.AddComponent<VaultClue>();
                vault.Clue.vault = vault;
            }

            // Inside: three plates and the beacon, waiting.
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f + 90f;
                Vector3 at = room.Centre + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 4.5f;
                var plate = new Plate { at = at };
                plate.ring = Part.Make(PrimitiveType.Cylinder, "Plate " + (i + 1), go.transform, Vector3.zero, new Vector3(2.2f, 0.03f, 2.2f), Colours[i] * 0.5f, true).GetComponent<Renderer>();
                plate.ring.transform.position = at + Vector3.up * 0.03f;
                vault._plates[i] = plate;
            }
            vault._beacon = Part.Make(PrimitiveType.Sphere, "Beacon", go.transform, new Vector3(0f, 2.2f, 0f), Vector3.one * 0.8f, new Color(0.3f, 0.3f, 0.3f), true).GetComponent<Renderer>();
            Part.Make(PrimitiveType.Cylinder, "Plinth", go.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.6f, 0.9f, 0.6f), new Color(0.3f, 0.3f, 0.35f));
            return vault;
        }

        public VaultClue Clue { get; private set; }

        public string CodeWords => $"{ColourNames[code[0]]}, then {ColourNames[code[1]]}, then {ColourNames[code[2]]}";

        static Material Flat(Color colour)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader != null ? shader : Shader.Find("Standard"));
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
            material.color = colour;
            return material;
        }

        static void Label(Transform parent, string text, Vector3 at, Color colour, bool face = true)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            var mesh = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            mesh.text = text;
            mesh.fontSize = 48;
            mesh.characterSize = 0.05f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.color = colour;
            if (face) go.AddComponent<FaceCamera>();
            else go.transform.localRotation = Quaternion.identity;
        }

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            Vector3 at = player.transform.position;

            if (!Open)
            {
                for (int i = 0; i < 3; i++)
                {
                    var dial = _dials[i];
                    if (dial.pad.SteppedOn(dial.at.position, at)) Turn(i);
                }
                return;
            }
            if (Cleared) return;

            // The trial starts when the player is well inside.
            Vector3 fromMiddle = at - room.Centre;
            fromMiddle.y = 0f;
            if (!_started && fromMiddle.magnitude < 7f) StartRound(1);
            if (!_started) return;

            // The beacon plays the order, one colour at a time; then it waits on the plates.
            if (Time.time < _showUntil)
            {
                float step = (Time.time - _showStarted) / 0.8f;
                int index = Mathf.FloorToInt(step);
                bool lit = index >= 0 && step - index < 0.7f && index < Order.Count;
                SetBeacon(lit ? Colours[Order[index]] : new Color(0.25f, 0.25f, 0.25f));
                return;
            }
            SetBeacon(new Color(0.85f, 0.85f, 0.85f));

            for (int i = 0; i < 3; i++)
            {
                var plate = _plates[i];
                Vector3 to = at - plate.at;
                to.y = 0f;
                bool on = to.magnitude <= 1.15f;
                if (on && !plate.on) StepOn(i);
                plate.on = on;
                plate.ring.sharedMaterial.color = Colours[i] * (on ? 1.2f : 0.5f);
                if (plate.ring.sharedMaterial.HasProperty("_BaseColor")) plate.ring.sharedMaterial.SetColor("_BaseColor", Colours[i] * (on ? 1.2f : 0.5f));
            }
        }

        /// <summary>Turns a dial on to its next colour; with all three right, the door opens.</summary>
        public void Turn(int dial)
        {
            if (Open) return;
            _dials[dial].value = (_dials[dial].value + 1) % Colours.Length;
            ShowDials();
            Sfx.Play(SfxId.Dodge, 0.4f);
            if (_dials[0].value == code[0] && _dials[1].value == code[1] && _dials[2].value == code[2]) Unlock();
        }

        void ShowDials()
        {
            foreach (var dial in _dials)
            {
                if (dial == null) continue;
                var material = dial.lamp.sharedMaterial;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Colours[dial.value]);
                material.color = Colours[dial.value];
            }
        }

        void Unlock()
        {
            Open = true;
            if (_door != null) _door.SetActive(false);
            if (node != null) node.visited = true;
            Sfx.Play(SfxId.PowerOn);
            if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.3f);
            SaveSystem.Data.meta.vaultsOpened++;
            SaveSystem.Save();
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast("The vault opens. Inside, a beacon and three plates. Watch the beacon, then step on the plates in its order.", 5f);
        }

        void StartRound(int round)
        {
            _started = true;
            Round = round;
            Order.Clear();
            int length = 2 + round;
            for (int i = 0; i < length; i++)
            {
                int next = Random.Range(0, 3);
                if (i > 0 && next == Order[i - 1]) next = (next + 1) % 3;
                Order.Add(next);
            }
            Stepped = 0;
            ShowOrder();
            // Company, a little more each round.
            var floors = FloorManager.Instance;
            if (floors != null) floors.SpawnInto(room, round);
        }

        void ShowOrder()
        {
            _showStarted = Time.time + 0.5f;
            _showUntil = _showStarted + Order.Count * 0.8f;
            foreach (var plate in _plates) plate.on = true; // nobody counts as stepping on until they step off and back
        }

        /// <summary>The player has stepped on a plate: the right one moves the round on; the wrong one starts it over.</summary>
        public void StepOn(int plate)
        {
            if (!Open || Cleared || !_started || Time.time < _showUntil) return;
            if (Order[Stepped] == plate)
            {
                Stepped++;
                Sfx.Play(SfxId.Item, 0.4f);
                if (Stepped < Order.Count) return;
                if (Round < Rounds) StartRound(Round + 1);
                else Finish();
                return;
            }
            Mistakes++;
            Stepped = 0;
            Sfx.Play(SfxId.Blocked);
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast("Wrong plate. The beacon shows the order again, and something else turns up.", 2.5f);
            ShowOrder();
            var floors = FloorManager.Instance;
            if (floors != null) floors.SpawnInto(room, 1);
        }

        void SetBeacon(Color colour)
        {
            if (_beacon == null) return;
            var material = _beacon.sharedMaterial;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
            material.color = colour;
        }

        void Finish()
        {
            Cleared = true;
            SetBeacon(new Color(1f, 0.85f, 0.3f));
            Sfx.Play(SfxId.BossAppear, 0.6f);
            var meta = SaveSystem.Data.meta;
            meta.vaultsCleared++;
            SaveSystem.Save();

            // The pay: something from the boss pool if there is anything in it yet, otherwise any item; samples; coins.
            var assets = GameAssets.Instance;
            Vector3 middle = room.Centre;
            if (assets != null && assets.items != null)
            {
                ItemDefinition prize = null;
                var pool = BossEnemy.Pool();
                if (pool.Count > 0) prize = assets.items.Get(pool[Random.Range(0, pool.Count)]);
                if (prize == null)
                {
                    var picks = assets.items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), 1);
                    if (picks.Count > 0) prize = picks[0];
                }
                if (prize != null) Pickup.Item(prize, middle + new Vector3(0f, 0f, -1.6f), 0);
            }
            Pickup.Sample(middle + new Vector3(1.8f, 0f, 0f), Samples);
            for (int i = 0; i < Coins; i++) Pickup.Coin(middle + new Vector3(-1.8f + (i % 4) * 0.4f, 0f, -0.6f + (i / 4) * 0.6f), 1);
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast(Mistakes == 0 ? "Vault cleared without a slip. Help yourself." : "Vault cleared. Help yourself.", 4f);
        }

        string Prompt(Vector3 playerPosition)
        {
            if (!Open)
            {
                foreach (var dial in _dials)
                {
                    if (dial.pad.Distance(dial.at.position, playerPosition) > 1.4f) continue;
                    return $"A vault door, and three dials: {ColourNames[_dials[0].value]}, {ColourNames[_dials[1].value]}, {ColourNames[_dials[2].value]}.\n{Interact.Verb} to turn the nearest. The code is somewhere on this floor.";
                }
                return null;
            }
            if (Cleared) return null;
            Vector3 to = playerPosition - room.Centre;
            to.y = 0f;
            if (to.magnitude > 8f) return null;
            return Time.time < _showUntil
                ? $"Round {Round} of {Rounds}: watch the beacon."
                : $"Round {Round} of {Rounds}: step on the plates in the beacon's order ({Stepped} of {Order.Count}).";
        }
    }

    /// <summary>The slate with the vault's code on it.</summary>
    public class VaultClue : MonoBehaviour
    {
        public CodeVault vault;

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        string Prompt(Vector3 playerPosition)
        {
            Vector3 to = playerPosition - transform.position;
            to.y = 0f;
            if (to.magnitude > 2.5f || vault == null) return null;
            return vault.Open
                ? "The slate's code: already used."
                : $"Chalked on a slate: {vault.CodeWords}. A code for something on this floor.";
        }
    }
}
