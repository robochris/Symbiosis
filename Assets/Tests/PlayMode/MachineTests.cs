using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The main road: four parts from four bosses, the machine they switch on, and the cycle that beating it starts.</summary>
    public class MachineTests
    {
        [SetUp]
        public void Fresh()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            Radio.Forget();
            Radio.ChanceOverride = 0f;
            RunDirector.Descent = false;
            RunDirector.ToMachine = false;
            RunDirector.CycleCard = RunDirector.CycleRadio = false;
            RunDirector.LastRunMessage = null;
        }

        [TearDown]
        public void CleanUp()
        {
            SaveSystem.Wipe();
            Radio.Forget();
            Radio.ChanceOverride = null;
            RunDirector.Descent = false;
            RunDirector.ToMachine = false;
            RunDirector.CycleCard = RunDirector.CycleRadio = false;
            RunDirector.LastRunMessage = null;
            Time.timeScale = 1f;
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        [UnityTest]
        public IEnumerator A_world_boss_gives_up_its_part_once_a_cycle()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var floors = FloorManager.Instance;
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            int found = 0;
            System.Action<MachinePart> count = part => found++;
            GameLoop.PieceFound += count;

            for (int round = 0; round < 3; round++)
            {
                // The third time round is a cycle later: the part is out there again.
                if (round == 2)
                {
                    MachineParts.Restart();
                    Assert.AreEqual(0, MachineParts.Count);
                    Assert.AreEqual(1, MachineParts.Cycle);
                }
                floors.BuildFloor(10);
                yield return null;
                player.Teleport(floors.ArenaCentre + new Vector3(0f, 0.05f, -8f));
                yield return WaitUntil(() => floors.Boss != null, 5f, "the Queen");
                yield return WaitUntil(() => Time.timeScale > 0f, 8f, "the title card to pass");
                floors.Boss.Health.Kill();
                yield return null;
                Assert.AreEqual(1, MachineParts.Count);
                Assert.AreEqual(round < 2 ? 1 : 2, found, "the part was handed over the wrong number of times");
                if (round == 0)
                {
                    Assert.AreEqual(GameLoop.PieceHeading, ScreenCards.Instance.LastHeading);
                    Assert.That(ScreenCards.Instance.LastLine, Does.StartWith("The fuse. 1 of 4."));
                }
                yield return WaitUntil(() => floors.ExtractPortal != null, 5f, "the portals");
            }
            GameLoop.PieceFound -= count;

            // The mini-boss half way down has no part.
            floors.BuildFloor(5);
            yield return null;
            player.Teleport(floors.ArenaCentre + new Vector3(0f, 0.05f, -8f));
            yield return WaitUntil(() => floors.Boss != null, 5f, "the Brood Mother");
            yield return WaitUntil(() => Time.timeScale > 0f, 8f, "the title card to pass");
            floors.Boss.Health.Kill();
            yield return null;
            Assert.AreEqual(1, MachineParts.Count);
        }

        [Test]
        public void Each_restart_tells_one_more_piece_of_the_story()
        {
            var told = new System.Collections.Generic.HashSet<string>();
            for (int cycle = 1; cycle <= 5; cycle++)
            {
                string key = RadioLines.RestartKey(cycle);
                Assert.IsNotNull(key);
                string line = RadioLines.Pick(RadioMoment.Cycle, key, new System.Random(1));
                Assert.IsTrue(told.Add(line), "cycle " + cycle + " repeated an earlier one: " + line);
                // Asked again, that part has been told: the ordinary remarks take over.
                Assert.AreNotEqual(line, RadioLines.Pick(RadioMoment.Cycle, key, new System.Random(1)));
            }
            Assert.IsNull(RadioLines.RestartKey(6));
            Assert.IsNull(RadioLines.RestartKey(0));
        }

        [UnityTest]
        public IEnumerator The_noticeboard_pins_three_jobs_pays_for_them_and_deals_the_next()
        {
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var board = Object.FindAnyObjectByType<JobBoard>();
            Assert.IsNotNull(board, "the lab has no noticeboard");
            Assert.IsNotNull(Object.FindAnyObjectByType<JobTracker>(), "nobody is counting in the lab");
            Assert.AreEqual(3, Jobs.Current.Count);
            string read = WorldPrompt.For(board.transform.position);
            Assert.That(read, Does.Contain("noticeboard").And.Contain("0 of 40").And.Contain("crowned").And.Contain("5 floors"));

            // Every job has a different number, and the pile has more than the board holds.
            var ids = new System.Collections.Generic.HashSet<int>();
            foreach (var job in Jobs.All) Assert.IsTrue(ids.Add(job.id), "two jobs share a number");
            Assert.Greater(Jobs.All.Length, Jobs.Pinned);

            // In a run: kills count, and the fortieth pays and pins up the next job.
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            Assert.IsNotNull(Object.FindAnyObjectByType<JobTracker>(), "nobody is counting in the run");
            int banked = SaveSystem.Data.meta.samples;
            Jobs.Count(JobKind.Kills, 39);
            Assert.AreEqual(39, Jobs.Progress(0));
            Assert.AreEqual(banked, SaveSystem.Data.meta.samples);
            var floors = FloorManager.Instance;
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            RoomNode fight = null;
            foreach (var node in floors.Layout.ordered) if (node.type == RoomType.Normal && fight == null) fight = node;
            player.Teleport(floors.RoomAt(fight.cell).Centre + new Vector3(RoomShape.TileSize, 0.05f, 0f));
            yield return WaitUntil(() => floors.IsLocked && floors.LiveEnemies.Count > 0, 3f, "a fight");
            floors.LiveEnemies[0].Health.Kill();
            yield return null;
            Assert.AreEqual(banked + 25, SaveSystem.Data.meta.samples, "the job did not pay");
            Assert.AreEqual(1, SaveSystem.Data.meta.jobsDone);
            Assert.That(Object.FindAnyObjectByType<Hud>().LastToast, Does.Contain("Job done"));
            Assert.AreEqual(3, Jobs.Current.Count);
            Assert.AreEqual(0, Jobs.Progress(0));
            Assert.AreNotEqual(0, Jobs.Current[0], "the finished job was pinned straight back up");
            Assert.AreEqual(3, new System.Collections.Generic.HashSet<int>(Jobs.Current).Count, "the same job is pinned up twice");

            // It goes round: thirty jobs later there are still three, all different.
            for (int i = 0; i < 30; i++)
            {
                Job first = Jobs.Get(Jobs.Current[0]);
                Jobs.Count(first.kind, first.needed);
                Assert.AreEqual(3, new System.Collections.Generic.HashSet<int>(Jobs.Current).Count);
            }
            Assert.GreaterOrEqual(SaveSystem.Data.meta.jobsDone, 31);
        }

        [UnityTest]
        public IEnumerator The_panel_is_there_from_the_first_day_and_needs_all_four_parts()
        {
            // A brand-new save: nothing done, nothing unlocked.
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var panel = Object.FindAnyObjectByType<MachinePanel>();
            Assert.IsNotNull(panel, "a new player cannot see where this is going");
            Assert.That(WorldPrompt.For(panel.transform.position), Does.Contain("0 of 4").And.Contain("the fuse").And.Contain("the lens"));
            foreach (var lamp in panel.lamps) Assert.IsFalse(lamp.activeSelf, "a part is showing that has not been found");

            // Three in: three lamps, and still no.
            for (int i = 0; i < 3; i++) Assert.IsTrue(MachineParts.Take(MachineParts.All[i]));
            Assert.IsFalse(MachineParts.Take(MachineParts.All[0]), "the same part was taken twice");
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            panel = Object.FindAnyObjectByType<MachinePanel>();
            Assert.IsTrue(panel.lamps[0].activeSelf && panel.lamps[2].activeSelf && !panel.lamps[3].activeSelf);
            Assert.That(WorldPrompt.For(panel.transform.position), Does.Contain("3 of 4").And.Contain("Still out there").And.Contain("the lens"));
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            yield return null;
            player.Teleport(panel.transform.position + Vector3.up * 0.05f);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual("Lab", SceneManager.GetActiveScene().name, "the machine switched on with a part missing");
        }

        [UnityTest]
        public IEnumerator Four_parts_switch_the_machine_on_and_beating_it_starts_the_next_cycle()
        {
            foreach (var part in MachineParts.All) MachineParts.Take(part);
            SaveSystem.Data.meta.runsStarted = 6;
            SaveSystem.Data.run.active = true;
            SaveSystem.Data.run.floorNumber = 4;
            SaveSystem.Data.run.depth = 1;
            SaveSystem.Data.run.biome = "ants";
            SaveSystem.Data.run.seed = 1234;
            SaveSystem.Data.run.health = 3;
            SaveSystem.Save();

            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var panel = Object.FindAnyObjectByType<MachinePanel>();
            Assert.That(WorldPrompt.For(panel.transform.position), Does.Contain("all 4 parts in hand"));
            // The panel only keeps count now (the machine is built in a run, see SpecialTests): straight to it.
            PlayerController player;
            RunDirector.Descent = true;
            RunDirector.ToMachine = true;
            SceneManager.LoadScene("Run");
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == "Run" && FloorManager.Instance != null && FloorManager.Instance.Layout != null, 5f, "the machine room to load");
            yield return null;

            var floors = FloorManager.Instance;
            var director = RunDirector.Instance;
            player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            Assert.IsTrue(director.IsMachine);
            Assert.AreEqual("machine", floors.biome.id, "the panel led to " + floors.biome.displayName);
            Assert.AreEqual(RadioMoment.MachineRoom, Radio.Instance.LastMoment);
            Assert.AreEqual(4, SaveSystem.Data.run.floorNumber, "the saved run was overwritten");

            player.Teleport(floors.ArenaCentre + new Vector3(0f, 0.05f, -12f));
            yield return WaitUntil(() => floors.Boss != null, 5f, "the machine");
            Assert.IsInstanceOf<MachineBoss>(floors.Boss);
            var machine = (MachineBoss)floors.Boss;
            yield return WaitUntil(() => Time.timeScale > 0f, 8f, "the title card to pass");
            Time.timeScale = 3f;

            // It does not move. It throws rings.
            Vector3 stood = machine.transform.position;
            yield return WaitUntil(() => machine.Rings >= 2, 20f, "a volley");
            Assert.Greater(Bullet.Live.Count, 4, "a ring with nothing in it");

            // The pull: the floor round it counts down, and the player is dragged toward it.
            player.Teleport(machine.transform.position + new Vector3(0f, 0.05f, -11f));
            yield return WaitUntil(() => machine.Pulling, 20f, "the pull");
            Assert.Greater(GroundHazard.All.Count, 0, "the pull came with no warning on the floor");
            float before = (player.transform.position - machine.transform.position).magnitude;
            yield return new WaitForSeconds(1f);
            Assert.Less((player.transform.position - machine.transform.position).magnitude, before - 1f, "the pull did not pull");
            Assert.Less((machine.transform.position - stood).magnitude, 0.2f, "the machine wandered off");

            // Two thirds: something comes through from another world.
            machine.Health.TakeDamage(Mathf.CeilToInt(machine.Health.maxHealth * 0.36f), Vector3.forward);
            yield return WaitUntil(() => machine.Phase == 2, 3f, "the second phase");
            yield return null;
            Assert.AreEqual(machine.visitorsPerPhase, machine.VisitorsLetThrough);
            Assert.Greater(floors.LiveEnemies.Count, 1, "nothing came through");

            // Beaten: it restarts. A cycle on, the parts are gone, and the only way on is home.
            machine.Health.Kill();
            yield return WaitUntil(() => floors.ExtractPortal != null, 6f, "the way home");
            Time.timeScale = 1f;
            Assert.AreEqual(1, MachineParts.Cycle);
            Assert.AreEqual(0, MachineParts.Count, "the parts stayed in after the restart");
            Assert.IsTrue(Achievements.Has(AchievementId.Reboot));
            Assert.AreEqual(0, SaveSystem.Data.meta.finchBeaten, "beating the machine counted as the cat");

            player.Teleport(floors.ExtractPortal.transform.position + new Vector3(5f, 0.05f, 0f));
            yield return null;
            yield return null;
            player.Teleport(floors.ExtractPortal.transform.position + Vector3.up * 0.05f);
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == "Lab" && Radio.Instance != null && Radio.Instance.SaidCount > 0, 5f, "the lab");
            yield return null;

            Assert.AreEqual(RadioMoment.Cycle, Radio.Instance.LastMoment);
            Assert.That(Radio.Instance.LastLine, Does.Contain("Let's leave it at near"), "the first restart did not shake the first of the story out of Finch");
            Assert.AreEqual("CYCLE 2", ScreenCards.Instance.LastHeading);
            Assert.That(Object.FindAnyObjectByType<Hud>().LastToast, Does.Contain("Cycle 2"));
            Assert.IsTrue(SaveSystem.Data.run.active, "the saved run was thrown away");
            panel = Object.FindAnyObjectByType<MachinePanel>();
            Assert.That(WorldPrompt.For(panel.transform.position), Does.Contain("0 of 4").And.Contain("Cycle 2"));

            // The next run starts a depth down, and is still a new run as far as the radio goes.
            Assert.AreEqual(2, TimeClock.StartDepth);
            SaveSystem.Data.run = new RunSave();
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            Assert.AreEqual(2, FloorManager.Instance.depth, "the new cycle is no harder than the last");
            Assert.AreEqual(1, FloorManager.Instance.FloorNumber);
            Assert.AreEqual(RadioMoment.RunStart, Radio.Instance.LastMoment);

            // And the lab does not announce the cycle twice.
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            Assert.AreNotEqual("CYCLE 2", ScreenCards.Instance.LastHeading);
        }
    }
}
