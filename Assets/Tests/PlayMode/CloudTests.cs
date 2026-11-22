using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The Clouds, behind the Toad, and the Thundercloud: lightning that is marked, gusts along a strip, rain that slows.</summary>
    public class CloudTests
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
        public IEnumerator The_clouds_are_behind_the_toad_and_full_of_their_own_things()
        {
            BiomeDefinition clouds = null;
            foreach (var biome in _floors.AllBiomes()) if (biome.id == "clouds") clouds = biome;
            Assert.IsNotNull(clouds, "there are no clouds");
            Assert.IsFalse(Achievements.BiomeUnlocked("clouds"));

            Assert.IsTrue(_floors.SetBiome("forest"));
            yield return MeetBoss(5);
            _floors.Boss.Health.Kill();
            yield return null;
            Assert.IsFalse(Achievements.BiomeUnlocked("clouds"), "the Froglet opened the clouds");
            yield return MeetBoss(10);
            _floors.Boss.Health.Kill();
            yield return null;
            Assert.IsTrue(Achievements.BiomeUnlocked("clouds"));

            Assert.IsTrue(_floors.SetBiome("clouds"));
            Assert.AreEqual(8, clouds.spawnTable.Length);
            foreach (var entry in clouds.spawnTable) Assert.IsNotNull(entry.prefab);
            Assert.IsFalse(clouds.ceiling);
            Assert.That(ScreenCards.CloudLines, Does.Contain(ScreenCards.TipFor("clouds", 4)));
        }

        [UnityTest]
        public IEnumerator The_thundercloud_marks_its_lightning_blows_along_a_strip_and_rains()
        {
            Assert.IsTrue(_floors.SetBiome("clouds"));
            yield return MeetBoss(10);
            Assert.IsInstanceOf<StormBoss>(_floors.Boss);
            var storm = (StormBoss)_floors.Boss;
            Assert.AreEqual("The Thundercloud", storm.displayName);
            Time.timeScale = 2f;

            yield return WaitUntil(() => storm.Current == StormBoss.Act.GustWindup, 20f, "a gust winding up");
            yield return null;
            Assert.IsTrue(storm.ChargeLineShowing, "a gust with no strip on the floor");
            // Stood on the strip, in front of it: carried away along it.
            Vector3 along = storm.transform.forward;
            along.y = 0f;
            along.Normalize();
            _player.Teleport(storm.transform.position + along * 5f + Vector3.up * 0.05f);
            yield return WaitUntil(() => storm.Current == StormBoss.Act.Gust, 4f, "the gust");
            float before = Vector3.Distance(storm.transform.position, _player.transform.position);
            yield return WaitUntil(() => storm.Current != StormBoss.Act.Gust, 4f, "the gust to drop");
            Assert.GreaterOrEqual(storm.Blown, 1, "the gust missed a player standing on its strip");
            Assert.Greater(Vector3.Distance(storm.transform.position, _player.transform.position), before + 2f, "the gust did not carry the player");

            yield return WaitUntil(() => storm.Showers > 0, 20f, "rain");
            yield return WaitUntil(() => storm.Strikes > 0, 20f, "lightning");
            Assert.Greater(GroundHazard.All.Count, 0, "lightning with no marks on the floor");

            // Never struck: Silver Lining.
            storm.Health.Kill();
            yield return null;
            Assert.IsTrue(Achievements.Has(AchievementId.SilverLining));
            Assert.IsTrue(Achievements.StyleUnlocked("Storm"));
        }

        [UnityTest]
        public IEnumerator The_kite_gusts_rains_and_throws_a_little_lightning()
        {
            Assert.IsTrue(_floors.SetBiome("clouds"));
            yield return MeetBoss(5);
            var kite = (StormBoss)_floors.Boss;
            Assert.AreEqual("The Kite", kite.displayName);
            Time.timeScale = 3f;
            yield return WaitUntil(() => kite.Gusts >= 1 && kite.Showers >= 1 && kite.Strikes >= 1, 30f, "gusts, rain and lightning");
            Assert.LessOrEqual(kite.Strikes % 3, 3, "a volley too big for a kite");
            kite.Health.Kill();
            yield return null;
            Assert.IsFalse(Achievements.Has(AchievementId.SilverLining), "the kite gave out Silver Lining");
        }
    }
}
