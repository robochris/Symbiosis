using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The Aquarium, behind the Glitch, and the Goldfish: a dash along a marked line, and slow bubbles.</summary>
    public class AquariumTests
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
        public IEnumerator The_aquarium_is_behind_the_glitch_and_full_of_its_own_things()
        {
            BiomeDefinition tank = null;
            foreach (var biome in _floors.AllBiomes()) if (biome.id == "aquarium") tank = biome;
            Assert.IsNotNull(tank, "there is no aquarium");
            Assert.IsFalse(Achievements.BiomeUnlocked("aquarium"));
            Achievements.Unlock(AchievementId.Patched);
            Assert.IsTrue(Achievements.BiomeUnlocked("aquarium"));
            Assert.IsTrue(_floors.SetBiome("aquarium"));
            Assert.AreEqual(8, tank.spawnTable.Length);
            foreach (var entry in tank.spawnTable) Assert.IsNotNull(entry.prefab);
            Assert.That(ScreenCards.AquariumLines, Does.Contain(ScreenCards.TipFor("aquarium", 4)));
            yield break;
        }

        [UnityTest]
        public IEnumerator The_goldfish_paints_its_dash_and_blows_bubbles()
        {
            Assert.IsTrue(_floors.SetBiome("aquarium"));
            yield return MeetBoss(10);
            Assert.IsInstanceOf<FishBoss>(_floors.Boss);
            var fish = (FishBoss)_floors.Boss;
            Assert.AreEqual("The Goldfish", fish.displayName);
            Time.timeScale = 2f;
            yield return WaitUntil(() => fish.Current == FishBoss.Act.DashWindup, 10f, "a dash winding up");
            yield return null;
            Assert.IsTrue(fish.ChargeLineShowing, "a dash with no line on the floor");
            yield return WaitUntil(() => fish.Dashes >= 1, 5f, "the dash");
            yield return WaitUntil(() => fish.Fans >= 1, 10f, "bubbles");
            fish.Health.Kill();
            yield return null;
            if (fish.DashHits == 0) Assert.IsTrue(Achievements.Has(AchievementId.FishedOut));
        }

        [UnityTest]
        public IEnumerator The_snail_cannot_be_hurt_in_its_shell_and_never_dashes()
        {
            Assert.IsTrue(_floors.SetBiome("aquarium"));
            yield return MeetBoss(5);
            var snail = (FishBoss)_floors.Boss;
            Assert.AreEqual("The Snail", snail.displayName);
            Time.timeScale = 3f;
            yield return WaitUntil(() => snail.Shelled, 20f, "the shell");
            int before = snail.Health.Current;
            Assert.IsFalse(snail.Health.TakeDamage(5, Vector3.forward), "the shell let a hit through");
            Assert.AreEqual(before, snail.Health.Current);
            Assert.AreEqual(0, snail.Dashes, "the snail dashed");
            snail.Health.Kill();
            yield return null;
            Assert.IsFalse(Achievements.Has(AchievementId.FishedOut), "the snail gave out Fished Out");
        }
    }
}
