using System;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Daily Experiment, on the lab's north wall: one run a day on the day's own seed, the same floors for anybody
    /// who plays it that day. One attempt; how far it got is chalked up, and so is how many days in a row.
    /// </summary>
    public class DailyExperiment : MonoBehaviour
    {
        public string runScene = "Run";
        public StepPad pad = new StepPad { radius = 1f };
        [Tooltip("Where today's result is chalked up.")]
        public TextMesh board;

        /// <summary>Tests set this to pretend it is another day.</summary>
        public static DateTime? TodayOverride;
        public static DateTime Today => (TodayOverride ?? DateTime.Now).Date;
        public static string Key(DateTime day) => day.ToString("yyyy-MM-dd");

        /// <summary>The day's seed: the same for everybody, different every day.</summary>
        public static int SeedFor(DateTime day)
        {
            unchecked
            {
                uint x = (uint)(day.Year * 10000 + day.Month * 100 + day.Day);
                x ^= x >> 16;
                x *= 0x7feb352d;
                x ^= x >> 15;
                x *= 0x846ca68b;
                x ^= x >> 16;
                return (int)(x & 0x3fffffff) + 1;
            }
        }

        public static bool PlayedToday => SaveSystem.Data.meta.dailyDate == Key(Today);

        void OnEnable()
        {
            WorldPrompt.Add(Prompt, transform);
            Write();
        }

        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || !pad.SteppedOn(transform.position, player.transform.position)) return;
            Begin();
        }

        /// <summary>Starts today's run, if it has not been played and nothing else is on the go. Returns whether it did.</summary>
        public bool Begin()
        {
            MetaSave meta = SaveSystem.Data.meta;
            if (PlayedToday || SaveSystem.Data.run.active)
            {
                Sfx.Play(SfxId.Blocked);
                return false;
            }
            string yesterday = Key(Today.AddDays(-1));
            meta.dailyStreak = meta.dailyDate == yesterday ? meta.dailyStreak + 1 : 1;
            meta.dailyBestStreak = Mathf.Max(meta.dailyBestStreak, meta.dailyStreak);
            meta.dailyDate = Key(Today);
            meta.dailyFloor = 0;
            Jobs.Count(JobKind.Dailies);
            SaveSystem.Save();
            RunDirector.Daily = true;
            Fade.LoadScene(runScene);
            return true;
        }

        void Write()
        {
            if (board == null) return;
            MetaSave meta = SaveSystem.Data.meta;
            string today = PlayedToday ? $"TODAY: FLOOR {meta.dailyFloor}" : "TODAY: NOT YET";
            board.text = $"DAILY EXPERIMENT\n{today}\nSTREAK {meta.dailyStreak}  ·  BEST FLOOR {meta.dailyBestFloor}";
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 2.4f) return null;
            MetaSave meta = SaveSystem.Data.meta;
            if (PlayedToday)
                return $"The Daily Experiment: today's got to floor {meta.dailyFloor}. A new one tomorrow. Streak: {meta.dailyStreak} day{(meta.dailyStreak == 1 ? "" : "s")}.";
            if (SaveSystem.Data.run.active)
                return "The Daily Experiment: you have a run on the go. Finish it, or lose it, first.";
            var twist = FloorTwists.ForDay(SeedFor(Today));
            return $"The Daily Experiment: one run on today's floors, the same for everybody, one try. Today: {FloorTwists.Name(twist)} on every floor it can be. {Interact.Verb} to start.";
        }
    }
}
