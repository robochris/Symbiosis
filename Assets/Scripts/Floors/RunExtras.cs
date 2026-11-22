using System;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// A rescue: now and then a fight room has a little cage in it with a larva inside. Clear the room and the cage
    /// opens; the larva comes along and bites things for the rest of the floor.
    /// </summary>
    public class RescueCage : MonoBehaviour
    {
        public const float Chance = 0.08f;
        public static float? ChanceForAll;

        public Room room;
        public RoomNode node;
        public bool Opened { get; private set; }
        public LarvaBuddy Freed { get; private set; }
        Transform _bars;

        public static RescueCage Create(Room room, RoomNode node, Vector3 position)
        {
            var go = new GameObject("Rescue Cage");
            go.transform.SetParent(room.transform, false);
            go.transform.position = position;
            var cage = go.AddComponent<RescueCage>();
            cage.room = room;
            cage.node = node;
            var metal = new Color(0.55f, 0.55f, 0.6f);
            Part.Make(PrimitiveType.Cylinder, "Base", go.transform, new Vector3(0f, 0.05f, 0f), new Vector3(1.2f, 0.05f, 1.2f), metal);
            Part.Make(PrimitiveType.Cylinder, "Lid", go.transform, new Vector3(0f, 1.3f, 0f), new Vector3(1.2f, 0.05f, 1.2f), metal);
            cage._bars = new GameObject("Bars").transform;
            cage._bars.SetParent(go.transform, false);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                Part.Make(PrimitiveType.Cylinder, "Bar " + i, cage._bars, new Vector3(Mathf.Sin(a) * 0.55f, 0.68f, Mathf.Cos(a) * 0.55f), new Vector3(0.05f, 0.62f, 0.05f), metal);
            }
            Part.Make(PrimitiveType.Sphere, "Larva", go.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.35f, 0.3f, 0.5f), new Color(0.95f, 0.9f, 0.7f));
            return cage;
        }

        void OnEnable()
        {
            WorldPrompt.Add(Prompt, transform);
            FloorManager.FloorBuilt += OnFloorBuilt;
        }

        void OnDisable()
        {
            WorldPrompt.Remove(Prompt);
            FloorManager.FloorBuilt -= OnFloorBuilt;
        }

        // A new floor: the larva goes home.
        void OnFloorBuilt(FloorManager floors)
        {
            if (Freed != null) Destroy(Freed.gameObject);
        }

        void Update()
        {
            if (Opened || node == null || !node.cleared) return;
            var floors = FloorManager.Instance;
            if (floors != null && floors.IsLocked) return;
            Open();
        }

        /// <summary>Lets it out: it follows the player for the rest of the floor.</summary>
        public void Open()
        {
            if (Opened) return;
            Opened = true;
            if (_bars != null) _bars.gameObject.SetActive(false);
            var larva = transform.Find("Larva");
            if (larva != null) larva.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            var assets = GameAssets.Instance;
            if (player != null && player.Items != null && assets != null && assets.larvaPrefab != null)
            {
                Freed = Instantiate(assets.larvaPrefab, transform.position, Quaternion.identity);
                Freed.owner = player.Items;
                // It goes home with the next floor.
                Freed.gameObject.AddComponent<FloorOnly>();
            }
            Sfx.Play(SfxId.Item, 0.6f);
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast("You let the larva out. It is coming with you for the rest of this floor.", 3.5f);
        }

        string Prompt(Vector3 playerPosition)
        {
            Vector3 to = playerPosition - transform.position;
            to.y = 0f;
            if (to.magnitude > 2.5f || Opened) return null;
            return "A larva, in a cage. It looks at you hopefully. Clear the room and the cage will open.";
        }
    }

    /// <summary>Gone when the next floor is built.</summary>
    public class FloorOnly : MonoBehaviour
    {
        void OnEnable() => FloorManager.FloorBuilt += Gone;
        void OnDisable() => FloorManager.FloorBuilt -= Gone;
        void Gone(FloorManager floors) => Destroy(gameObject);
    }

    /// <summary>
    /// The rule of the day: one small rule that changes every run started today, the same for everybody, picked by
    /// the date. Tomorrow, another. Daily runs and the Gauntlet keep their own rules.
    /// </summary>
    public static class RunRules
    {
        public enum Rule { None, Bouncy, GlassCannon, GoldRush, QuickFeet, SharpEyes, LongShot, Sticky }

        public static readonly string[] Names = { "", "Bouncy", "Glass Cannon", "Gold Rush", "Quick Feet", "Sharp Eyes", "Long Shot", "Sticky Fingers" };
        public static readonly string[] Lines =
        {
            "",
            "Every shot bounces off one wall.",
            "Shots hit one harder, and you start with one less health.",
            "Every coin drop is one more.",
            "You move a fifth faster.",
            "One shot in ten hits for double.",
            "Shots hit harder the further they fly.",
            "Coins and samples come to you from twice as far.",
        };

        /// <summary>Tests switch it off, so the stats they read are their own.</summary>
        public static bool Off;
        /// <summary>The rule in force this run (None in the lab and on daily or Gauntlet runs).</summary>
        public static Rule Active { get; private set; }
        public static DateTime? TodayOverride;

        public static Rule Today
        {
            get
            {
                DateTime day = (TodayOverride ?? DateTime.Now).Date;
                int index = 1 + (int)((day - new DateTime(2026, 1, 1)).TotalDays % (Names.Length - 1) + Names.Length - 1) % (Names.Length - 1);
                return (Rule)index;
            }
        }

        /// <summary>Puts today's rule in force for this run, or none.</summary>
        public static void Begin(bool applies)
        {
            Active = Off || !applies ? Rule.None : Today;
        }

        public static void End() => Active = Rule.None;

        public static string Describe(Rule rule) => rule == Rule.None ? "" : $"{Names[(int)rule]}: {Lines[(int)rule]}";
    }
}
