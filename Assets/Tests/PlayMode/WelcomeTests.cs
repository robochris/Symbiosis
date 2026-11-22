using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>For new players: the daily parcel, beginner's luck, the floating use markers, and what to go back for.</summary>
    public class WelcomeTests
    {
        [SetUp]
        public void Fresh()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
        }

        [TearDown]
        public void CleanUp()
        {
            DailyParcel.Off = true;
            DailyParcel.TodayOverride = null;
            RunDirector.BeginnersLuckOff = true;
            SaveSystem.Wipe();
        }

        [UnityTest]
        public IEnumerator A_parcel_waits_in_the_lab_each_day_and_grows_with_the_streak()
        {
            DailyParcel.Off = false;
            DailyParcel.TodayOverride = new DateTime(2026, 3, 1);
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var parcel = DailyParcel.Current;
            Assert.IsNotNull(parcel, "no parcel in the lab");
            int before = SaveSystem.Data.meta.samples;
            Assert.AreEqual(DailyParcel.BaseSamples, parcel.Open());
            Assert.AreEqual(before + DailyParcel.BaseSamples, SaveSystem.Data.meta.samples);
            Assert.IsFalse(DailyParcel.Waiting, "a second parcel the same day");
            Assert.IsNull(DailyParcel.Place(Vector3.zero));

            // The next day: a bigger one. A day missed: back to the start.
            DailyParcel.TodayOverride = new DateTime(2026, 3, 2);
            Assert.IsTrue(DailyParcel.Waiting);
            Assert.AreEqual(2, DailyParcel.StreakIfOpened);
            Assert.AreEqual(DailyParcel.BaseSamples + DailyParcel.PerDay, DailyParcel.SamplesFor(2));
            DailyParcel.TodayOverride = new DateTime(2026, 3, 4);
            Assert.AreEqual(1, DailyParcel.StreakIfOpened);

            // A whole week: lucky coins as well.
            SaveSystem.Data.meta.parcelStreak = DailyParcel.MaxStreak - 1;
            SaveSystem.Data.meta.parcelDate = "2026-03-03";
            int coins = SaveSystem.Data.meta.luckyCoins;
            var weekly = DailyParcel.Place(Vector3.zero);
            yield return null;
            weekly.Open();
            Assert.AreEqual(coins + DailyParcel.LuckyCoins, SaveSystem.Data.meta.luckyCoins);
        }

        [UnityTest]
        public IEnumerator The_first_runs_start_with_an_item()
        {
            RunDirector.BeginnersLuckOff = false;
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            Assert.AreEqual(1, player.Items.Collected.Count, "no beginner's luck on the first run");
            int? pinned = Progress.LevelOverride;
            Progress.LevelOverride = 1;
            string goal = RunDirector.NextGoal();
            Progress.LevelOverride = pinned;
            StringAssert.StartsWith("Next: level", goal);
        }

        [UnityTest]
        public IEnumerator Usable_things_have_a_floating_marker_instead_of_a_mat()
        {
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            yield return null;
            Assert.Greater(UseMarker.All.Count, 5, "few markers in the lab");
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.gameObject.name == "Mat") Assert.IsFalse(renderer.enabled, "a mat still showing under " + renderer.transform.parent.name);
            }

            // Walk up to one and it shows the key.
            var marker = UseMarker.All[0];
            var player = PlayerController.Instance;
            // Only standing there, not using it (the tests' step-on-it shortcut would start whatever it is).
            Interact.AutoUse = false;
            player.Teleport(marker.transform.position + Vector3.up * 0.05f);
            yield return null;
            yield return null;
            bool inReach = marker.InReach;
            Interact.AutoUse = true;
            Assert.IsTrue(inReach, "standing at it and it is not in reach");
        }
    }
}
