using System;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Seasons: four weeks long, the same for everybody. Every XP earned also counts toward the season's track of 30
    /// levels; each level pays a little (samples, and every fifth, lucky coins for the next run), and the thirtieth
    /// pays a Season Star, worn as a hat. A new season starts the track again. The board by the lab door shows it.
    /// </summary>
    public static class Season
    {
        public const int Days = 28, Levels = 30, XpPerLevel = 100, Samples = 15, Coins = 10;
        static readonly DateTime Start = new DateTime(2026, 11, 2);
        public static DateTime? NowOverride;
        /// <summary>Tests switch it off, so XP pays only what they expect.</summary>
        public static bool Off;
        static DateTime Now => NowOverride ?? DateTime.Now;

        public static int Id => (int)Math.Floor((Now.Date - Start).TotalDays / Days) + 1;
        public static int DaysLeft => Days - (int)((Now.Date - Start).TotalDays % Days);

        static MetaSave Meta => SaveSystem.Data.meta;

        /// <summary>A new season since the save last looked: the track starts again.</summary>
        static void Catch()
        {
            if (Meta.seasonId == Id) return;
            Meta.seasonId = Id;
            Meta.seasonXp = 0;
        }

        public static int Xp
        {
            get
            {
                Catch();
                return Meta.seasonXp;
            }
        }

        public static int Level => Mathf.Min(Levels, Xp / XpPerLevel);

        /// <summary>What a level of the track pays, in words.</summary>
        public static string Reward(int level) => level >= Levels ? "a Season Star" : level % 5 == 0 ? $"{Coins} lucky coins" : $"{Samples} samples";

        /// <summary>Counts XP toward the season and pays for every level passed. Called with every XP the game gives.</summary>
        public static void Add(int amount)
        {
            if (amount <= 0 || Off) return;
            Catch();
            int before = Level;
            Meta.seasonXp += amount;
            int after = Level;
            for (int level = before + 1; level <= after; level++)
            {
                if (level >= Levels) Meta.seasonStars++;
                else if (level % 5 == 0) Meta.luckyCoins += Coins;
                else Meta.samples += Samples;
                var hud = UnityEngine.Object.FindAnyObjectByType<Hud>();
                if (hud != null) hud.Toast($"Season {Id}, level {level} of {Levels}: {Reward(level)}.", 3.5f);
            }
        }

        public static string Summary()
        {
            int level = Level;
            if (level >= Levels) return $"SEASON {Id}\nThe track is done: a Season Star is yours.\n{DaysLeft} days until the next.";
            int into = Xp - level * XpPerLevel;
            return $"SEASON {Id}  ·  {DaysLeft} days left\nLevel {level} of {Levels}  ({into}/{XpPerLevel} XP)\nNext: {Reward(level + 1)}";
        }
    }

    /// <summary>The season board by the lab door.</summary>
    public class SeasonBoard : MonoBehaviour
    {
        TextMesh _text;
        public static SeasonBoard Current { get; private set; }

        public static SeasonBoard Place(Vector3 position, Vector3 lookFrom)
        {
            var go = new GameObject("Season Board");
            go.transform.position = position;
            Part.Make(PrimitiveType.Cube, "Board", go.transform, new Vector3(0f, 1.7f, 0f), new Vector3(1.8f, 1f, 0.08f), new Color(0.15f, 0.2f, 0.35f));
            Part.Make(PrimitiveType.Cube, "Leg", go.transform, new Vector3(0f, 0.6f, 0.05f), new Vector3(0.12f, 1.2f, 0.12f), new Color(0.3f, 0.3f, 0.35f));
            Part.Make(PrimitiveType.Cube, "Bar", go.transform, new Vector3(0f, 1.22f, -0.05f), new Vector3(1.6f, 0.08f, 0.02f), new Color(0.3f, 0.3f, 0.35f), true);
            float fill = Season.Level / (float)Season.Levels;
            var bar = Part.Make(PrimitiveType.Cube, "Fill", go.transform, new Vector3(-0.8f + 0.8f * fill, 1.22f, -0.06f), new Vector3(1.6f * Mathf.Max(0.01f, fill), 0.08f, 0.02f), new Color(1f, 0.8f, 0.25f), true);
            var label = new GameObject("Text");
            label.transform.SetParent(go.transform, false);
            label.transform.localPosition = new Vector3(0f, 1.75f, -0.06f);
            var board = go.AddComponent<SeasonBoard>();
            board._text = label.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            board._text.font = font;
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            board._text.fontSize = 48;
            board._text.characterSize = 0.03f;
            board._text.anchor = TextAnchor.MiddleCenter;
            board._text.alignment = TextAlignment.Center;
            board._text.color = Color.white;
            board._text.text = Season.Summary();
            Vector3 face = position - lookFrom;
            face.y = 0f;
            if (face.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(face);
            return board;
        }

        void OnEnable() => Current = this;
        void OnDisable() { if (Current == this) Current = null; }
    }
}
