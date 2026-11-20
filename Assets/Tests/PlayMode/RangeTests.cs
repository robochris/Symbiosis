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
            var everything = Object.FindObjectsByType<RangeTarget>(FindObjectsSortMode.None);
            var list = new System.Collections.Generic.List<RangeTarget>();
            RangeTarget radio = null;
            RangeTarget gold = null;
            foreach (var each in everything) { if (each.isRadio) radio = each; else if (each.appearsEvery > 0f) gold = each; else list.Add(each); }
            var targets = list.ToArray();
            Assert.AreEqual(5, targets.Length);
            Assert.IsNotNull(radio, "Finch's radio set is missing");
            Assert.IsNotNull(gold, "the range has no gold target");
            yield return null;
            Assert.IsTrue(gold.IsHidden, "the gold target is up from the start");
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();

            // The south wall is a wall, except at the doorway.
            Assert.IsTrue(Physics.CheckSphere(new Vector3(-4f, 1f, -8.5f), 0.3f), "no south wall");
            Assert.IsTrue(Physics.CheckSphere(new Vector3(7f, 1f, -8.5f), 0.3f), "no south wall east of the doorway");
            Assert.IsFalse(Physics.CheckSphere(new Vector3(2.6f, 1f, -8.5f), 0.6f), "the doorway to the range is blocked");

            // There is floor all the way through it: under the doorway itself, and a step either side.
            foreach (float z in new[] { -7.6f, -8.1f, -8.5f, -8.9f, -9.4f })
            {
                foreach (float x in new[] { 1.9f, 2.6f, 3.3f })
                {
                    Assert.IsTrue(Physics.Raycast(new Vector3(x, 1f, z), Vector3.down, out var ground, 1.5f), $"no floor in the doorway at {x}, {z}");
                    Assert.AreEqual(0f, ground.point.y, 0.02f, $"a step or a hole in the doorway at {x}, {z}");
                }
            }

            // Stood still in the doorway, the player stays there.
            player.Teleport(new Vector3(2.6f, 0.05f, -8.5f));
            yield return new WaitForSeconds(0.6f);
            Assert.Greater(player.transform.position.y, -0.1f, "the player fell through the doorway");

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

            // Every target down at the same moment.
            yield return new WaitForSeconds(1.6f);
            Assert.IsFalse(Achievements.Has(AchievementId.CleanSweep));
            foreach (var target in targets) target.GetComponent<Health>().Kill();
            yield return null;
            Assert.IsTrue(Achievements.Has(AchievementId.CleanSweep));

            // The gold one: out of sight most of the time, up for a few seconds, and worth three.
            gold.appearsEvery = 0.5f;
            for (float t = 0f; t < 12f && gold.IsHidden; t += Time.deltaTime) yield return null;
            Assert.IsFalse(gold.IsHidden, "the gold target never came up");
            Assert.IsTrue(gold.face.activeSelf);
            int beforeGold = range.Hits;
            gold.GetComponent<Health>().Kill();
            yield return null;
            Assert.AreEqual(beforeGold + 3, range.Hits, "the gold target was not worth three");
            yield return new WaitForSeconds(gold.standsUpAfter + 0.2f);
            Assert.IsTrue(gold.IsHidden, "the gold target stood straight back up");

            // The radio set is not a target: shooting it scores nothing, and Finch has something to say.
            int hits = range.Hits;
            Radio.Forget();
            radio.GetComponent<Health>().Kill();
            yield return null;
            Assert.IsTrue(radio.IsDown);
            Assert.AreEqual(hits, range.Hits, "shooting the radio scored on the range");
            Assert.AreEqual(RadioMoment.ShotRadio, Radio.Instance.LastMoment, "Finch let that pass");
        }

        [UnityTest]
        public IEnumerator The_fortune_machine_reads_one_a_visit_and_a_lucky_one_pays()
        {
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var machine = Object.FindAnyObjectByType<FortuneMachine>();
            Assert.IsNotNull(machine, "no fortune machine");
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            Assert.That(WorldPrompt.For(machine.transform.position), Does.Contain("fortune machine"));
            foreach (string fortune in FortuneMachine.Fortunes) Assert.LessOrEqual(fortune.Length, 110, "too long to read: " + fortune);

            // Stepping on the mat reads one; a second step this visit reads nothing.
            machine.rigged = true;
            player.Teleport(machine.transform.position + new Vector3(3f, 0.05f, 0f));
            yield return null;
            player.Teleport(machine.transform.position + Vector3.up * 0.05f);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(machine.Used);
            Assert.AreEqual(FortuneMachine.Fortunes[0], machine.Last);
            Assert.That(Object.FindAnyObjectByType<Hud>().LastToast, Does.Contain(machine.Last).And.Contain("lucky"));
            Assert.AreEqual(FortuneMachine.LuckyCoins, SaveSystem.Data.meta.luckyCoins);
            Assert.IsFalse(machine.Read());
            Assert.That(WorldPrompt.For(machine.transform.position), Does.Contain("one a visit"));

            // The lucky coins are in hand at the start of the next run, once.
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            Assert.AreEqual(FortuneMachine.LuckyCoins, RunState.Coins, "the lucky fortune paid nothing");
            Assert.AreEqual(0, SaveSystem.Data.meta.luckyCoins);

            // Next visit: a different fortune.
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            machine = Object.FindAnyObjectByType<FortuneMachine>();
            machine.rigged = false;
            Assert.IsTrue(machine.Read());
            Assert.AreEqual(FortuneMachine.Fortunes[1], machine.Last);
            Assert.AreEqual(0, SaveSystem.Data.meta.luckyCoins, "an ordinary fortune paid");
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
