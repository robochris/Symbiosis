using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The machine's room: off the map on a boss floor, from the second world of a run on, once all four parts are in
    /// hand. Walking in fits the parts to the frame. The portal in it stays dark until the floor's boss is beaten;
    /// then using it goes to the machine, with everything the run is carrying. Afterwards the way back to the run is
    /// open as well as the way home.
    /// </summary>
    public class MachineRoom : MonoBehaviour
    {
        public StepPad pad = new StepPad { radius = 1.5f };
        public Room room;
        public GameObject dark, lit;
        public GameObject[] parts;

        /// <summary>True once the player has walked in and the parts are fitted.</summary>
        public bool Built { get; private set; }
        public bool Ready => Built && FloorManager.Instance != null && FloorManager.Instance.ExitOpen;

        public static MachineRoom Create(Room room, Vector3 centre)
        {
            var go = new GameObject("Machine Frame");
            go.transform.SetParent(room.transform, false);
            go.transform.position = centre;
            var machine = go.AddComponent<MachineRoom>();
            machine.room = room;

            var brass = Lit(new Color(0.7f, 0.55f, 0.25f), 0.8f);
            var steel = Lit(new Color(0.3f, 0.32f, 0.36f), 0.7f);
            Piece(PrimitiveType.Cube, go.transform, new Vector3(-1.4f, 1.6f, 0f), new Vector3(0.35f, 3.2f, 0.5f), steel);
            Piece(PrimitiveType.Cube, go.transform, new Vector3(1.4f, 1.6f, 0f), new Vector3(0.35f, 3.2f, 0.5f), steel);
            Piece(PrimitiveType.Cube, go.transform, new Vector3(0f, 3.3f, 0f), new Vector3(3.2f, 0.35f, 0.5f), brass);
            machine.dark = Piece(PrimitiveType.Cube, go.transform, new Vector3(0f, 1.6f, 0f), new Vector3(2.4f, 3f, 0.1f), Unlit(new Color(0.05f, 0.05f, 0.07f)));
            machine.lit = Piece(PrimitiveType.Cube, go.transform, new Vector3(0f, 1.6f, 0f), new Vector3(2.4f, 3f, 0.1f), Unlit(new Color(0.45f, 1f, 0.6f)));
            machine.lit.SetActive(false);

            // The four sockets along the top: empty until the player walks in with the parts.
            Color[] colours = { new Color(1f, 0.85f, 0.3f), new Color(0.4f, 0.8f, 1f), new Color(1f, 0.45f, 0.35f), new Color(0.7f, 0.95f, 1f) };
            machine.parts = new GameObject[MachineParts.All.Length];
            for (int i = 0; i < machine.parts.Length; i++)
            {
                machine.parts[i] = Piece(PrimitiveType.Sphere, go.transform, new Vector3(-1.05f + i * 0.7f, 3.75f, 0f), Vector3.one * 0.38f, Unlit(colours[i % colours.Length]));
                machine.parts[i].SetActive(false);
            }
            return machine;
        }

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var floors = FloorManager.Instance;
            var player = PlayerController.Instance;
            if (floors == null || player == null) return;
            if (!Built && floors.CurrentRoom == room) Build();
            if (dark != null) dark.SetActive(!Ready);
            if (lit != null) lit.SetActive(Ready);
            if (!pad.SteppedOn(transform.position, player.transform.position)) return;
            if (!Ready)
            {
                Sfx.Play(SfxId.Blocked);
                return;
            }
            floors.EnterSpecial(floors.machineBiome);
        }

        void Build()
        {
            Built = true;
            foreach (var part in parts) if (part != null) part.SetActive(true);
            Sfx.Play(SfxId.PowerOn);
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast("The machine. Fuse, coil, dial, lens: they go in with a click each. It will run once this floor's boss is down.", 6f);
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 3.5f) return null;
            return Ready
                ? $"The machine, built and humming. {Interact.Verb} to go through and switch it on. Bring everything you have."
                : "The machine, built. It will run once this floor's boss is down.";
        }

        internal static GameObject Piece(PrimitiveType shape, Transform parent, Vector3 at, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(shape);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        internal static Material Lit(Color colour, float smoothness)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", colour);
            material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        internal static Material Unlit(Color colour)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", colour);
            return material;
        }
    }

    /// <summary>
    /// The shop's cat flap: now and then, once Dr. Finch's notebook is complete, a shop has a cardboard box in the
    /// corner with a flap in it and a sign saying who is really in charge. Through it is the cat. Afterwards the way
    /// back to the run is open as well as the way home.
    /// </summary>
    public class CatFlap : MonoBehaviour
    {
        public StepPad pad = new StepPad { radius = 1.2f };

        public static CatFlap Create(Room room, Vector3 position)
        {
            var go = new GameObject("Cat Flap");
            go.transform.SetParent(room.transform, false);
            go.transform.position = position;
            var flap = go.AddComponent<CatFlap>();

            var cardboard = MachineRoom.Lit(new Color(0.72f, 0.55f, 0.35f), 0.05f);
            MachineRoom.Piece(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.9f, 0f), new Vector3(2f, 1.8f, 1.6f), cardboard);
            MachineRoom.Piece(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.55f, -0.81f), new Vector3(0.8f, 0.8f, 0.04f), MachineRoom.Unlit(new Color(0.08f, 0.06f, 0.05f)));

            var sign = new GameObject("Sign");
            sign.transform.SetParent(go.transform, false);
            sign.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            var text = sign.AddComponent<TextMesh>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font;
            sign.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            text.text = "MANAGEMENT";
            text.fontSize = 48;
            text.characterSize = 0.05f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.9f, 0.6f);
            sign.AddComponent<FaceCamera>();
            return flap;
        }

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var floors = FloorManager.Instance;
            var player = PlayerController.Instance;
            if (floors == null || player == null || floors.IsLocked) return;
            if (!pad.SteppedOn(transform.position, player.transform.position)) return;
            floors.EnterSpecial(floors.bottomBiome);
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 2.6f) return null;
            return SaveSystem.Data.meta.finchBeaten > 0
                ? $"The box, the flap, the sign: management is in. {Interact.Verb} to go through. It remembers you."
                : $"A cardboard box with a cat flap in it, and a sign: MANAGEMENT. Something inside is purring. {Interact.Verb} to go through.";
        }
    }

    /// <summary>Keeps a sign turned to the camera.</summary>
    public class FaceCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            var camera = Camera.main;
            if (camera != null) transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position);
        }
    }
}
