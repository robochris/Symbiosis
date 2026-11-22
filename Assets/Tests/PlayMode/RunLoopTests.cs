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
            ScreenCards.ShowInTests = false;
            ScreenCards.TitleSeen = false;
            Achievements.EverythingUnlocked = false;
        }

        [UnityTest]
        public IEnumerator The_lab_fills_in_as_the_player_gets_further()
        {
            // A brand-new save: the portal and the workbench, and nothing else yet.
            yield return Load("Lab");
            Assert.IsNull(Object.FindAnyObjectByType<TrophyBoard>(), "the record board is there before there is anything to record");
            Assert.IsNull(Object.FindAnyObjectByType<StyleMirror>(), "the mirror is there before there is a second style");
            Assert.IsNotNull(Object.FindAnyObjectByType<LabPortal>());
            Assert.AreEqual(System.Enum.GetValues(typeof(UpgradeType)).Length, Object.FindObjectsByType<UpgradePad>(FindObjectsSortMode.None).Length);

            // One run and one style later, both are there, and the lab says so once.
            SaveSystem.Data.meta.runsStarted = 1;
            Achievements.Unlock(AchievementId.FirstSteps);
            yield return Load("Lab");
            Assert.IsNotNull(Object.FindAnyObjectByType<TrophyBoard>());
            Assert.IsNotNull(Object.FindAnyObjectByType<StyleMirror>());
            var hud = Object.FindAnyObjectByType<Hud>();
            yield return WaitUntil(() => (hud.LastToast ?? "").Contains("New in the lab"), 8f, "the lab to say what is new");
            Assert.That(hud.LastToast, Does.Contain("record board").And.Contain("mirror"));

            yield return Load("Lab");
            yield return new WaitForSeconds(0.3f);
            Assert.That(Object.FindAnyObjectByType<Hud>().LastToast ?? "", Does.Not.Contain("New in the lab"), "it announced the same things twice");
        }

        [UnityTest]
        public IEnumerator The_game_opens_on_a_title_over_the_lab_once()
        {
            ScreenCards.ShowInTests = true;
            ScreenCards.TitleSeen = false;
            yield return Load("Lab");
            var cards = ScreenCards.Instance;
            Assert.IsNotNull(cards, "the lab has no screen cards");
            Assert.IsTrue(cards.TitleShowing, "no title when the game opens");
            Assert.IsTrue(ScreenCards.Blocking);

            // The camera circles the Void, and the controls do nothing until the player starts.
            Vector3 view = Camera.main.transform.position;
            Player.Input.overrideFrame = null;
            Assert.AreEqual(Vector3.zero, Player.Input.Read().move);
            Assert.IsFalse(Player.Input.Read().fire);
            yield return new WaitForSeconds(0.4f);
            Assert.AreNotEqual(view, Camera.main.transform.position, "the title camera is not moving");
            Assert.IsFalse(CameraFollow.Instance.enabled);

            cards.DismissTitle();
            yield return null;
            Assert.IsFalse(ScreenCards.Blocking);
            Assert.IsTrue(CameraFollow.Instance.enabled, "the camera did not go back to the player");
            Player.Input.overrideFrame = new InputFrame();

            // Back to the lab later in the same sitting: no title again.
            yield return Load("Lab");
            Assert.IsFalse(ScreenCards.Instance.TitleShowing, "the title came back on returning to the lab");
        }

        [UnityTest]
        public IEnumerator Cards_announce_the_biome_each_floor_the_boss_and_the_end_of_the_biome()
        {
            ScreenCards.ShowInTests = true;
            ScreenCards.TitleSeen = true;
            yield return Load("Run");
            var cards = ScreenCards.Instance;
            Assert.AreEqual("BACKYARD ANT COLONY", cards.LastHeading, "arriving in a biome should say which");
            Assert.That(cards.LastLine, Does.Contain("acid"));
            Assert.IsTrue(cards.CardShowing || cards.LastHeading.Length > 0);

            Floors.twistOverride = FloorTwist.None;
            Floors.BuildFloor(3);
            yield return null;
            Assert.AreEqual("FLOOR 3", cards.LastHeading);
            Assert.That(ScreenCards.FloorLines, Does.Contain(cards.LastLine));
            Assert.AreEqual(1f, Time.timeScale, "a floor card must not stop the game");

            // The boss walks on: its card, a joke, and the world held still for a moment.
            Floors.BuildFloor(10);
            yield return null;
            Player.Health.SetInvulnerable(600f);
            Player.Teleport(Floors.ArenaCentre + new Vector3(0f, 0.05f, -8f));
            for (float t = 0f; t < 3f && Floors.Boss == null; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsNotNull(Floors.Boss);
            yield return null;
            Assert.AreEqual("ANT QUEEN", cards.LastHeading);
            Assert.IsNotEmpty(Floors.Boss.subtitle, "the boss has no line for its card");
            Assert.AreEqual(Floors.Boss.subtitle, cards.LastLine);
            Assert.AreEqual(0f, Time.timeScale, "the world should stand still for the boss's card");
            yield return new WaitForSecondsRealtime(cards.bossFreeze + 0.4f);
            Assert.AreEqual(1f, Time.timeScale, "the world never started again");

            // And when it falls: the summary.
            Assert.IsFalse(cards.SummaryShowing);
            Floors.Boss.Health.Kill();
            yield return WaitUntil(() => Floors.ExtractPortal != null, 3f, "the portals");
            yield return null;
            Assert.IsTrue(cards.SummaryShowing, "no summary at the end of the biome");
            Assert.That(cards.LastSummary, Does.Contain("BACKYARD ANT COLONY CLEARED").And.Contain("Kills").And.Contain("Hits taken 0"));
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
            // Stand clear of where the portals appear: a pad only counts a step onto it from outside, and in some
            // layouts the spot above is right beside the way home.
            Player.Teleport(Floors.ArenaCentre + new Vector3(0f, 0.05f, -9f));
            yield return null;
        }

        // ---------- the lab ----------

        [UnityTest]
        public IEnumerator Lab_has_a_pad_for_every_upgrade_and_a_portal_that_starts_a_run()
        {
            yield return Load("Lab");
            var pads = Object.FindObjectsByType<UpgradePad>(FindObjectsSortMode.None);
            Assert.AreEqual(System.Enum.GetValues(typeof(UpgradeType)).Length, pads.Length);
            // Far enough apart that standing on one never reads or buys its neighbour.
            foreach (var a in pads)
            {
                foreach (var b in pads)
                {
                    if (a != b) Assert.Greater((a.transform.position - b.transform.position).magnitude, 2.05f, a.name + " and " + b.name + " are too close");
                }
            }
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
                { UpgradeType.PocketMoney, new[] { 120, 360 } },
                { UpgradeType.RegularCustomer, new[] { 300 } },
            };
            int total = 0;
            foreach (var pair in expected)
            {
                CollectionAssert.AreEqual(pair.Value, Pad(pair.Key).upgrade.costs, pair.Key.ToString());
                foreach (int cost in pair.Value) total += cost;
            }
            Assert.AreEqual(4030, total, "the doc's six come to 3250; the two added since bring it to 4030");
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

            // Field Kit's random item can itself change health: a Sugar Cube or a Fishbowl adds one, a Paper Hat takes one.
            int sugar = Player.Items.Count(ItemType.SugarCube) + Player.Items.Count(ItemType.Fishbowl) - Player.Items.Count(ItemType.PaperHat);
            Assert.AreEqual(5 + sugar, Player.Health.maxHealth, "Thicker Shadow: 3 + 2");
            Assert.LessOrEqual(Player.Health.Current, 5 + sugar);
            Assert.GreaterOrEqual(Player.Health.Current, 5 + sugar - 1);
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
            // The lab says how it went: where, how far, and what was done on the way.
            Assert.That(Object.FindAnyObjectByType<Hud>().LastToast, Does.Contain("fell on floor 1").And.Contain("0 kills").And.Contain("Backyard Ant Colony"));

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

        [UnityTest]
        public IEnumerator The_mirror_changes_the_Voids_style_and_the_next_run_keeps_it()
        {
            Achievements.EverythingUnlocked = true; // this is about the mirror; what unlocks a style is tested with the achievements
            yield return Load("Lab");
            var mirror = Object.FindAnyObjectByType<StyleMirror>();
            Assert.IsNotNull(mirror, "the lab has no mirror");
            var style = Player.GetComponent<PlayerStyle>();
            Assert.IsNotNull(style);
            Assert.GreaterOrEqual(style.styles.Length, 4);
            Assert.AreEqual(0, style.Current, "a new player starts as the Shadow");

            Renderer torso = Player.visual.Find("Torso").GetComponent<Renderer>();
            Renderer eye = Player.visual.Find("Eye L").GetComponent<Renderer>();
            Assert.AreEqual(style.styles[0].body, torso.sharedMaterial);

            Assert.That(WorldPrompt.For(mirror.transform.position + Vector3.right), Does.Contain("Shadow"));
            StepOnto(mirror.transform.position);
            yield return null;
            yield return null;
            Assert.AreEqual(1, style.Current, "stepping in front of the mirror did nothing");
            Assert.AreEqual(style.styles[1].body, torso.sharedMaterial, "the body did not change");
            Assert.AreEqual(style.styles[1].eyes, eye.sharedMaterial, "the eyes did not change");
            Assert.AreEqual(style.styles[1].body, Player.visual.Find("Arm R/Limb").GetComponent<Renderer>().sharedMaterial, "the arms were left behind");
            Assert.AreEqual(1, SaveSystem.Data.meta.style);

            // Standing there does not keep flipping through them.
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(1, style.Current);

            // Into a run: same style, and an item worn on top keeps its own look.
            yield return Load("Run");
            var inRun = Player.GetComponent<PlayerStyle>();
            Assert.AreEqual(1, inRun.Current, "the run forgot the style");
            Assert.AreEqual(inRun.styles[1].body, Player.visual.Find("Torso").GetComponent<Renderer>().sharedMaterial);
            var item = GameAssets.Instance.items.Get(ItemType.ChitinPlate);
            Player.Items.Add(item);
            inRun.Next();
            Assert.AreEqual(2, inRun.Current);
            foreach (var part in Player.Items.LookIn(ItemSlot.Body).GetComponentsInChildren<Renderer>())
            {
                Assert.AreNotEqual(inRun.styles[2].body, part.sharedMaterial, "a style swap repainted an item");
            }
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

        /// <summary>Not a test: beats the Queen and steps into the way home over and over, to catch the time it does not work.</summary>
        [UnityTest, Explicit, Timeout(1800000)]
        public IEnumerator Hunt_for_a_failed_extraction()
        {
            for (int round = 0; round < 12; round++)
            {
                SaveSystem.Wipe();
                yield return Load("Run");
                Floors.BuildFloor(10);
                yield return null;
                yield return KillTheBoss();
                yield return new WaitForSeconds(1f);
                Vector3 portal = Floors.ExtractPortal.transform.position;
                var padField = typeof(FloorManager).GetField("_extractPad", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var armedField = typeof(StepPad).GetField("_armed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Vector3 before = Player.transform.position - portal;
                bool armedBefore = (bool)armedField.GetValue(padField.GetValue(Floors));
                Debug.Log($"[Shrunk] HUNT round {round}: before stepping, player {before.magnitude:F1} away, armed {armedBefore}, enabled {Floors.enabled}, node {Floors.CurrentNode?.type}, portal active {Floors.ExtractPortal.activeInHierarchy}");
                StepOnto(portal);
                float t = 0f;
                for (; t < 3f && SceneManager.GetActiveScene().name != "Lab"; t += Time.deltaTime) yield return null;
                if (SceneManager.GetActiveScene().name == "Lab") continue;
                Vector3 off = Player.transform.position - portal;
                Assert.Fail($"round {round}: still in the run. Player is {off.magnitude:F2} from the portal ({off}), locked {Floors.IsLocked}, ended {RunDirector.Instance.Ended}, " +
                            $"timeScale {Time.timeScale}, dead {Player.Health.IsDead}, health {Player.Health.Current}, seed {Floors.Layout.seed}, arena {Floors.Layout.hasArena}");
            }
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
