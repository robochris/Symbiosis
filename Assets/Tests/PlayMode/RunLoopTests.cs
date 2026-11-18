using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>Milestone 6: lab, upgrades, portal, extract, death and saving. Uses its own save file, never the player's.</summary>
    public class RunLoopTests
    {
        [SetUp]
        public void FreshSave()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            RunDirector.LastRunMessage = null;
        }

        [TearDown]
        public void CleanUp()
        {
            SaveSystem.Wipe();
            Time.timeScale = 1f;
        }

        static PlayerController Player => PlayerController.Instance;
        static FloorManager Floors => FloorManager.Instance;

        IEnumerator Load(string scene)
        {
            SceneManager.LoadScene(scene);
            yield return null;
            yield return null;
            Assert.AreEqual(scene, SceneManager.GetActiveScene().name);
            Assert.IsNotNull(Player, scene + " has no player");
            Player.Input.overrideFrame = new InputFrame();
        }

        IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.deltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        IEnumerator WaitForScene(string scene, float timeout)
        {
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == scene && Player != null, timeout, "the " + scene + " scene");
            yield return null;
            Player.Input.overrideFrame = new InputFrame();
        }

        static void StepOnto(Vector3 position)
        {
            Player.Teleport(new Vector3(position.x, 0.05f, position.z));
        }

        static UpgradePad Pad(UpgradeType type)
        {
            foreach (var pad in Object.FindObjectsByType<UpgradePad>(FindObjectsSortMode.None)) if (pad.upgrade.type == type) return pad;
            Assert.Fail("no pad for " + type);
            return null;
        }

        // Walks into the boss room on the current (boss) floor and kills the boss.
        IEnumerator KillTheBoss()
        {
            Room arena = Floors.RoomAt(Floors.Layout.exit.cell);
            Player.Health.SetInvulnerable(600f);
            Player.Teleport(arena.Centre + new Vector3(0f, 0.05f, -6f));
            yield return WaitUntil(() => Floors.IsLocked && Floors.Boss != null, 2f, "the boss fight to start");
            Floors.Boss.Health.Kill();
            yield return WaitUntil(() => !Floors.IsLocked, 2f, "the boss room to open");
        }

        // ---------- the lab ----------

        [UnityTest]
        public IEnumerator Lab_has_six_upgrade_pads_and_a_portal_that_starts_a_run()
        {
            yield return Load("Lab");
            Assert.AreEqual(6, Object.FindObjectsByType<UpgradePad>(FindObjectsSortMode.None).Length);
            var portal = Object.FindAnyObjectByType<LabPortal>();
            Assert.IsNotNull(portal);
            Assert.That(WorldPrompt.For(portal.transform.position + Vector3.forward * 2f), Does.Contain("start a run"));

            // Standing in the middle of the lab starts nothing.
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual("Lab", SceneManager.GetActiveScene().name);

            StepOnto(portal.transform.position);
            yield return WaitForScene("Run", 3f);
            Assert.AreEqual(1, Floors.FloorNumber);
            Assert.AreEqual(1, Floors.depth);
            Assert.IsTrue(SaveSystem.Data.run.active, "the run was not saved at the start of floor 1");
            Assert.AreEqual(1, SaveSystem.Data.meta.runsStarted);
        }

        [UnityTest]
        public IEnumerator Upgrades_cost_samples_and_are_remembered()
        {
            yield return Load("Lab");
            var pad = Pad(UpgradeType.ThickerShadow);
            Assert.That(WorldPrompt.For(pad.transform.position + Vector3.back * 1.5f), Does.Contain("Thicker Shadow (0/3)").And.Contain("50 samples"));

            // No samples: stepping on the pad buys nothing.
            StepOnto(pad.transform.position);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(0, Upgrades.Level(UpgradeType.ThickerShadow));

            // 60 samples: one level (50), and standing there does not buy a second.
            SaveSystem.Data.meta.samples = 260;
            StepOnto(pad.transform.position + Vector3.back * 3f);
            yield return null;
            StepOnto(pad.transform.position);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(1, Upgrades.Level(UpgradeType.ThickerShadow));
            Assert.AreEqual(210, SaveSystem.Data.meta.samples);
            Assert.IsTrue(pad.levelMarkers[0].activeSelf, "the lab did not show the new level");
            Assert.IsFalse(pad.levelMarkers[1].activeSelf);

            // Step off and on again: level 2 (150).
            StepOnto(pad.transform.position + Vector3.back * 3f);
            yield return null;
            StepOnto(pad.transform.position);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(2, Upgrades.Level(UpgradeType.ThickerShadow));
            Assert.AreEqual(60, SaveSystem.Data.meta.samples);

            // Level 3 costs 400: not enough.
            StepOnto(pad.transform.position + Vector3.back * 3f);
            yield return null;
            StepOnto(pad.transform.position);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(2, Upgrades.Level(UpgradeType.ThickerShadow));

            // Still there after the game is closed and opened again.
            SaveSystem.Load();
            Assert.AreEqual(2, Upgrades.Level(UpgradeType.ThickerShadow));
            Assert.AreEqual(60, SaveSystem.Data.meta.samples);
        }

        [UnityTest]
        public IEnumerator Every_upgrade_matches_the_design_doc()
        {
            yield return Load("Lab");
            var expected = new Dictionary<UpgradeType, int[]>
            {
                { UpgradeType.ThickerShadow, new[] { 50, 150, 400 } },
                { UpgradeType.QuickReflexes, new[] { 100, 300 } },
                { UpgradeType.SamplePouch, new[] { 150, 450 } },
                { UpgradeType.OverchargeCoil, new[] { 200, 500 } },
                { UpgradeType.FieldKit, new[] { 350 } },
                { UpgradeType.LuckyFind, new[] { 600 } },
            };
            int total = 0;
            foreach (var pair in expected)
            {
                CollectionAssert.AreEqual(pair.Value, Pad(pair.Key).upgrade.costs, pair.Key.ToString());
                foreach (int cost in pair.Value) total += cost;
            }
            Assert.AreEqual(3250, total, "everything together should cost about what the doc says");
        }

        // ---------- upgrades in a run ----------

        [UnityTest]
        public IEnumerator Upgrades_change_the_next_run()
        {
            Upgrades.SetLevel(UpgradeType.ThickerShadow, 2);
            Upgrades.SetLevel(UpgradeType.QuickReflexes, 2);
            Upgrades.SetLevel(UpgradeType.OverchargeCoil, 1);
            Upgrades.SetLevel(UpgradeType.FieldKit, 1);
            Upgrades.SetLevel(UpgradeType.LuckyFind, 1);
            yield return Load("Run");

            // Field Kit's random item can itself be a Sugar Cube (+1 health).
            int sugar = Player.Items.Count(ItemType.SugarCube);
            Assert.AreEqual(5 + sugar, Player.Health.maxHealth, "Thicker Shadow: 3 + 2");
            Assert.AreEqual(5 + sugar, Player.Health.Current);
            Assert.AreEqual(1.3f, Player.dodgeRechargeMultiplier, 0.001f, "Quick Reflexes");
            Assert.AreEqual(1.2f, Player.Power.fillMultiplier, 0.001f, "Overcharge Coil");
            Assert.AreEqual(1, Player.Items.Collected.Count, "Field Kit: one starting item");
            Assert.IsFalse(Player.Items.Collected[0].rare);

            // Lucky Find: the treasure room offers two; taking one removes the other.
            RoomNode treasure = null;
            foreach (var node in Floors.Layout.ordered) if (node.type == RoomType.Treasure) treasure = node;
            Room room = Floors.RoomAt(treasure.cell);
            var offers = new List<Pickup>();
            foreach (var pickup in Pickup.All) if (pickup.treasureRoom == room) offers.Add(pickup);
            Assert.AreEqual(2, offers.Count, "Lucky Find should offer two items");
            Assert.AreNotEqual(offers[0].item.type, offers[1].item.type);

            Vector3 second = offers[1].transform.position;
            StepOnto(second);
            yield return WaitUntil(() => Player.Items.Collected.Count == 2, 1f, "the chosen item");
            yield return null;
            Assert.IsTrue(offers[0] == null, "the item not chosen is still there");
        }

        [UnityTest]
        public IEnumerator Quick_Reflexes_really_shortens_the_dodge_cooldown()
        {
            yield return Load("Run");
            Player.Input.overrideFrame = new InputFrame { move = Vector3.zero, dodge = true };
            yield return null;
            Player.Input.overrideFrame = new InputFrame();
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(0, Player.DodgeCharges, "the plain dodge came back in under a second");

            Upgrades.SetLevel(UpgradeType.QuickReflexes, 2);
            SaveSystem.Data.run = new RunSave();
            yield return Load("Run");
            Player.Input.overrideFrame = new InputFrame { move = Vector3.zero, dodge = true };
            yield return null;
            Player.Input.overrideFrame = new InputFrame();
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(1, Player.DodgeCharges, "30% faster recharge should have the dodge back by now");
        }

        // ---------- death ----------

        [UnityTest]
        public IEnumerator Dying_sends_a_quarter_of_the_samples_home()
        {
            yield return Load("Run");
            RunState.AddSamples(100);
            RunState.AddCoins(40);
            Player.Health.Kill();

            yield return WaitForScene("Lab", 4f);
            Assert.AreEqual(25, SaveSystem.Data.meta.samples);
            Assert.AreEqual(1, SaveSystem.Data.meta.deaths);
            Assert.IsFalse(SaveSystem.Data.run.active, "a dead run was left saved");
            Assert.AreEqual(3, Player.Health.Current, "arrived in the lab hurt");

            var portal = Object.FindAnyObjectByType<LabPortal>();
            Assert.That(WorldPrompt.For(portal.transform.position + Vector3.forward * 2f), Does.Contain("start a run"));
        }

        [UnityTest]
        public IEnumerator Sample_Pouch_keeps_more_on_death()
        {
            Upgrades.SetLevel(UpgradeType.SamplePouch, 2);
            yield return Load("Run");
            RunState.AddSamples(100);
            Player.Health.Kill();
            yield return WaitForScene("Lab", 4f);
            Assert.AreEqual(45, SaveSystem.Data.meta.samples);
        }

        // ---------- the portal room: extract or go deeper ----------

        [UnityTest]
        public IEnumerator After_the_Queen_two_portals_appear_and_extracting_banks_everything()
        {
            yield return Load("Run");
            Floors.BuildFloor(10);
            yield return null;
            Assert.IsNull(Floors.ExtractPortal);
            yield return KillTheBoss();

            Assert.IsNotNull(Floors.ExtractPortal, "no extract portal after the biome boss");
            Assert.IsNotNull(Floors.DeeperPortal, "no deeper portal after the biome boss");
            Room arena = Floors.RoomAt(Floors.Layout.exit.cell);
            Assert.IsFalse(arena.ExitHole.activeSelf, "floor 10 should end in portals, not a hole");

            // Samples picked up from the Queen fly in over a moment; wait, then top up to a known number.
            yield return new WaitForSeconds(1f);
            RunState.AddSamples(200 - RunState.Samples);
            Assert.That(WorldPrompt.For(Floors.ExtractPortal.transform.position + Vector3.back * 2f), Does.Contain("200 samples"));

            StepOnto(Floors.ExtractPortal.transform.position);
            yield return WaitForScene("Lab", 3f);
            Assert.AreEqual(200, SaveSystem.Data.meta.samples);
            Assert.AreEqual(1, SaveSystem.Data.meta.extractions);
            Assert.AreEqual(1, SaveSystem.Data.meta.rareMaterials);
            Assert.IsFalse(SaveSystem.Data.run.active);
        }

        [UnityTest]
        public IEnumerator Going_deeper_raises_the_multiplier_and_the_mutation_budget()
        {
            yield return Load("Run");
            Floors.BuildFloor(10);
            yield return null;
            yield return KillTheBoss();

            Assert.That(WorldPrompt.For(Floors.DeeperPortal.transform.position + Vector3.back * 2f), Does.Contain("depth 2").And.Contain("×1.5"));
            StepOnto(Floors.DeeperPortal.transform.position);
            yield return WaitUntil(() => Floors.FloorNumber == 11, 2f, "the next biome");
            yield return null;

            Assert.AreEqual(2, Floors.depth);
            Assert.AreEqual(1, Floors.FloorInBiome, "floor 11 should be floor 1 of the next biome");
            Assert.AreEqual(2, MutationBudget.MaxPoints(Floors.depth, Floors.FloorInBiome));
            Assert.AreEqual(1.5f, RunDirector.RewardMultiplier(Floors.depth), 0.001f);
            Assert.AreEqual(2, SaveSystem.Data.run.depth, "the new depth was not saved");

            // Extracting later pays out at the higher rate.
            RunState.AddSamples(100 - RunState.Samples);
            RunDirector.Instance.Extract();
            yield return WaitForScene("Lab", 3f);
            Assert.AreEqual(150, SaveSystem.Data.meta.samples);
            Assert.AreEqual(2, SaveSystem.Data.meta.rareMaterials);
        }

        // ---------- saving mid-run ----------

        [UnityTest]
        public IEnumerator A_run_resumes_at_the_start_of_its_floor_after_quitting()
        {
            yield return Load("Run");
            var library = GameAssets.Instance.items;
            Player.Items.Add(library.Get(ItemType.NeedleTip));
            Player.Items.Add(library.Get(ItemType.SugarCube));
            Player.Items.Add(library.Get(ItemType.NeedleTip));
            Player.Health.ClearInvulnerability();
            Player.Health.TakeDamage(1, Vector3.forward);
            RunState.AddCoins(17);
            RunState.AddSamples(33);
            Player.Power.SetMeter(0.6f);

            // Reaching the next floor is what saves.
            Floors.BuildFloor(3);
            yield return null;
            string layout = Floors.Layout.ToAscii();
            int seed = Floors.Layout.seed;

            // Things that happen after the save are lost on purpose: you restart the floor.
            RunState.AddCoins(1000);

            // Quit and come back: nothing kept in memory, only the file.
            SaveSystem.Load();
            Assert.IsTrue(SaveSystem.Data.run.active);
            yield return Load("Lab");
            var portal = Object.FindAnyObjectByType<LabPortal>();
            Assert.That(WorldPrompt.For(portal.transform.position + Vector3.forward * 2f), Does.Contain("floor 3"));
            StepOnto(portal.transform.position);
            yield return WaitForScene("Run", 3f);

            Assert.AreEqual(3, Floors.FloorNumber);
            Assert.AreEqual(seed, Floors.Layout.seed);
            Assert.AreEqual(layout, Floors.Layout.ToAscii(), "a different floor was built");
            Assert.AreEqual(17, RunState.Coins);
            Assert.AreEqual(33, RunState.Samples);
            Assert.AreEqual(2, Player.Items.Count(ItemType.NeedleTip));
            Assert.AreEqual(1, Player.Items.Count(ItemType.SugarCube));
            Assert.AreEqual(4, Player.Health.maxHealth, "Sugar Cube's extra health was lost");
            Assert.AreEqual(3, Player.Health.Current, "health was not as it stood");
            Assert.AreEqual(0.6f, Player.Power.Meter, 0.001f);
            Assert.AreEqual(4, Player.Items.Stats.pierce);
            Assert.AreEqual(1, SaveSystem.Data.meta.runsStarted, "resuming was counted as a new run");
        }

        // ---------- the whole loop ----------

        [UnityTest]
        public IEnumerator Full_loop_lab_run_extract_upgrade_run_die_lab()
        {
            // Lab -> portal -> run.
            yield return Load("Lab");
            StepOnto(Object.FindAnyObjectByType<LabPortal>().transform.position);
            yield return WaitForScene("Run", 3f);

            // Skip to the Queen, beat her, extract.
            Floors.BuildFloor(10);
            yield return null;
            yield return KillTheBoss();
            yield return new WaitForSeconds(1f);
            RunState.AddSamples(120);
            int carried = RunState.Samples;
            StepOnto(Floors.ExtractPortal.transform.position);
            yield return WaitForScene("Lab", 3f);
            Assert.AreEqual(carried, SaveSystem.Data.meta.samples);

            // Spend it.
            var pad = Pad(UpgradeType.ThickerShadow);
            StepOnto(pad.transform.position);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(1, Upgrades.Level(UpgradeType.ThickerShadow));

            // Go again, with the upgrade.
            StepOnto(Object.FindAnyObjectByType<LabPortal>().transform.position);
            yield return WaitForScene("Run", 3f);
            Assert.AreEqual(1, Floors.FloorNumber);
            Assert.AreEqual(4, Player.Health.maxHealth);
            Assert.AreEqual(0, RunState.Samples, "samples carried over from the last run");
            Assert.AreEqual(0, Player.Items.Collected.Count, "items carried over from the last run");

            // Die; back to the lab, ready to go again.
            RunState.AddSamples(40);
            int banked = SaveSystem.Data.meta.samples;
            Player.Health.Kill();
            yield return WaitForScene("Lab", 4f);
            Assert.AreEqual(banked + 10, SaveSystem.Data.meta.samples);
            Assert.AreEqual(2, SaveSystem.Data.meta.runsStarted);
            Assert.IsNotNull(Object.FindAnyObjectByType<LabPortal>());
        }
    }
}
