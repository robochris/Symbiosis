using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The Bathroom, behind the Jack, and the Rubber Duck: rings to dodge through, and a bounce to get out from under.</summary>
    public class BathroomTests
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
        public IEnumerator The_bathroom_is_behind_the_jack_and_full_of_its_own_things()
        {
            BiomeDefinition bath = null;
            foreach (var biome in _floors.AllBiomes()) if (biome.id == "bathroom") bath = biome;
            Assert.IsNotNull(bath, "there is no bathroom");
            Assert.IsFalse(Achievements.BiomeUnlocked("bathroom"));
            Achievements.Unlock(AchievementId.BoxedIn);
            Assert.IsTrue(Achievements.BiomeUnlocked("bathroom"));
            Assert.IsTrue(_floors.SetBiome("bathroom"));
            Assert.AreEqual(8, bath.spawnTable.Length);
            Assert.That(ScreenCards.BathroomLines, Does.Contain(ScreenCards.TipFor("bathroom", 4)));
            yield break;
        }

        [UnityTest]
        public IEnumerator A_ring_hurts_on_its_edge_and_a_dodge_goes_through()
        {
            _player.Health.SetInvulnerable(0f);
            _player.Health.ClearInvulnerability();
            int health = _player.Health.Current;
            // Standing still in the ring's way: caught.
            RingWave.Spawn(_player.transform.position + new Vector3(4f, 0f, 0f), 8f, 10f, null, null);
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(health - 1, _player.Health.Current, "the ring's edge went past without touching");
            yield return new WaitForSeconds(1.2f);

            // Dodging as the edge arrives: through it.
            health = _player.Health.Current;
            var ring = RingWave.Spawn(_player.transform.position + new Vector3(4f, 0f, 0f), 8f, 10f, null, null);
            for (float t = 0f; t < 2f && ring != null && ring.Radius < 2.9f; t += Time.deltaTime) yield return null;
            _player.Input.overrideFrame = new InputFrame { move = Vector3.right, dodge = true };
            yield return null;
            _player.Input.overrideFrame = new InputFrame();
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(health, _player.Health.Current, "a dodge did not carry through the ring");
        }

        [UnityTest]
        public IEnumerator The_duck_squeaks_rings_and_bounces_and_the_soap_slides()
        {
            _player.Health.SetInvulnerable(600f);
            Assert.IsTrue(_floors.SetBiome("bathroom"));
            yield return MeetBoss(10);
            var duck = (BathBoss)_floors.Boss;
            Assert.AreEqual("The Rubber Duck", duck.displayName);
            Time.timeScale = 2f;
            yield return WaitUntil(() => duck.Rings >= 1, 10f, "a ring");
            Assert.Greater(RingWave.All.Count, 0);
            yield return WaitUntil(() => duck.Bounces >= 1, 10f, "a bounce");
            duck.Health.Kill();
            yield return null;
            Assert.IsTrue(Achievements.Has(AchievementId.RubberDucky), "never caught (the player could not be hurt), and no Rubber Ducky");

            yield return MeetBoss(5);
            var bar = (BathBoss)_floors.Boss;
            Assert.AreEqual("The Bar of Soap", bar.displayName);
            yield return WaitUntil(() => bar.Slides >= 2, 15f, "slides");
            Assert.AreEqual(0, bar.Rings, "the soap squeaked");
        }
    }
}
