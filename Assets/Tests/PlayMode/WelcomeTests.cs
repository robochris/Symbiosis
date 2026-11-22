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
            [UnityTest]
        public IEnumerator Hats_are_earned_by_playing_and_tried_on_at_the_stand()
        {
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var stand = HatStand.Current;
            Assert.IsNotNull(stand, "no hat stand in the lab");
            var player = PlayerController.Instance;
            Assert.IsNotNull(player.GetComponent<PlayerHat>(), "the player has no head for hats");

            // A new player has the beanie, and nothing else yet.
            Assert.AreEqual(2, Hats.EarnedCount);
            Assert.AreEqual("Beanie", Hats.WearNext().name);
            Assert.AreEqual("No hat", Hats.WearNext().name);
            Assert.AreEqual("Party Hat", Hats.NextToEarn().name);

            // Three runs started: the party hat, announced once.
            SaveSystem.Data.meta.runsStarted = 3;
            var fresh = Hats.TakeNew();
            Assert.AreEqual(1, fresh.Count);
            Assert.AreEqual("Party Hat", fresh[0].name);
            Assert.AreEqual(0, Hats.TakeNew().Count, "the same hat announced twice");
            Assert.AreEqual("Beanie", Hats.WearNext().name);
            Assert.AreEqual("Party Hat", Hats.WearNext().name);
            Assert.AreEqual("Party Hat", Hats.All[Hats.Worn].name);
            yield return null;
            Assert.IsNotNull(player.visual.Find("Hat/Pom"), "the party hat is not on the head");
        }

        [UnityTest]
        public IEnumerator First_time_tips_are_said_once_each()
        {
            FirstTips.Off = false;
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            Assert.IsFalse(FirstTips.Seen(FirstTips.Tip.Coins));
            RunState.AddCoins(3);
            for (float t = 0f; t < 10f && !FirstTips.Seen(FirstTips.Tip.Coins); t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(FirstTips.Seen(FirstTips.Tip.Coins), "no tip about coins");
            foreach (FirstTips.Tip tip in System.Enum.GetValues(typeof(FirstTips.Tip))) Assert.IsNotEmpty(FirstTips.Line(tip));
            FirstTips.Off = true;
        }

        [UnityTest]
        public IEnumerator Quick_kills_chain_into_a_combo_that_pays_coins()
        {
            KillCombo.Off = false;
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            KillCombo.StartRun();
            int pickups = Pickup.All.Count;
            for (int i = 0; i < KillCombo.Milestone; i++) KillCombo.Register();
            Assert.AreEqual(KillCombo.Milestone, KillCombo.Chain);
            Assert.AreEqual(KillCombo.Milestone, KillCombo.RunBest);
            Assert.Greater(Pickup.All.Count, pickups, "no coins for a chain of five");
            Assert.GreaterOrEqual(SaveSystem.Data.meta.bestCombo, KillCombo.Milestone);

            // A pause longer than the window breaks it.
            yield return new WaitForSeconds(KillCombo.Window + 0.2f);
            KillCombo.Register();
            Assert.AreEqual(1, KillCombo.Chain);
            KillCombo.Off = true;
        }
    }
}
