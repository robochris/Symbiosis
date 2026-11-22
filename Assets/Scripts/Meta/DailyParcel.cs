using System;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// A parcel left in the lab once a day: samples, more for each day in a row it is collected (up to a week), and
    /// on the seventh day a lucky coin for the next run as well. It is put down by the lab when it opens, beside where
    /// the player starts, and is gone once opened until tomorrow.
    /// </summary>
    public class DailyParcel : MonoBehaviour
    {
        public const int BaseSamples = 20;
        public const int PerDay = 10;
        public const int MaxStreak = 7;
        public const int LuckyCoins = 15;

        /// <summary>Tests set this to pretend it is another day.</summary>
        public static DateTime? TodayOverride;
        /// <summary>Tests switch it off, so a parcel by the door never changes what they count.</summary>
        public static bool Off;
        static DateTime Today => (TodayOverride ?? DateTime.Now).Date;
        static string Key(DateTime day) => day.ToString("yyyy-MM-dd");

        public readonly StepPad pad = new StepPad { radius = 1f };
        public static DailyParcel Current { get; private set; }

        /// <summary>True if today's parcel has not been opened yet.</summary>
        public static bool Waiting => SaveSystem.Data.meta.parcelDate != Key(Today);

        /// <summary>The streak the parcel waiting now would make, counting today.</summary>
        public static int StreakIfOpened
        {
            get
            {
                MetaSave meta = SaveSystem.Data.meta;
                bool yesterday = meta.parcelDate == Key(Today.AddDays(-1));
                return yesterday ? Mathf.Min(meta.parcelStreak + 1, MaxStreak) : 1;
            }
        }

        public static int SamplesFor(int streak) => BaseSamples + PerDay * (Mathf.Clamp(streak, 1, MaxStreak) - 1);

        /// <summary>Puts today's parcel down here, if there is one to put down.</summary>
        public static DailyParcel Place(Vector3 position)
        {
            if (Off || !Waiting) return null;
            var go = new GameObject("Daily Parcel");
            go.transform.position = position;
            var box = Part(PrimitiveType.Cube, "Box", go.transform, new Vector3(0f, 0.45f, 0f), new Vector3(1f, 0.9f, 0.8f), new Color(0.72f, 0.55f, 0.35f));
            Part(PrimitiveType.Cube, "Ribbon A", go.transform, new Vector3(0f, 0.46f, 0f), new Vector3(1.02f, 0.92f, 0.16f), new Color(0.9f, 0.2f, 0.3f));
            Part(PrimitiveType.Cube, "Ribbon B", go.transform, new Vector3(0f, 0.46f, 0f), new Vector3(0.16f, 0.92f, 0.82f), new Color(0.9f, 0.2f, 0.3f));
            Part(PrimitiveType.Sphere, "Bow", go.transform, new Vector3(0f, 0.95f, 0f), new Vector3(0.4f, 0.2f, 0.3f), new Color(0.95f, 0.3f, 0.4f));
            // Under it, the mat the use markers look for: it becomes the floating diamond.
            Part(PrimitiveType.Cylinder, "Mat", go.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.6f, 0.02f, 1.6f), new Color(0.95f, 0.35f, 0.45f));
            box.AddComponent<BoxCollider>();
            var parcel = go.AddComponent<DailyParcel>();
            UseMarkers.Rescan();
            return parcel;
        }

        static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 at, Vector3 scale, Color colour)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            UnityEngine.Object.Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = at;
            part.transform.localScale = scale;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader != null ? shader : Shader.Find("Standard"));
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
            material.color = colour;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
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
            if (player != null && pad.SteppedOn(transform.position, player.transform.position)) Open();
        }

        /// <summary>Opens it: the samples go straight into the lab's stock. Returns what was in it.</summary>
        public int Open()
        {
            if (!Waiting) return 0;
            MetaSave meta = SaveSystem.Data.meta;
            int streak = StreakIfOpened;
            int samples = SamplesFor(streak);
            meta.parcelStreak = streak;
            meta.parcelDate = Key(Today);
            meta.samples += samples;
            bool week = streak >= MaxStreak;
            if (week) meta.luckyCoins += LuckyCoins;
            SaveSystem.Save();

            var hud = FindAnyObjectByType<Hud>();
            string days = streak == 1 ? "Come back tomorrow for a bigger one." : $"Day {streak} in a row.";
            if (hud != null) hud.Toast($"Today's parcel: {samples} samples. {days}" + (week ? $" And {LuckyCoins} lucky coins for your next run." : ""), 5f);
            Sfx.Play(SfxId.Item);
            Destroy(gameObject);
            return samples;
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 2f) return null;
            int streak = StreakIfOpened;
            string tail = streak >= MaxStreak ? " A full week: lucky coins inside too." : streak > 1 ? $" Day {streak} in a row." : "";
            return $"A parcel, addressed to you. {SamplesFor(streak)} samples inside.{tail}\n{Interact.Verb} to open it.";
        }
    }
}
