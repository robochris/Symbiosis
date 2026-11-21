using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The Gauntlet: shut until four jars, then boss after boss with health between, and the best kept.</summary>
    public class GauntletTests
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
            RunDirector.Gauntlet = false;
            Time.timeScale = 1f;
            SaveSystem.Wipe();
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        [UnityTest]
        public IEnumerator Boss_after_boss_with_health_between_and_the_best_kept()
        {
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var station = Object.FindAnyObjectByType<GauntletStation>();
            Assert.IsNotNull(station, "no Gauntlet in the lab");
            Assert.IsFalse(station.Begin(), "the Gauntlet opened with no jars");
            Assert.That(WorldPrompt.For(station.transform.position), Does.Contain("0 so far"));

            foreach (string key in new[] { "AntQueen", "TrickCandle", "RobotVacuum", "IceMaker" }) SaveSystem.Data.meta.specimens.Add(key);
            Assert.IsTrue(station.Begin());
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == "Run" && FloorManager.Instance != null && FloorManager.Instance.Layout != null, 5f, "the run");
            yield return null;

            var floors = FloorManager.Instance;
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            Assert.IsTrue(floors.InGauntlet);
            Assert.AreEqual(4, floors.GauntletLength);
            Assert.AreEqual("ants", floors.biome.id);
            foreach (var node in floors.Layout.ordered) if (node.type == RoomType.Normal) Assert.IsTrue(node.cleared, "a fight before the boss");

            // The first boss down: the way on, and home.
            player.Teleport(floors.ArenaCentre + new Vector3(0f, 0.05f, -9f));
            yield return WaitUntil(() => floors.Boss != null, 5f, "the first boss");
            yield return WaitUntil(() => Time.timeScale > 0f, 8f, "the title card");
            floors.Boss.Health.Kill();
            yield return WaitUntil(() => floors.ExitOpen && floors.ExtractPortal != null, 8f, "the portals");
            Assert.AreEqual(1, floors.GauntletBeaten);
            FloorManager.BiomePortal next = null;
            foreach (var portal in floors.Portals) if (portal.gauntletNext) next = portal;
            Assert.IsNotNull(next, "no way on to the next boss");

            player.Health.Restore(1);
            player.Teleport(next.gate.transform.position + new Vector3(0f, 0.05f, -4f));
            yield return null;
            player.Teleport(next.gate.transform.position + Vector3.up * 0.05f);
            yield return WaitUntil(() => floors.GauntletIndex == 1, 3f, "the next boss's floor");
            Assert.AreEqual("cake", floors.biome.id);
            Assert.AreEqual(2, player.Health.Current, "no health back between bosses");

            // Home from here: the best is kept.
            Object.FindAnyObjectByType<RunDirector>().Extract();
            Assert.AreEqual(1, SaveSystem.Data.meta.gauntletBest);
            Assert.AreEqual("GAUNTLET: HOME", RunDirector.LastSummaryHeading);
            Assert.IsFalse(SaveSystem.Data.run.active, "a Gauntlet run was saved");
            yield return new WaitForSecondsRealtime(1.2f);
        }
    }
}
