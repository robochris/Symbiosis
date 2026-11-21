using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The Attic, behind the Duck, and the Spider: web, a drop from out of sight, a bite.</summary>
    public class AtticTests
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
        public IEnumerator The_attic_is_behind_the_duck_and_full_of_its_own_things()
        {
            BiomeDefinition attic = null;
            foreach (var biome in _floors.AllBiomes()) if (biome.id == "attic") attic = biome;
            Assert.IsNotNull(attic, "there is no attic");
            Assert.IsFalse(Achievements.BiomeUnlocked("attic"));
            Achievements.Unlock(AchievementId.BathTime);
            Assert.IsTrue(Achievements.BiomeUnlocked("attic"));
            Assert.IsTrue(_floors.SetBiome("attic"));
            Assert.AreEqual(8, attic.spawnTable.Length);
            Assert.That(ScreenCards.AtticLines, Does.Contain(ScreenCards.TipFor("attic", 4)));
            yield break;
        }

        [UnityTest]
        public IEnumerator The_spider_lays_web_climbs_out_of_reach_and_drops()
        {
            Assert.IsTrue(_floors.SetBiome("attic"));
            yield return MeetBoss(10);
            Assert.IsInstanceOf<SpiderBoss>(_floors.Boss);
            var spider = (SpiderBoss)_floors.Boss;
            Assert.AreEqual("The Spider", spider.displayName);
            Time.timeScale = 2f;
            yield return WaitUntil(() => spider.Webs >= 1, 10f, "web");
            Assert.Greater(GroundHazard.All.Count, 3, "web with no strips on the floor");
            yield return WaitUntil(() => spider.Up, 12f, "the climb");
            Assert.IsFalse(spider.Targetable, "it can be aimed at out of sight");
            Assert.IsFalse(spider.Health.TakeDamage(5, Vector3.forward), "it can be hurt out of sight");
            yield return WaitUntil(() => spider.Drops >= 1, 6f, "the drop");
            Assert.IsTrue(spider.Targetable);
            spider.Health.Kill();
            yield return null;
            Assert.IsTrue(Achievements.Has(AchievementId.Cobwebs));
        }

        [UnityTest]
        public IEnumerator The_bat_swoops_along_its_line()
        {
            Assert.IsTrue(_floors.SetBiome("attic"));
            yield return MeetBoss(5);
            var bat = (FishBoss)_floors.Boss;
            Assert.AreEqual("The Bat", bat.displayName);
            Time.timeScale = 2f;
            yield return WaitUntil(() => bat.Dashes >= 2, 15f, "swoops");
        }
    }
}
