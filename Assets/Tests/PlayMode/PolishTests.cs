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
            GameSettings.Reset();
            ScreenCards.ShowInTests = false;
            ScreenCards.TitleSeen = false;
            Time.timeScale = 1f;
        }

        [Test]
        public void Each_biome_has_its_own_lines_between_floors()
        {
            var seen = new HashSet<string>();
            foreach (var lines in new[] { ScreenCards.FloorLines, ScreenCards.CakeLines, ScreenCards.SofaLines, ScreenCards.FridgeLines })
            {
                Assert.GreaterOrEqual(lines.Length, 10);
                foreach (string line in lines)
                {
                    Assert.IsTrue(seen.Add(line), "said in two places: " + line);
                    Assert.LessOrEqual(line.Length, 90, "too long for the card: " + line);
                }
            }
            CollectionAssert.Contains(ScreenCards.CakeLines, ScreenCards.TipFor("cake", 12345));
            CollectionAssert.Contains(ScreenCards.SofaLines, ScreenCards.TipFor("sofa", -7));
            CollectionAssert.Contains(ScreenCards.FridgeLines, ScreenCards.TipFor("fridge", 99));
            CollectionAssert.Contains(ScreenCards.FloorLines, ScreenCards.TipFor("ants", 3));
            CollectionAssert.Contains(ScreenCards.FloorLines, ScreenCards.TipFor("bottom", 3));
        }

        [UnityTest]
        public IEnumerator The_way_in_to_a_boss_is_marked_and_warns()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var floors = FloorManager.Instance;
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            Assert.AreEqual(0, BossGate.All.Count, "an ordinary floor has a boss gate");

            floors.BuildFloor(10, 4242);
            yield return null;
            Assert.Greater(BossGate.All.Count, 0, "no arch on the way in to the boss");
            foreach (var gate in BossGate.All)
            {
                // Each one stands in a doorway of the boss's room: just outside one of its squares.
                Vector2Int cell = floors.CellOf(gate.transform.parent.position);
                Assert.IsTrue(cell == floors.Layout.exit.cell || floors.Layout.InArena(cell), "an arch on a room that is not the boss's");
                Assert.IsNull(gate.GetComponentInChildren<Collider>(), "the arch can be walked into");
            }

            // Standing at one, before the fight: a warning. Not during the fight, and not after.
            Vector3 at = BossGate.All[0].transform.position;
            Assert.IsNotEmpty(WorldPrompt.For(at) ?? "", "no warning at the arch");
            Assert.IsNull(WorldPrompt.For(at + new Vector3(40f, 0f, 40f)));
            player.Teleport(floors.ArenaCentre + Vector3.up * 0.05f);
            yield return WaitUntil(() => floors.Boss != null, 5f, "the boss");
            Assert.IsNull(WorldPrompt.For(at), "still warning with the boss in plain view");
            floors.Boss.Health.Kill();
            yield return WaitUntil(() => floors.ExtractPortal != null, 8f, "the portals");
            foreach (var gate in BossGate.All) Assert.IsNull(WorldPrompt.For(gate.transform.position), "still warning with the boss dead");

            floors.BuildFloor(11);
            yield return null;
            yield return null;
            Assert.AreEqual(0, BossGate.All.Count, "the arches outlived their floor");
        }

        [UnityTest]
        public IEnumerator The_cupboard_issues_three_weapons_that_shoot_differently()
        {
            SaveSystem.Data.meta.runsStarted = 2;
            SaveSystem.Data.meta.bossesBeaten = 1;
            Achievements.Unlock(AchievementId.MothersDay);
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var rack = Object.FindAnyObjectByType<WeaponRack>();
            Assert.IsNotNull(rack, "the lab has no equipment cupboard");

            // The Sprinkler is issued, the Syringe is not: the cupboard goes between the two it has and says what the third takes.
            Assert.AreEqual(WeaponKind.Pipette, Weapons.Chosen);
            Assert.AreEqual(WeaponKind.Sprinkler, rack.Next());
            Assert.AreEqual(RadioMoment.Weapon, Radio.Instance.LastMoment, "Finch had nothing to say about the new weapon");
            Assert.AreEqual(WeaponKind.Pipette, rack.Next(), "a weapon was handed out before it was issued");
            Assert.That(WorldPrompt.For(rack.transform.position), Does.Contain("you carry the Pipette").And.Contain("Ant Queen"));
            Achievements.Unlock(AchievementId.Regicide);
            Assert.AreEqual(WeaponKind.Sprinkler, rack.Next());
            Assert.AreEqual(WeaponKind.Syringe, rack.Next());

            // It is remembered, and carried into a run.
            SaveSystem.Load();
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            Weapon weapon = player.weapon;
            Assert.AreEqual(WeaponKind.Syringe, weapon.kind);
            player.Input.overrideFrame = new InputFrame();
            yield return new WaitForSeconds(0.5f);

            // The Syringe: one shot a pull, three damage, and it goes through the first thing it hits.
            int before = Bullet.Live.Count;
            weapon.Tick(true, Vector3.forward);
            Assert.AreEqual(before + 1, Bullet.Live.Count);
            Bullet heavy = null;
            foreach (var bullet in Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None)) if (bullet.Shot != null && bullet.Shot.damage == weapon.damage * 3) heavy = bullet;
            Assert.IsNotNull(heavy, "the Syringe does not hit three times as hard");
            Assert.AreEqual(1, heavy.Shot.pierce);
            Assert.Greater(heavy.transform.localScale.z, heavy.transform.localScale.x * 2f, "the Syringe's shot looks like any other");
            Assert.Greater(weapon.Reach, weapon.range);

            // The Sprinkler: three a pull, slower to fire again, and short.
            yield return new WaitForSeconds(1f);
            weapon.kind = WeaponKind.Sprinkler;
            before = Bullet.Live.Count;
            weapon.Tick(true, Vector3.forward);
            Assert.AreEqual(before + 3, Bullet.Live.Count, "the Sprinkler did not throw three");
            Assert.Less(weapon.Reach, weapon.range * 0.6f);
            yield return new WaitForSeconds(weapon.fireInterval * 1.3f);
            weapon.Tick(true, Vector3.forward);
            Assert.AreEqual(1, weapon.ShotsFired - 1, "the Sprinkler fires as fast as the Pipette");
            yield return new WaitForSeconds(weapon.fireInterval);
            weapon.Tick(true, Vector3.forward);
            Assert.AreEqual(3, weapon.ShotsFired);
        }

        [UnityTest]
        public IEnumerator The_power_rack_hands_out_only_what_has_been_issued()
        {
            SaveSystem.Data.meta.runsStarted = 2;
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var rack = Object.FindAnyObjectByType<PowerRack>();
            Assert.IsNotNull(rack, "the lab has no power rack");
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();

            // A new intern has the one power, and is told what the next takes.
            Assert.AreEqual(PowerKind.Overcharge, HeroPower.Chosen);
            Assert.AreEqual(PowerKind.Overcharge, rack.Next(), "a power was handed out before it was issued");
            Assert.That(WorldPrompt.For(rack.transform.position), Does.Contain("you carry Overcharge").And.Contain("first extraction"));

            // After an extraction: Freeze Frame. After three falls: Second Wind. Round and back to the start.
            SaveSystem.Data.meta.extractions = 1;
            Assert.AreEqual(PowerKind.FreezeFrame, rack.Next());
            Assert.AreEqual(PowerKind.FreezeFrame, player.Power.Kind, "the Void is not carrying what was chosen");
            Assert.AreEqual(PowerKind.Overcharge, rack.Next());
            SaveSystem.Data.meta.deaths = 3;
            Assert.AreEqual(PowerKind.FreezeFrame, rack.Next());
            Assert.AreEqual(PowerKind.SecondWind, rack.Next());
            Assert.That(WorldPrompt.For(rack.transform.position), Does.Contain("you carry Second Wind"));

            // It is remembered, and carried into a run.
            SaveSystem.Load();
            Assert.AreEqual(PowerKind.SecondWind, HeroPower.Chosen);
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            Assert.AreEqual(PowerKind.SecondWind, PlayerController.Instance.Power.Kind);
            Assert.AreEqual("Second Wind", PlayerController.Instance.Power.displayName);
        }

        [UnityTest]
        public IEnumerator The_playtest_shortcut_unlocks_all_of_it()
        {
            Playtest.UnlockEverything();
            Assert.AreEqual(Achievements.All.Length, Achievements.Count);
            Assert.IsTrue(Achievements.BiomeUnlocked("cake") && Achievements.BiomeUnlocked("sofa"));
            Assert.AreEqual(FinchNotes.All.Length, FinchNotes.Count);
            Assert.AreEqual(Specimens.All.Length, Specimens.Count);
            foreach (var info in HeroPower.All) Assert.IsTrue(HeroPower.Unlocked(info.kind), info.name + " is still locked");
            // Twice does no harm.
            Playtest.UnlockEverything();
            Assert.AreEqual(Achievements.All.Length, Achievements.Count);

            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            Assert.IsNotNull(Object.FindAnyObjectByType<Playtest>(), "the lab has no playtest keys");
            Assert.IsNotNull(Object.FindAnyObjectByType<HiddenDoor>(), "the door is not there with everything unlocked");
            Assert.IsNotNull(Object.FindAnyObjectByType<SpecimenShelf>());
            Assert.IsNotNull(Object.FindAnyObjectByType<PowerRack>());
            // Not the time clock: that is for whoever has really been to the bottom.
            Assert.IsNull(Object.FindAnyObjectByType<TimeClock>(), "overtime offered without beating Finch");
            Assert.AreEqual(1, TimeClock.StartDepth);
            var style = PlayerController.Instance.GetComponent<PlayerStyle>();
            Assert.AreEqual(style.styles.Length, style.UnlockedCount);
        }

        [UnityTest]
        public IEnumerator Relaxed_mode_is_two_more_health_and_can_be_switched_mid_run()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            int usual = player.Health.maxHealth;

            // Switched on in the pause menu, in the middle of a run: two more, straight away.
            var menu = PauseMenu.Instance;
            int row = menu.RowNamed("Difficulty");
            Assert.GreaterOrEqual(row, 0, "no difficulty row in the pause menu");
            menu.Open();
            yield return null;
            Assert.That(menu.RowText(row), Does.Contain("As intended"));
            menu.Use(row);
            yield return null;
            Assert.That(menu.RowText(row), Does.Contain("Relaxed"));
            menu.Close();
            Assert.AreEqual(usual + GameSettings.RelaxedHealth, player.Health.maxHealth);
            Assert.AreEqual(usual + GameSettings.RelaxedHealth, player.Health.Current);

            // It is remembered: the next run starts with it.
            SaveSystem.Wipe();
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            player = PlayerController.Instance;
            Assert.AreEqual(usual + GameSettings.RelaxedHealth, player.Health.maxHealth);

            // And off again takes them away without killing anybody.
            PauseMenu.Instance.Use(PauseMenu.Instance.RowNamed("Difficulty"));
            Assert.AreEqual(usual, player.Health.maxHealth);
            Assert.AreEqual(usual, player.Health.Current);
            GameSettings.Reset();
        }

        [UnityTest]
        public IEnumerator Pocket_Money_and_Regular_Customer_do_what_they_say()
        {
            Upgrades.SetLevel(UpgradeType.PocketMoney, 2);
            Upgrades.SetLevel(UpgradeType.RegularCustomer, 1);
            SaveSystem.Save();
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            Assert.AreEqual(2 * Upgrades.CoinsPerPocketLevel, RunState.Coins, "no pocket money at the start of the run");

            ShopReroll bell = null;
            var floors = FloorManager.Instance;
            for (int floor = 1; floor <= 4 && bell == null; floor++)
            {
                if (floor > 1) floors.BuildFloor(floor);
                yield return null;
                bell = Object.FindAnyObjectByType<ShopReroll>();
            }
            Assert.IsNotNull(bell, "no shop in four floors");
            Assert.AreEqual(0, bell.price, "the first ring should be free");
            Assert.That(WorldPrompt.For(bell.transform.position), Does.Contain("on the house"));
            int coins = RunState.Coins;
            Assert.IsTrue(bell.TryReroll());
            Assert.AreEqual(coins, RunState.Coins);
            Assert.AreEqual(ShopReroll.FirstPrice, bell.price, "only the first ring is free");

            // A run picked up again does not pay out a second time.
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            Assert.AreEqual(coins, RunState.Coins, "a resumed run was given pocket money again");
        }

        [UnityTest]
        public IEnumerator Items_found_in_a_run_are_on_file_in_the_lab()
        {
            SaveSystem.Data.meta.runsStarted = 2;
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var cabinet = Object.FindAnyObjectByType<FilingCabinet>(FindObjectsInactive.Include);
            Assert.IsNotNull(cabinet, "the lab has no filing cabinet");
            Assert.IsFalse(cabinet.gameObject.activeInHierarchy, "the cabinet is there before anything has been found");

            // Two kinds picked up in a run, one of them twice: two cards.
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var library = GameAssets.Instance.items;
            var items = PlayerController.Instance.Items;
            items.Add(library.Get(ItemType.NeedleTip));
            items.Add(library.Get(ItemType.NeedleTip));
            items.Add(library.Get(ItemType.Lunchbox));
            Assert.AreEqual(2, FilingCabinet.Filed);
            SaveSystem.Save();
            SaveSystem.Load();
            Assert.AreEqual(2, FilingCabinet.Filed, "what was found was not kept");

            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            cabinet = Object.FindAnyObjectByType<FilingCabinet>(FindObjectsInactive.Include);
            Assert.IsTrue(cabinet.gameObject.activeInHierarchy);
            Vector3 at = cabinet.transform.position + new Vector3(0f, 0.05f, 1.5f);
            Assert.That(WorldPrompt.For(at), Does.Contain("2 of " + library.items.Length + " items on file").And.Contain("Lunchbox"));
            Assert.IsNull(WorldPrompt.For(new Vector3(0f, 0f, 1f)));
            Assert.That(WorldPrompt.For(at), Does.Contain("Needle Tip").And.Contain("pierce"));
        }

        [UnityTest]
        public IEnumerator Every_lab_station_reads_as_itself_when_stood_at()
        {
            // The whole lab at once: everything unlocked, an item on file, and Finch beaten.
            Playtest.UnlockEverything();
            SaveSystem.Data.meta.finchBeaten = 1;
            SaveSystem.Data.meta.endingSeen = true;
            SaveSystem.Data.meta.itemsFound.Add((int)ItemType.NeedleTip);
            SaveSystem.Save();
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;

            var stations = new List<KeyValuePair<Component, string>>();
            foreach (var pad in Object.FindObjectsByType<UpgradePad>(FindObjectsSortMode.None)) stations.Add(new KeyValuePair<Component, string>(pad, pad.upgrade.displayName));
            stations.Add(new KeyValuePair<Component, string>(Object.FindAnyObjectByType<LabPortal>(), "Portal"));
            stations.Add(new KeyValuePair<Component, string>(Object.FindAnyObjectByType<StyleMirror>(), "The mirror"));
            stations.Add(new KeyValuePair<Component, string>(Object.FindAnyObjectByType<TrophyBoard>(), "The record board"));
            stations.Add(new KeyValuePair<Component, string>(Object.FindAnyObjectByType<EvidenceBoard>(), "The evidence board"));
            stations.Add(new KeyValuePair<Component, string>(Object.FindAnyObjectByType<SpecimenShelf>(), "The specimen shelf"));
            stations.Add(new KeyValuePair<Component, string>(Object.FindAnyObjectByType<PowerRack>(), "The power rack"));
            stations.Add(new KeyValuePair<Component, string>(Object.FindAnyObjectByType<FilingCabinet>(), "The filing cabinet"));
            stations.Add(new KeyValuePair<Component, string>(Object.FindAnyObjectByType<HiddenDoor>(), "cat flap"));
            stations.Add(new KeyValuePair<Component, string>(Object.FindAnyObjectByType<TimeClock>(), "The time clock"));
            stations.Add(new KeyValuePair<Component, string>(Object.FindAnyObjectByType<WeaponRack>(), "The equipment cupboard"));
            stations.Add(new KeyValuePair<Component, string>(Object.FindAnyObjectByType<MachinePanel>(), "The machine"));
            Assert.AreEqual(System.Enum.GetValues(typeof(UpgradeType)).Length + 11, stations.Count);

            foreach (var station in stations)
            {
                Assert.IsNotNull(station.Key, "the lab is missing: " + station.Value);
                // Stood exactly at it, and a step to each side: it is always this one that speaks.
                foreach (var offset in new[] { Vector3.zero, new Vector3(0.6f, 0f, 0f), new Vector3(-0.6f, 0f, 0f), new Vector3(0f, 0f, 0.6f), new Vector3(0f, 0f, -0.6f) })
                {
                    string said = WorldPrompt.For(station.Key.transform.position + offset) ?? "";
                    Assert.That(said, Does.Contain(station.Value), $"standing at {station.Key.name} (offset {offset}) read something else");
                }
            }

            // And the middle of the room says nothing at all.
            Assert.IsNull(WorldPrompt.For(new Vector3(0f, 0f, 1f)));
        }

        [UnityTest]
        public IEnumerator Mimics_the_cabinet_and_overtime_each_have_an_achievement()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var library = GameAssets.Instance.items;
            var items = PlayerController.Instance.Items;

            // Ten mimics.
            SaveSystem.Data.meta.mimicsKilled = Achievements.MimicsNeeded - 1;
            Assert.IsFalse(Achievements.Has(AchievementId.LooseChange));
            var floors = FloorManager.Instance;
            Enemy mimicPrefab = null;
            foreach (var entry in floors.biome.spawnTable) if (entry.prefab is MimicEnemy) mimicPrefab = entry.prefab;
            Assert.IsNotNull(mimicPrefab, "the ants have no mimic");
            var mimic = Object.Instantiate(mimicPrefab, new Vector3(3f, 0.05f, 3f), Quaternion.identity);
            mimic.gameObject.SetActive(true);
            yield return null;
            mimic.Health.Kill();
            yield return null;
            Assert.IsTrue(Achievements.Has(AchievementId.LooseChange));

            // Every item on file: the cabinet is full and says so with a style.
            Assert.IsFalse(Achievements.StyleUnlocked("Paper"));
            foreach (var item in library.items) FilingCabinet.File(item);
            Pickup.Item(library.Get(ItemType.NeedleTip), PlayerController.Instance.transform.position, 0);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(Achievements.Has(AchievementId.FiledAway), "a full cabinet earned nothing");
            Assert.IsTrue(Achievements.StyleUnlocked("Paper"));

            // Extracting on overtime.
            Assert.IsFalse(Achievements.Has(AchievementId.TimeAndAHalf));
            SaveSystem.Data.meta.finchBeaten = 1;
            SaveSystem.Data.meta.overtime = true;
            RunDirector.Instance.Extract();
            yield return null;
            yield return null;
            Assert.IsTrue(Achievements.Has(AchievementId.TimeAndAHalf));
        }

        [UnityTest]
        public IEnumerator Beaten_bosses_end_up_in_jars_on_the_shelf()
        {
            SaveSystem.Data.meta.runsStarted = 3;
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var shelf = Object.FindAnyObjectByType<SpecimenShelf>(FindObjectsInactive.Include);
            Assert.IsNotNull(shelf, "the lab has no specimen shelf");
            Assert.IsFalse(shelf.gameObject.activeInHierarchy, "the shelf is there before any boss is beaten");
            Assert.AreEqual(Specimens.All.Length, shelf.jars.Length);

            // Beat a boss in a run: it is kept, once.
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var floors = FloorManager.Instance;
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            for (int round = 0; round < 2; round++)
            {
                floors.BuildFloor(5);
                yield return null;
                player.Teleport(floors.ArenaCentre + Vector3.up * 0.05f);
                yield return WaitUntil(() => floors.Boss != null, 5f, "the boss");
                floors.Boss.Health.Kill();
                yield return WaitUntil(() => !floors.IsLocked, 5f, "the arena to open");
            }
            Assert.AreEqual(1, Specimens.Count, "the same boss should fill one jar, however often it is beaten");
            Assert.IsTrue(Specimens.Has("BroodMother"));
            Assert.AreEqual(2, SaveSystem.Data.meta.bossesBeaten);
            SaveSystem.Data.meta.specimens.Add("TheCat");
            SaveSystem.Save();

            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            shelf = Object.FindAnyObjectByType<SpecimenShelf>(FindObjectsInactive.Include);
            Assert.IsTrue(shelf.gameObject.activeInHierarchy);
            Assert.IsTrue(shelf.jars[0].activeSelf && !shelf.jars[1].activeSelf && shelf.jars[shelf.jars.Length - 1].activeSelf);
            Vector3 at = shelf.transform.position + Vector3.up * 0.05f;
            Assert.That(WorldPrompt.For(at), Does.Contain("2 of " + Specimens.All.Length + " jars").And.Contain("Dr. Finch").And.Contain("do not tap the glass"));
            Assert.IsNull(WorldPrompt.For(at + Vector3.right * 5.5f + Vector3.forward * 6f));
            Assert.That(WorldPrompt.For(at), Does.Contain("Brood Mother"));
        }

        [UnityTest]
        public IEnumerator The_title_has_a_play_button_and_a_careful_erase_button()
        {
            SaveSystem.Data.meta.runsStarted = 6;
            SaveSystem.Data.meta.samples = 40;
            SaveSystem.Data.run.active = true;
            SaveSystem.Data.run.floorNumber = 4;
            SaveSystem.Save();
            ScreenCards.ShowInTests = true;
            ScreenCards.TitleSeen = false;
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var cards = ScreenCards.Instance;
            Assert.IsTrue(cards.TitleShowing);
            Assert.That(cards.PlayLabel, Does.Contain("CONTINUE").And.Contain("floor 4"));

            // One press only asks; it forgets it was asked after a few seconds.
            cards.PressErase();
            Assert.IsTrue(cards.EraseArmed);
            Assert.That(cards.EraseLabel, Does.Contain("really"));
            Assert.AreEqual(40, SaveSystem.Data.meta.samples, "one press erased the save");
            yield return new WaitForSecondsRealtime(4.3f);
            Assert.IsFalse(cards.EraseArmed);
            Assert.AreEqual("erase save", cards.EraseLabel);

            // Two presses mean it: everything goes, and the game opens again as new.
            cards.PressErase();
            cards.PressErase();
            yield return null;
            yield return null;
            cards = ScreenCards.Instance;
            Assert.AreEqual(0, SaveSystem.Data.meta.runsStarted);
            Assert.IsFalse(SaveSystem.Data.run.active);
            Assert.IsTrue(cards.TitleShowing, "the title did not come back after erasing");
            Assert.AreEqual("START", cards.PlayLabel);
            cards.DismissTitle();
            Assert.IsFalse(ScreenCards.Blocking);
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
