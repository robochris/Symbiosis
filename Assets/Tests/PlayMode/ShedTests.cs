using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The Garden Shed, behind the Spider, and the Lawnmower: a band of marks with a gap in it.</summary>
    public class ShedTests
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
        public IEnumerator The_shed_is_behind_the_spider_and_full_of_its_own_things()
        {
            BiomeDefinition shed = null;
            foreach (var biome in _floors.AllBiomes()) if (biome.id == "shed") shed = biome;
            Assert.IsNotNull(shed, "there is no shed");
            Assert.IsFalse(Achievements.BiomeUnlocked("shed"));
            Achievements.Unlock(AchievementId.Cobwebs);
            Assert.IsTrue(Achievements.BiomeUnlocked("shed"));
            Assert.IsTrue(_floors.SetBiome("shed"));
            Assert.AreEqual(8, shed.spawnTable.Length);
            Assert.That(ScreenCards.ShedLines, Does.Contain(ScreenCards.TipFor("shed", 4)));
            yield break;
        }

        [UnityTest]
        public IEnumerator The_lawnmower_marks_a_band_with_a_gap_and_the_trimmer_only_charges()
        {
            Assert.IsTrue(_floors.SetBiome("shed"));
            yield return MeetBoss(10);
            var mower = (MowerBoss)_floors.Boss;
            Assert.AreEqual("The Lawnmower", mower.displayName);
            Time.timeScale = 2f;
            yield return WaitUntil(() => mower.Sweeps >= 1, 10f, "a sweep");
            Assert.Greater(GroundHazard.All.Count, 5, "a sweep with no band");
            Assert.IsNotNull(mower.LastGaps);
            Assert.GreaterOrEqual(mower.LastGaps.Length, 1, "a band with no gap in it");
            yield return WaitUntil(() => mower.Charges >= 1, 10f, "a charge");
            mower.Health.Kill();
            yield return null;
            Assert.IsTrue(Achievements.Has(AchievementId.LawnOrder));

            yield return MeetBoss(5);
            var trimmer = (MowerBoss)_floors.Boss;
            Assert.AreEqual("The Hedge Trimmer", trimmer.displayName);
            yield return WaitUntil(() => trimmer.Charges >= 2, 15f, "charges");
            Assert.AreEqual(0, trimmer.Sweeps, "the trimmer swept");
        }
    }
}
