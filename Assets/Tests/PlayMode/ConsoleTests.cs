using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>Inside the Console, behind the Thundercloud, and the Glitch: clicks, a blink, and rows with a gap.</summary>
    public class ConsoleTests
    {
        PlayerController _player;
        FloorManager _floors;

        [UnitySetUp]
        public IEnumerator LoadRun()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            Radio.Forget();
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            _player = PlayerController.Instance;
            _floors = FloorManager.Instance;
            _player.Input.overrideFrame = new InputFrame();
            _player.Health.SetInvulnerable(600f);
        }

        [TearDown]
        public void CleanUp()
        {
            Time.timeScale = 1f;
            Achievements.EverythingUnlocked = false;
            SaveSystem.Wipe();
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        IEnumerator MeetBoss(int floor)
        {
            _floors.BuildFloor(floor);
            yield return null;
            _player.Teleport(_floors.ArenaCentre + new Vector3(0f, 0.05f, -9f));
            yield return WaitUntil(() => _floors.Boss != null, 4f, "the boss");
            yield return WaitUntil(() => Time.timeScale > 0f, 8f, "the title card to pass");
        }

        [UnityTest]
        public IEnumerator The_console_is_behind_the_thundercloud_and_full_of_its_own_things()
        {
            BiomeDefinition console = null;
            foreach (var biome in _floors.AllBiomes()) if (biome.id == "console") console = biome;
            Assert.IsNotNull(console, "there is no console");
            Assert.IsFalse(Achievements.BiomeUnlocked("console"));

            Assert.IsTrue(_floors.SetBiome("clouds"));
            yield return MeetBoss(5);
            _floors.Boss.Health.Kill();
            yield return null;
            Assert.IsFalse(Achievements.BiomeUnlocked("console"), "the Kite opened the console");
            yield return MeetBoss(10);
            _floors.Boss.Health.Kill();
            yield return null;
            Assert.IsTrue(Achievements.BiomeUnlocked("console"));

            Assert.IsTrue(_floors.SetBiome("console"));
            Assert.AreEqual(8, console.spawnTable.Length);
            foreach (var entry in console.spawnTable) Assert.IsNotNull(entry.prefab);
            Assert.That(ScreenCards.ConsoleLines, Does.Contain(ScreenCards.TipFor("console", 4)));
        }

        [UnityTest]
        public IEnumerator The_glitch_clicks_blinks_to_its_mark_and_sweeps_rows_with_a_gap()
        {
            Assert.IsTrue(_floors.SetBiome("console"));
            yield return MeetBoss(10);
            Assert.IsInstanceOf<GlitchBoss>(_floors.Boss);
            var glitch = (GlitchBoss)_floors.Boss;
            Assert.AreEqual("The Glitch", glitch.displayName);
            Time.timeScale = 2f;

            yield return WaitUntil(() => glitch.Clicks >= 3, 15f, "clicks");
            yield return WaitUntil(() => glitch.Current == GlitchBoss.Act.Blink, 15f, "a blink");
            yield return WaitUntil(() => glitch.Blinks >= 1, 5f, "the blink to land");
            yield return WaitUntil(() => glitch.Rows >= 1, 15f, "a row");

            // The gap in the row really is clear: no mark within a mark's width of it.
            Vector3 gap = glitch.LastGap;
            foreach (var hazard in GroundHazard.All)
            {
                Vector3 to = hazard.transform.position - gap;
                to.y = 0f;
                Assert.Greater(to.magnitude, glitch.markRadius * 1.5f, "a mark in the gap");
            }

            glitch.Health.Kill();
            yield return null;
            Assert.IsTrue(Achievements.Has(AchievementId.Patched));
            Assert.IsTrue(Achievements.StyleUnlocked("Pixel"));
        }

        [UnityTest]
        public IEnumerator The_cursor_only_clicks()
        {
            Assert.IsTrue(_floors.SetBiome("console"));
            yield return MeetBoss(5);
            var cursor = (GlitchBoss)_floors.Boss;
            Assert.AreEqual("The Cursor", cursor.displayName);
            Time.timeScale = 3f;
            yield return WaitUntil(() => cursor.Clicks >= 6, 20f, "clicks");
            Assert.AreEqual(0, cursor.Blinks);
            Assert.AreEqual(0, cursor.Rows);
            cursor.Health.Kill();
            yield return null;
            Assert.IsFalse(Achievements.Has(AchievementId.Patched), "the cursor gave out Patched");
        }
    }

    /// <summary>The Daily Experiment: the day's seed, one go a day, and a streak.</summary>
    public class DailyTests
    {
        [SetUp]
        public void Fresh()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            DailyExperiment.TodayOverride = new System.DateTime(2026, 11, 21);
        }

        [TearDown]
        public void CleanUp()
        {
            DailyExperiment.TodayOverride = null;
            RunDirector.Daily = false;
            SaveSystem.Wipe();
        }

        [UnityTest]
        public IEnumerator One_run_a_day_on_the_days_own_floors_and_the_streak_counts_days_in_a_row()
        {
            Assert.AreNotEqual(DailyExperiment.SeedFor(new System.DateTime(2026, 11, 21)), DailyExperiment.SeedFor(new System.DateTime(2026, 11, 22)));
            Assert.AreEqual(DailyExperiment.SeedFor(new System.DateTime(2026, 11, 21)), DailyExperiment.SeedFor(new System.DateTime(2026, 11, 21, 23, 0, 0).Date));

            // Yesterday's was played: today's carries the streak on.
            SaveSystem.Data.meta.dailyDate = "2026-11-20";
            SaveSystem.Data.meta.dailyStreak = 4;
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var daily = Object.FindAnyObjectByType<DailyExperiment>();
            Assert.IsNotNull(daily, "the lab has no Daily Experiment");
            Assert.That(WorldPrompt.For(daily.transform.position), Does.Contain("one try"));
            Assert.IsTrue(daily.Begin());
            Assert.AreEqual(5, SaveSystem.Data.meta.dailyStreak);
            for (float t = 0f; t < 5f && (SceneManager.GetActiveScene().name != "Run" || FloorManager.Instance == null || FloorManager.Instance.Layout == null); t += Time.unscaledDeltaTime) yield return null;
            yield return null;

            var floors = FloorManager.Instance;
            var director = RunDirector.Instance;
            Assert.IsTrue(director.IsDaily);
            Assert.AreEqual(DailyExperiment.SeedFor(DailyExperiment.Today), floors.fixedSeed);
            Assert.AreEqual(floors.fixedSeed + 1, floors.Layout.seed, "floor 1 is not on the day's seed");
            Assert.AreEqual(FloorTwists.ForDay(floors.fixedSeed), floors.dailyTwist, "no twist of the day");
            floors.BuildFloor(4);
            yield return null;
            Assert.AreEqual(floors.dailyTwist, floors.Twist, "floor 4 of a daily does not have the day's twist");
            Assert.AreEqual(floors.fixedSeed, SaveSystem.Data.run.dailySeed, "a resumed daily would lose its seed");
            Assert.AreEqual(4, SaveSystem.Data.meta.dailyFloor);

            // Back in the lab: no second go today, whatever happens.
            SaveSystem.Data.run = new RunSave();
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            daily = Object.FindAnyObjectByType<DailyExperiment>();
            Assert.IsFalse(daily.Begin(), "a second go on the same day");
            Assert.That(WorldPrompt.For(daily.transform.position), Does.Contain("floor 4").And.Contain("tomorrow"));

            // Skip a day and the streak starts again.
            DailyExperiment.TodayOverride = new System.DateTime(2026, 11, 23);
            Assert.IsTrue(daily.Begin());
            Assert.AreEqual(1, SaveSystem.Data.meta.dailyStreak);
            Assert.AreEqual(5, SaveSystem.Data.meta.dailyBestStreak);
            RunDirector.Daily = false;
            yield return new WaitForSecondsRealtime(1.2f);
        }
    }
}
