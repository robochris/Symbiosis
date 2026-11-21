using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// A pond in a forest fight room, now and then. Wading slows the player down. Once the room is clear there is a
    /// glint in the middle of it: stand there and use it to dive, and come up with an item and a handful of samples.
    /// Once a pond.
    /// </summary>
    public class ForestPond : MonoBehaviour
    {
        public const float Radius = 1.8f;
        public const int DiveSamples = 8;
        [Tooltip("Chance a forest floor (from its second floor on) has a pond in one of its fight rooms.")]
        public static float Chance = 0.4f;
        /// <summary>Tests set this to make a pond certain (1) or impossible (0).</summary>
        public static float? ChanceForAll;

        public StepPad pad = new StepPad { radius = 0.9f };
        public Room room;
        public GameObject glint;

        public bool Dived { get; private set; }

        public static ForestPond Create(Room room, Vector3 position)
        {
            var go = new GameObject("Pond");
            go.transform.SetParent(room.transform, false);
            go.transform.position = position;
            var pond = go.AddComponent<ForestPond>();
            pond.room = room;
            var water = MachineRoom.Lit(new Color(0.18f, 0.38f, 0.42f), 0.95f);
            MachineRoom.Piece(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.03f, 0f), new Vector3(Radius * 2f, 0.02f, Radius * 2f), water);
            MachineRoom.Piece(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.02f, 0f), new Vector3(Radius * 2f + 0.4f, 0.015f, Radius * 2f + 0.4f), MachineRoom.Lit(new Color(0.3f, 0.24f, 0.15f), 0.1f));
            pond.glint = MachineRoom.Piece(PrimitiveType.Sphere, go.transform, new Vector3(0f, 0.08f, 0f), new Vector3(0.35f, 0.05f, 0.35f), MachineRoom.Unlit(new Color(1f, 0.95f, 0.6f)));
            pond.glint.SetActive(false);
            return pond;
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
            // Wading is slow going. A dodge or a hover keeps the feet dry.
            if (to.magnitude <= Radius && !player.IsHovering) player.Slow(0.6f, 0.2f);

            bool ready = !Dived && floors.CurrentRoom == room && !floors.IsLocked;
            if (glint != null && glint.activeSelf != ready) glint.SetActive(ready);
            if (glint != null && ready) glint.transform.localScale = new Vector3(0.35f, 0.05f, 0.35f) * (1f + 0.2f * Mathf.Sin(Time.time * 6f));
            if (!ready || !pad.SteppedOn(transform.position, player.transform.position)) return;
            Dive();
        }

        /// <summary>Under and back up with whatever was down there. Once.</summary>
        public void Dive()
        {
            if (Dived) return;
            Dived = true;
            Jobs.Count(JobKind.Dives);
            var assets = GameAssets.Instance;
            Vector3 bank = transform.position + new Vector3(0f, 0f, -(Radius + 1.2f));
            if (assets != null && assets.items != null)
            {
                var picks = assets.items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), 1);
                if (picks.Count > 0) Pickup.Item(picks[0], bank, 0);
            }
            Pickup.Sample(bank + Vector3.right * 1.2f, DiveSamples);
            Sfx.Play(SfxId.Item);
            Fx.Burst(transform.position + Vector3.up * 0.3f, new Color(0.5f, 0.8f, 1f), 18, 4f, 0.15f, 0.5f);
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast("You dive. It is deeper than it looks, and something down there was shiny.", 4f);
        }

        string Prompt(Vector3 playerPosition)
        {
            Vector3 to = playerPosition - transform.position;
            to.y = 0f;
            if (to.magnitude > Radius + 1f) return null;
            if (Dived) return "The pond. You have already been to the bottom of it.";
            var floors = FloorManager.Instance;
            if (floors != null && floors.CurrentRoom == room && floors.IsLocked) return "A pond. Wading is slow. Deal with the room first.";
            return $"A pond, and something glinting in the middle. Stand on the glint and {Interact.Verb.ToLower()} to dive.";
        }
    }
}
