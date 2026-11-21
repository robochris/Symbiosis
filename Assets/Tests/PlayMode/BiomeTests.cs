using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The second biome and the choice of portals after a biome boss.</summary>
    public class BiomeTests
    {
        PlayerController _player;
        FloorManager _floors;

        [UnitySetUp]
        public IEnumerator LoadRun()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
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
            SaveSystem.Wipe();
            Time.timeScale = 1f;
            Achievements.EverythingUnlocked = false;
            Progress.LevelOverride = Progress.TopLevel;
        }

        BiomeDefinition Cake
        {
            get
            {
                foreach (var biome in _floors.AllBiomes()) if (biome.id == "cake") return biome;
                Assert.Fail("the Run scene has no Birthday Cake biome");
                return null;
            }
        }

        IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.deltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        IEnumerator KillTheBoss()
        {
            Room arena = _floors.RoomAt(_floors.Layout.exit.cell);
            _player.Teleport(arena.Centre + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.Boss != null, 2f, "the boss to appear");
            _floors.Boss.Health.Kill();
            yield return WaitUntil(() => !_floors.IsLocked, 2f, "the arena to open");
            // Clear of where the portals appear: a pad only counts a step onto it from outside.
            _player.Teleport(_floors.ArenaCentre + new Vector3(0f, 0.05f, -9f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator The_sofa_opens_with_the_candle_and_has_its_own_bosses()
        {
            Assert.IsFalse(Achievements.BiomeUnlocked("sofa"), "the sofa is open to a new player");
            Achievements.Unlock(AchievementId.SweetTooth);
            Assert.IsTrue(Achievements.BiomeUnlocked("sofa"));

            Assert.IsTrue(_floors.SetBiome("sofa"));
            _floors.BuildFloor(5);
            yield return null;
            Assert.IsTrue(RenderSettings.fog, "under the sofa should be dark and hazy");
            _player.Teleport(_floors.RoomAt(_floors.Layout.exit.cell).Centre + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.Boss != null, 2f, "the mini boss");
            Assert.AreEqual("Grandmother Dust", _floors.Boss.displayName);
            yield return new WaitForSeconds(5f);
            Assert.Greater(_floors.LiveEnemies.Count, 1, "Grandmother Dust shed nothing");
            _floors.Boss.Health.Kill();
            yield return WaitUntil(() => !_floors.IsLocked, 2f, "the arena to open");

            _floors.BuildFloor(10);
            yield return null;
            _player.Health.SetInvulnerable(600f);
            _player.Teleport(_floors.RoomAt(_floors.Layout.exit.cell).Centre + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.Boss != null, 2f, "the boss");
            Assert.AreEqual("The Robot Vacuum", _floors.Boss.displayName);
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(EggEnemy.All.Count, 0, "the Robot Vacuum has no lint piles");
            _floors.Boss.Health.Kill();
            yield return WaitUntil(() => !_floors.IsLocked, 2f, "the arena to open");
            Assert.AreEqual("sofa", _floors.Portals[0].leadsTo.id);

            // A fight room fills with the sofa's own.
            _floors.BuildFloor(4);
            yield return null;
            _player.Health.SetInvulnerable(600f);
            foreach (var node in _floors.Layout.ordered)
            {
                if (node.type != RoomType.Normal) continue;
                _player.Teleport(_floors.RoomAt(node.cell).Centre + new Vector3(RoomShape.TileSize, 0.05f, 0f));
                yield return WaitUntil(() => _floors.IsLocked, 2f, "a room to lock");
                break;
            }
            Assert.Greater(_floors.LiveEnemies.Count, 0);
            var names = new HashSet<string> { "DustBunny", "Marble", "ToySoldier", "Moth", "Popcorn", "RemoteControl", "Mimic" };
            foreach (var enemy in _floors.LiveEnemies) Assert.IsTrue(names.Contains(enemy.name.Replace("(Clone)", "")), enemy.name + " does not live under the sofa");
        }

        [UnityTest]
        public IEnumerator The_fridge_opens_with_the_vacuum()
        {
            Assert.IsFalse(Achievements.BiomeUnlocked("fridge"), "the fridge is open to a new player");
            Achievements.Unlock(AchievementId.SpringClean);
            Assert.IsTrue(Achievements.BiomeUnlocked("fridge"));
            Assert.IsTrue(_floors.SetBiome("fridge"));
            _floors.BuildFloor(4);
            yield return null;
            Assert.AreEqual("The Fridge", _floors.biome.displayName);
            foreach (var node in _floors.Layout.ordered)
            {
                if (node.type != RoomType.Normal) continue;
                _player.Teleport(_floors.RoomAt(node.cell).Centre + new Vector3(RoomShape.TileSize, 0.05f, 0f));
                yield return WaitUntil(() => _floors.IsLocked, 2f, "a room to lock");
                break;
            }
            Assert.Greater(_floors.LiveEnemies.Count, 0);
            var names = new HashSet<string> { "MouldyBerry", "FrozenPea", "KetchupPacket", "FruitFly", "YogurtPot", "BakingSoda", "Mimic" };
            foreach (var enemy in _floors.LiveEnemies) Assert.IsTrue(names.Contains(enemy.name.Replace("(Clone)", "")), enemy.name + " does not live in the fridge");
        }

        [UnityTest]
        public IEnumerator Every_biome_is_complete()
        {
            var ids = new HashSet<string>();
            foreach (var biome in _floors.AllBiomes())
            {
                Assert.IsTrue(ids.Add(biome.id), "two biomes share the id " + biome.id);
                Assert.IsNotNull(biome.floorMaterial, biome.displayName + ": floor");
                Assert.IsNotNull(biome.wallMaterial, biome.displayName + ": walls");
                Assert.IsNotNull(biome.miniBoss, biome.displayName + ": mini boss");
                Assert.IsNotNull(biome.boss, biome.displayName + ": boss");
                Assert.IsNotNull(biome.swarmEnemy, biome.displayName + ": swarm enemy");
                Assert.IsNotNull(biome.portalProp, biome.displayName + ": its own portal");
                Assert.IsNotNull(biome.startProp);
                Assert.IsNotNull(biome.exitProp);
                Assert.GreaterOrEqual(biome.spawnTable.Length, 4, biome.displayName + ": one enemy per behaviour");
                Assert.GreaterOrEqual(biome.mutations.Length, 6, biome.displayName + ": mutations");
                foreach (var mutation in biome.mutations) Assert.IsNotNull(mutation, biome.displayName + " has an empty mutation slot");
                var kinds = new HashSet<System.Type>();
                foreach (var entry in biome.spawnTable)
                {
                    Assert.IsNotNull(entry.prefab);
                    Assert.IsNotNull(entry.prefab.GetComponent<EnemyMutations>(), entry.prefab.name + " cannot carry mutations");
                    kinds.Add(entry.prefab.GetType());
                }
                // Seven everywhere; some worlds also have the eighth, the one that paints a line and fires along it.
                kinds.Remove(typeof(SniperEnemy));
                Assert.AreEqual(7, kinds.Count, biome.displayName + " should have a chaser, a charger, a shooter, a flyer, a lobber, a support and a mimic");
            }
            Assert.GreaterOrEqual(ids.Count, 2);
            yield break;
        }

        [UnityTest]
        public IEnumerator The_portal_room_offers_home_this_biome_again_or_another_biome()
        {
            _floors.BuildFloor(10);
            yield return null;
            BiomeDefinition ants = _floors.biome;
            Assert.AreEqual("ants", ants.id);
            Assert.AreEqual(new Color(0.08f, 0.06f, 0.05f), Camera.main.backgroundColor, "the ant colony should be dark");
            yield return KillTheBoss();

            Assert.IsNotNull(_floors.ExtractPortal);
            Assert.AreEqual(2, _floors.Portals.Count, "this biome again, and the cake");
            Assert.AreEqual(ants, _floors.Portals[0].leadsTo, "the first portal is always this biome again");
            Assert.AreEqual(Cake, _floors.Portals[1].leadsTo);
            Assert.AreNotEqual(_floors.Portals[0].gate.name, _floors.Portals[1].gate.name, "the two portals look the same");

            // No two gates on top of each other, and all inside the arena.
            var gates = new List<Vector3> { _floors.ExtractPortal.transform.position };
            foreach (var portal in _floors.Portals) gates.Add(portal.gate.transform.position);
            for (int i = 0; i < gates.Count; i++)
                for (int j = i + 1; j < gates.Count; j++)
                    Assert.Greater((gates[i] - gates[j]).magnitude, 5f, "two portals overlap");

            string prompt = WorldPrompt.For(_floors.Portals[1].gate.transform.position + Vector3.back * 2f);
            Assert.That(prompt, Does.Contain("Birthday Cake").And.Contain("sticky").And.Contain("depth 2"));

            // Into the cake.
            Vector3 gate = _floors.Portals[1].gate.transform.position;
            _player.Teleport(new Vector3(gate.x, 0.05f, gate.z));
            yield return WaitUntil(() => _floors.FloorNumber == 11, 2f, "the next biome");
            yield return null;
            Assert.AreEqual(Cake, _floors.biome);
            Assert.AreEqual(2, _floors.depth);
            Assert.AreEqual(Cake.skyColor, Camera.main.backgroundColor, "the sky did not change");
            Assert.IsFalse(RenderSettings.fog, "no haze out of doors");
            Assert.AreEqual("cake", SaveSystem.Data.run.biome, "the save does not know which biome the run is in");

            // Open to the sky: no ceiling on any room.
            foreach (var node in _floors.Layout.ordered)
            {
                Room room = _floors.RoomAt(node.cell);
                Assert.AreEqual(2, room.GetComponent<MeshFilter>().sharedMesh.subMeshCount, "a roofed room in the cake");
                Assert.AreEqual(Cake.floorMaterial, room.GetComponent<MeshRenderer>().sharedMaterials[0]);
            }

            // A fight room there is full of candy, not ants.
            RoomNode fight = null;
            foreach (var node in _floors.Layout.ordered) if (node.type == RoomType.Normal) { fight = node; break; }
            _player.Health.SetInvulnerable(600f);
            _player.Teleport(_floors.RoomAt(fight.cell).Centre + new Vector3(RoomShape.TileSize, 0.05f, 0f));
            yield return WaitUntil(() => _floors.IsLocked, 2f, "the room to lock");
            foreach (var enemy in _floors.LiveEnemies)
            {
                Assert.IsNotNull(Cake.EnemyNamed(enemy.name.Replace("(Clone)", "").Trim()), enemy.name + " does not belong in the cake");
            }
        }

        [UnityTest]
        public IEnumerator A_saved_run_comes_back_in_the_biome_it_was_in()
        {
            Assert.IsTrue(_floors.SetBiome("cake"));
            _floors.depth = 2;
            _floors.BuildFloor(13);
            yield return null;
            Assert.AreEqual("cake", SaveSystem.Data.run.biome);
            Assert.IsTrue(SaveSystem.Data.run.active);
            int seed = _floors.Layout.seed;

            // Close the game and open it again.
            SaveSystem.Save();
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var floors = FloorManager.Instance;
            Assert.AreEqual("cake", floors.biome.id, "the run came back in the wrong biome");
            Assert.AreEqual(13, floors.FloorNumber);
            Assert.AreEqual(2, floors.depth);
            Assert.AreEqual(seed, floors.Layout.seed);
            Assert.IsFalse(floors.SetBiome("no-such-biome"));
            Assert.AreEqual("cake", floors.biome.id);
        }

        [UnityTest]
        public IEnumerator Cake_bosses_appear_on_their_floors_under_their_own_names()
        {
            _floors.SetBiome("cake");
            _floors.BuildFloor(5);
            yield return null;
            _player.Teleport(_floors.RoomAt(_floors.Layout.exit.cell).Centre + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.Boss != null, 2f, "the mini boss");
            Assert.AreEqual("Jelly Mother", _floors.Boss.displayName);
            yield return new WaitForSeconds(5f);
            Assert.Greater(_floors.LiveEnemies.Count, 1, "the Jelly Mother brought nothing with her");
            _floors.Boss.Health.Kill();
            yield return WaitUntil(() => !_floors.IsLocked, 2f, "the arena to open");

            _floors.BuildFloor(10);
            yield return null;
            _player.Health.SetInvulnerable(600f);
            _player.Teleport(_floors.RoomAt(_floors.Layout.exit.cell).Centre + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.Boss != null, 2f, "the boss");
            Assert.AreEqual("The Trick Candle", _floors.Boss.displayName);
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(EggEnemy.All.Count, 0, "the Trick Candle has no sparkler boxes");
            _floors.Boss.Health.Kill();
            yield return WaitUntil(() => !_floors.IsLocked, 2f, "the arena to open");
            // The cake again, the way back to the ants, and (the candle being what opens it) the way under the sofa.
            Assert.AreEqual(3, _floors.Portals.Count);
            var leads = new List<string>();
            foreach (var portal in _floors.Portals) leads.Add(portal.leadsTo.id);
            Assert.AreEqual("cake", leads[0]);
            CollectionAssert.Contains(leads, "ants", "from the cake, a portal leads back to the ants");
            CollectionAssert.Contains(leads, "sofa", "beating the candle did not open the way under the sofa");
        }

        [UnityTest]
        public IEnumerator The_Trick_Candle_goes_out_once_then_relights_with_health_back()
        {
            _floors.SetBiome("cake");
            _floors.BuildFloor(10);
            yield return null;
            _player.Health.SetInvulnerable(600f);
            _player.Teleport(_floors.RoomAt(_floors.Layout.exit.cell).Centre + new Vector3(0f, 0.05f, -9f));
            yield return WaitUntil(() => _floors.Boss != null, 2f, "the boss");
            var candle = _floors.Boss as TrickCandle;
            Assert.IsNotNull(candle, "the cake's boss is not the Trick Candle");
            Assert.IsNotNull(candle.flame);
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(candle.IsOut);

            int max = candle.Health.maxHealth;
            candle.Health.TakeDamage(max - Mathf.RoundToInt(max * 0.3f), Vector3.forward);
            yield return null;
            yield return null;
            Assert.IsTrue(candle.IsOut, "it did not go out at low health");
            Assert.IsFalse(candle.flame.gameObject.activeSelf, "the flame is still burning");
            Vector3 stood = candle.transform.position;

            yield return new WaitForSeconds(candle.outTime + 0.3f);
            Assert.IsFalse(candle.IsOut);
            Assert.IsTrue(candle.flame.gameObject.activeSelf, "it did not relight");
            Assert.AreEqual(candle.relightsTo, candle.Health01, 0.02f, "relighting should bring its health back");
            Assert.Less((candle.transform.position - stood).magnitude, 0.5f, "it moved while it was out");

            // Only the once.
            candle.Health.TakeDamage(candle.Health.Current - Mathf.RoundToInt(max * 0.2f), Vector3.forward);
            yield return null;
            yield return null;
            Assert.IsFalse(candle.IsOut, "it used its trick twice");

            int beaten = SaveSystem.Data.meta.bossesBeaten;
            candle.Health.Kill();
            yield return WaitUntil(() => !_floors.IsLocked, 2f, "the arena to open");
            Assert.AreEqual(beaten + 1, SaveSystem.Data.meta.bossesBeaten, "the record board was not told");
        }

        [UnityTest]
        public IEnumerator Achievements_unlock_the_cake_items_and_styles()
        {
            var library = GameAssets.Instance.items;
            var style = _player.GetComponent<PlayerStyle>();
            int announced = 0;
            System.Action<Achievement> count = achievement => announced++;
            Achievements.Unlocked += count;

            // A new player, at level 1: one style, the items neither an achievement nor a level holds back, no cake.
            Progress.LevelOverride = 1;
            int onLadder = 0;
            foreach (var reward in Progress.Ladder) if (reward.kind == RewardKind.Item) onLadder++;
            Assert.AreEqual(0, Achievements.Count);
            Assert.AreEqual(1, style.UnlockedCount);
            Assert.IsFalse(Achievements.BiomeUnlocked("cake"));
            var rng = new System.Random(3);
            var seen = new HashSet<ItemType>();
            for (int i = 0; i < 300; i++) foreach (var item in library.RandomCommon(rng, 3)) seen.Add(item.type);
            Assert.AreEqual(23 - onLadder, seen.Count, "a new player should find 23 common items, less the ones levels hold back");
            Assert.IsFalse(seen.Contains(ItemType.LarvaBuddy));
            style.Next();
            Assert.AreEqual(0, style.Current, "a locked style was put on");

            // Clear floor 1 without a scratch: two at once, and it says so.
            foreach (var node in _floors.Layout.ordered)
            {
                if (node.type != RoomType.Normal && node.type != RoomType.Exit) continue;
                _player.Teleport(_floors.RoomAt(node.cell).Centre + new Vector3(RoomShape.TileSize, 0.05f, 0f));
                yield return WaitUntil(() => _floors.IsLocked, 2f, "a room to lock");
                foreach (var enemy in new List<Enemy>(_floors.LiveEnemies)) enemy.Health.Kill();
                yield return WaitUntil(() => !_floors.IsLocked, 2f, "the room to open");
            }
            Assert.IsTrue(_floors.ExitOpen);
            yield return null;
            Assert.IsTrue(Achievements.Has(AchievementId.FirstSteps), "clearing floor 1 earned nothing");
            Assert.IsTrue(Achievements.Has(AchievementId.Untouchable));
            // Killing a whole floor at a stroke, as this does, also runs up a streak nobody could miss.
            int expected = Achievements.Has(AchievementId.OnARoll) ? 3 : 2;
            Assert.AreEqual(expected, announced, "each achievement should be announced once");
            Achievements.Unlocked -= count;
            Assert.AreEqual(3, style.UnlockedCount, "Toy and Ghost should be unlocked now");
            style.Next();
            Assert.AreEqual("Toy", style.CurrentStyle.displayName);

            // The Brood Mother puts Larva Buddy into the pool.
            _floors.BuildFloor(5);
            yield return null;
            _player.Health.SetInvulnerable(600f);
            yield return KillTheBoss();
            Assert.IsTrue(Achievements.Has(AchievementId.MothersDay));
            Assert.IsTrue(Achievements.ItemUnlocked(ItemType.LarvaBuddy));

            // The Queen, with every egg left alone: Regicide and Hands Off, and the cake's portal is there at once.
            _floors.BuildFloor(10);
            yield return null;
            _player.Health.SetInvulnerable(600f);
            _player.Teleport(_floors.ArenaCentre + new Vector3(0f, 0.05f, -9f));
            yield return WaitUntil(() => _floors.Boss != null, 2f, "the boss");
            yield return new WaitForSeconds(0.3f);
            _floors.Boss.Health.Kill();
            yield return WaitUntil(() => !_floors.IsLocked, 2f, "the arena to open");
            Assert.IsTrue(Achievements.Has(AchievementId.Regicide));
            Assert.IsTrue(Achievements.Has(AchievementId.HandsOff));
            Assert.IsFalse(Achievements.Has(AchievementId.Omelette));
            Assert.AreEqual(2, _floors.Portals.Count, "the cake portal should appear the moment it is unlocked");

            // Counted ones: shells and pods add up across runs.
            for (int i = 0; i < Achievements.PodsNeeded; i++) SaveSystem.Data.meta.podsPopped = i;
            Assert.IsFalse(Achievements.Has(AchievementId.BubbleWrap));
            Assert.IsFalse(Achievements.Unlock(AchievementId.Regicide), "an achievement was earned twice");

            // It is all in the save file.
            int earned = Achievements.Count;
            SaveSystem.Save();
            SaveSystem.Load();
            Assert.AreEqual(earned, Achievements.Count);
        }

        [UnityTest]
        public IEnumerator Without_Regicide_the_portal_room_does_not_offer_the_cake()
        {
            SaveSystem.Data.meta.achievements.Add((int)AchievementId.Regicide); // so killing her here earns nothing new
            SaveSystem.Data.meta.achievements.Clear();
            _floors.SetBiome("cake");
            _floors.BuildFloor(10);
            yield return null;
            yield return KillTheBoss();
            // From the cake, the way back to the ants is always there: the ants need no unlocking.
            // The candle's death has just opened the sofa, so that is there too. The cake itself is "this biome again".
            Assert.AreEqual(3, _floors.Portals.Count);
            Assert.IsTrue(Achievements.Has(AchievementId.SweetTooth));
        }

        [UnityTest]
        public IEnumerator Enemies_mostly_drop_their_own_kind_of_item()
        {
            var library = GameAssets.Instance.items;
            Achievements.EverythingUnlocked = true; // with every item in the pool
            foreach (var biome in _floors.AllBiomes())
            {
                foreach (var entry in biome.spawnTable)
                {
                    Enemy enemy = entry.prefab;
                    Assert.IsTrue(enemy.favouredDrops != null && enemy.favouredDrops.Length > 0, enemy.name + " favours no item");
                    var favoured = new HashSet<ItemType>(enemy.favouredDrops);
                    var rng = new System.Random(7);
                    int own = 0;
                    for (int i = 0; i < 400; i++)
                    {
                        ItemDefinition item = Loot.PickFor(enemy, library, rng);
                        Assert.IsNotNull(item);
                        if (favoured.Contains(item.type)) own++;
                    }
                    Assert.That(own, Is.InRange(240, 330), $"{enemy.name}: {own} of 400 drops were its own kind");
                }
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator Sticky_ants_leave_a_patch_that_slows_and_sugar_rush_ants_speed_up_when_hurt()
        {
            _floors.SetBiome("cake");
            _floors.BuildFloor(3);
            yield return null;
            RoomNode fight = null;
            foreach (var node in _floors.Layout.ordered) if (node.type == RoomType.Normal) { fight = node; break; }
            Room room = _floors.RoomAt(fight.cell);
            _player.Teleport(room.Centre + new Vector3(RoomShape.TileSize, 0.05f, 0f));
            yield return WaitUntil(() => _floors.IsLocked, 2f, "the room to lock");

            // Sugar rush: nothing until half health, then much faster.
            Enemy bear = _floors.AddToFight(Cake.swarmEnemy, room.Centre + new Vector3(0f, 0f, 3f), false);
            bear.frozen = true;
            bear.Mutations.Apply(new[] { Cake.Mutation(MutationType.SugarRush) });
            Assert.IsFalse(bear.Mutations.IsRushing);
            Assert.AreEqual(1f, bear.SpeedMultiplier);
            bear.Health.TakeDamage(bear.Health.maxHealth / 2 + 1, Vector3.forward);
            Assert.IsTrue(bear.Mutations.IsRushing);
            Assert.AreEqual(EnemyMutations.SugarRushSpeed, bear.SpeedMultiplier, 0.001f);
            bear.Health.TakeDamage(1, Vector3.forward);
            Assert.AreEqual(EnemyMutations.SugarRushSpeed, bear.SpeedMultiplier, 0.001f, "the rush stacked");

            // Sticky: dies, leaves a patch, and the player standing in it is slowed without being hurt.
            Enemy sticky = _floors.AddToFight(Cake.swarmEnemy, room.Centre + new Vector3(0f, 0f, -3f), false);
            sticky.frozen = true;
            sticky.Mutations.Apply(new[] { Cake.Mutation(MutationType.Sticky) });
            Vector3 where = sticky.transform.position;
            int hazards = GroundHazard.All.Count;
            sticky.Health.Kill();
            Assert.AreEqual(hazards + 1, GroundHazard.All.Count, "no sticky patch");

            foreach (var enemy in new List<Enemy>(_floors.LiveEnemies)) enemy.Health.Kill();
            _player.Health.ClearInvulnerability();
            int health = _player.Health.Current;
            Assert.IsFalse(_player.IsSlowed);
            _player.Teleport(new Vector3(where.x, 0.05f, where.z));
            yield return null;
            yield return null;
            Assert.IsTrue(_player.IsSlowed, "standing in a sticky patch did nothing");
            Assert.GreaterOrEqual(_player.Health.Current, health, "a sticky patch should not hurt");

            _player.Teleport(room.Centre + Vector3.up * 0.05f);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(_player.IsSlowed, "still slowed after walking out of it");
        }
    }
}
