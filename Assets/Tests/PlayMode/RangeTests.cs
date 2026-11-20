using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The firing range off the lab: a room of its own, somewhere to mess about.</summary>
    public class RangeTests
    {
        [SetUp]
        public void Fresh()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
        }

        [TearDown]
        public void CleanUp() => SaveSystem.Wipe();

        [UnityTest]
        public IEnumerator The_range_is_through_a_doorway_and_its_targets_fall_and_get_up()
        {
            // A brand-new save: the range is there from the first day.
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var range = Object.FindAnyObjectByType<FiringRange>();
            Assert.IsNotNull(range, "the lab has no firing range");
            var targets = Object.FindObjectsByType<RangeTarget>(FindObjectsSortMode.None);
            Assert.AreEqual(5, targets.Length);
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();

            // The south wall is a wall, except at the doorway.
            Assert.IsTrue(Physics.CheckSphere(new Vector3(-4f, 1f, -8.5f), 0.3f), "no south wall");
            Assert.IsTrue(Physics.CheckSphere(new Vector3(7f, 1f, -8.5f), 0.3f), "no south wall east of the doorway");
            Assert.IsFalse(Physics.CheckSphere(new Vector3(2.6f, 1f, -8.5f), 0.6f), "the doorway to the range is blocked");

            // Walk through it, the way a person would.
            player.Teleport(new Vector3(2.6f, 0.05f, -6.5f));
            player.Input.overrideFrame = new InputFrame { move = Vector3.back };
            for (float t = 0f; t < 3f && player.transform.position.z > -10.5f; t += Time.deltaTime) yield return null;
            player.Input.overrideFrame = new InputFrame();
            Assert.Less(player.transform.position.z, -10f, "could not walk into the range");
            Assert.Greater(player.transform.position.y, -0.2f, "there is no floor in the range");

            // A still target, shot from four metres: down, counted, and up again.
            RangeTarget still = null;
            foreach (var target in targets) if (target.slide == Vector3.zero && (still == null || target.transform.position.x < still.transform.position.x)) still = target;
            player.Teleport(still.transform.position + new Vector3(-4f, 0.05f, 0f));
            yield return null;
            for (float t = 0f; t < 2f && !still.IsDown; t += Time.deltaTime)
            {
                player.weapon.Tick(true, Vector3.right);
                yield return null;
            }
            Assert.IsTrue(still.IsDown, "a shot went through the target");
            Assert.AreEqual(1, range.Hits);
            Assert.IsFalse(still.face.activeSelf);
            yield return new WaitForSeconds(still.standsUpAfter + 0.3f);
            Assert.IsFalse(still.IsDown, "the target stayed down");
            Assert.IsTrue(still.face.activeSelf);

            // Nothing aims itself at a target: it is not an enemy.
            Assert.AreEqual(0, Enemy.Active.Count);
            Assert.IsFalse(player.weapon.HasTarget(Vector3.right));

            // A timed round: only what falls on the clock counts, and the best is kept.
            Assert.That(WorldPrompt.For(range.transform.position), Does.Contain("The range").And.Contain("best is 0"));
            range.roundSeconds = 0.6f;
            player.Teleport(range.transform.position + new Vector3(4f, 0.05f, 0f));
            yield return null;
            player.Teleport(range.transform.position + Vector3.up * 0.05f);
            for (float t = 0f; t < 1f && !range.Running; t += Time.deltaTime) yield return null;
            Assert.IsTrue(range.Running, "the mat did not start the clock");
            targets[0].GetComponent<Health>().Kill();
            targets[1].GetComponent<Health>().Kill();
            yield return null;
            Assert.AreEqual(2, range.RoundHits);
            Assert.That(range.board.text, Does.StartWith("2"));
            yield return new WaitForSeconds(0.8f);
            Assert.IsFalse(range.Running);
            Assert.AreEqual(2, FiringRange.Best);
            Assert.That(range.board.text, Does.Contain("BEST 2"));
            Assert.That(Object.FindAnyObjectByType<Hud>().LastToast, Does.Contain("new best"));
        }

        [UnityTest]
        public IEnumerator The_vent_behind_the_crates_pays_out_once()
        {
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var vent = Object.FindAnyObjectByType<RangeVent>();
            Assert.IsNotNull(vent, "no vent");
            var range = Object.FindAnyObjectByType<FiringRange>();
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();

            // Out of sight from the firing line, and it says nothing until stood beside.
            Vector3 eye = range.transform.position + Vector3.up * 1.2f;
            Assert.IsTrue(Physics.Linecast(eye, vent.transform.position + Vector3.up * 0.4f), "the vent can be seen from the firing line");
            Assert.IsNull(WorldPrompt.For(vent.transform.position + new Vector3(-3f, 0f, -3f)));
            Assert.IsFalse(Physics.CheckSphere(vent.transform.position + Vector3.up * 1f, 0.4f), "the vent is walled in");

            int banked = SaveSystem.Data.meta.samples;
            player.Teleport(vent.transform.position + new Vector3(0f, 0.05f, -3f));
            yield return null;
            player.Teleport(vent.transform.position + Vector3.up * 0.05f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(banked + RangeVent.PettyCash, SaveSystem.Data.meta.samples);
            Assert.IsTrue(RangeVent.Emptied);
            Assert.That(WorldPrompt.For(vent.transform.position), Does.Contain("Empty"));
            Assert.IsFalse(vent.Take(), "the petty cash paid out twice");
            Assert.AreEqual(banked + RangeVent.PettyCash, SaveSystem.Data.meta.samples);
        }
    }
}
