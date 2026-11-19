using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The small things that make it feel finished: fades, a boss dying slowly, and the words over the portals.</summary>
    public class PolishTests
    {
        [SetUp]
        public void Fresh()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            Radio.Forget();
            Radio.ChanceOverride = 0f;
        }

        [TearDown]
        public void CleanUp()
        {
            SaveSystem.Wipe();
            Radio.ChanceOverride = null;
            Fade.EnabledOverride = null;
            GameLoop.SlowMotionOverride = null;
            Time.timeScale = 1f;
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        [UnityTest]
        public IEnumerator Going_through_the_portal_fades_out_and_back_in()
        {
            SaveSystem.Data.meta.runsStarted = 2;
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            Fade.EnabledOverride = true;

            var portal = Object.FindAnyObjectByType<LabPortal>();
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            yield return null;
            player.Teleport(portal.transform.position + Vector3.up * 0.05f);
            yield return WaitUntil(() => Fade.Leaving, 1f, "the fade to start");
            Assert.AreEqual("Lab", SceneManager.GetActiveScene().name, "the scene changed before the screen went dark");
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == "Run", 3f, "the run to load");
            Assert.Greater(Fade.Amount, 0.9f, "the new scene was shown before the fade back in");
            yield return WaitUntil(() => Fade.Amount <= 0f && !Fade.Leaving, 3f, "the fade back in");

            // A new floor dips to black too.
            FloorManager.Instance.BuildFloor(2);
            Assert.Greater(Fade.Amount, 0.9f, "no dip to black for a new floor");
            yield return WaitUntil(() => Fade.Amount <= 0f, 3f, "the floor to fade in");
        }

        [UnityTest]
        public IEnumerator A_boss_dies_in_slow_motion_and_time_comes_back()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            var spawner = Object.FindAnyObjectByType<EnemySpawner>();
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            spawner.maxAlive = 0;
            foreach (var enemy in new List<Enemy>(Enemy.Active)) Object.Destroy(enemy.gameObject);
            yield return null;

            GameLoop.SlowMotionOverride = true;
            var boss = spawner.SpawnAt(spawner.biome.miniBoss, new Vector3(0f, 0f, 10f), null);
            yield return null;
            boss.Health.Kill();
            yield return null;
            Assert.IsTrue(GameLoop.InSlowMotion);
            Assert.Less(Time.timeScale, 0.5f);
            yield return WaitUntil(() => !GameLoop.InSlowMotion, 3f, "time to come back");
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
        }

        [UnityTest]
        public IEnumerator Portals_say_where_they_go_and_what_it_is_worth()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            var floors = FloorManager.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);

            floors.BuildFloor(10);
            yield return null;
            player.Teleport(floors.ArenaCentre + Vector3.up * 0.05f);
            yield return WaitUntil(() => floors.Boss != null, 5f, "the boss");
            yield return WaitUntil(() => Time.timeScale > 0f, 8f, "the title card to pass");
            RunState.AddSamples(7);
            floors.Boss.Health.Kill();
            yield return WaitUntil(() => floors.ExtractPortal != null, 8f, "the portals");
            yield return new WaitForSecondsRealtime(0.4f);

            var home = floors.ExtractPortal.GetComponentInChildren<PortalTag>();
            Assert.IsNotNull(home, "nothing over the way home");
            Assert.That(home.Shown, Does.StartWith("GO HOME").And.Contain(RunState.Samples + " samples"));
            Assert.GreaterOrEqual(floors.Portals.Count, 1);
            foreach (var portal in floors.Portals)
            {
                var tag = portal.gate.GetComponentInChildren<PortalTag>();
                Assert.IsNotNull(tag, "nothing over the portal to " + portal.leadsTo.displayName);
                Assert.That(tag.Shown, Does.Contain(portal.leadsTo == floors.biome ? "DEEPER" : portal.leadsTo.displayName.ToUpperInvariant()).And.Contain("depth 2").And.Contain("×1.5"));
            }
        }
    }
}
