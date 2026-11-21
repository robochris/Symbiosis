using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The Toy Box, behind the Goldfish, and the Jack: circles to get out of, and a box in one of them.</summary>
    public class ToyBoxTests
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
        public IEnumerator The_toy_box_is_behind_the_goldfish_and_full_of_its_own_things()
        {
            BiomeDefinition box = null;
            foreach (var biome in _floors.AllBiomes()) if (biome.id == "toybox") box = biome;
            Assert.IsNotNull(box, "there is no toy box");
            Assert.IsFalse(Achievements.BiomeUnlocked("toybox"));
            Achievements.Unlock(AchievementId.GoneFishing);
            Assert.IsTrue(Achievements.BiomeUnlocked("toybox"));
            Assert.IsTrue(_floors.SetBiome("toybox"));
            Assert.AreEqual(8, box.spawnTable.Length);
            Assert.That(ScreenCards.ToyBoxLines, Does.Contain(ScreenCards.TipFor("toybox", 4)));
            yield break;
        }

        [UnityTest]
        public IEnumerator The_jack_hides_marks_circles_and_springs_up_out_of_one()
        {
            Assert.IsTrue(_floors.SetBiome("toybox"));
            yield return MeetBoss(10);
            Assert.IsInstanceOf<ToyBoss>(_floors.Boss);
            var jack = (ToyBoss)_floors.Boss;
            Assert.AreEqual("The Jack-in-the-Box", jack.displayName);
            Time.timeScale = 2f;
            yield return WaitUntil(() => jack.Throws >= 1, 10f, "jacks thrown");
            yield return WaitUntil(() => jack.Hidden, 10f, "it to hide");
            Assert.IsFalse(jack.Targetable, "it can be aimed at in its box");
            Assert.IsFalse(jack.Health.TakeDamage(5, Vector3.forward), "it can be hurt in its box");
            yield return WaitUntil(() => jack.Current == ToyBoss.Act.Springing, 5f, "the circles");
            Assert.GreaterOrEqual(GroundHazard.All.Count, jack.circlesPerSpring[0], "not enough circles");
            yield return WaitUntil(() => jack.Springs >= 1, 5f, "the spring");
            Assert.IsTrue(jack.Targetable);
            jack.Health.Kill();
            yield return null;
            Assert.IsTrue(Achievements.Has(AchievementId.BoxedIn));
        }

        [UnityTest]
        public IEnumerator The_mouse_charges_three_times_then_runs_down()
        {
            Assert.IsTrue(_floors.SetBiome("toybox"));
            yield return MeetBoss(5);
            var mouse = (ToyBoss)_floors.Boss;
            Assert.AreEqual("The Wind-up Mouse", mouse.displayName);
            Time.timeScale = 2f;
            yield return WaitUntil(() => mouse.WindDowns >= 1, 20f, "it to run down");
            Assert.AreEqual(mouse.chargesInARow, mouse.Charges, "it ran down after the wrong number of charges");
            Assert.AreEqual(0, mouse.Springs, "the mouse hid in a box");
            mouse.Health.Kill();
            yield return null;
            Assert.IsFalse(Achievements.Has(AchievementId.BoxedIn), "the mouse gave out Boxed In");
        }
    }
}
