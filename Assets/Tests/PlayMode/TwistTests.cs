using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The things that make one floor different from the last: twists, and the dare bell.</summary>
    public class TwistTests
    {
        PlayerController _player;
        FloorManager _floors;

        [UnitySetUp]
        public IEnumerator LoadRun()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            Radio.Forget();
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            _player = PlayerController.Instance;
            _floors = FloorManager.Instance;
            _player.Input.overrideFrame = new InputFrame();
            _player.Health.SetInvulnerable(600f);
        }

        [TearDown]
        public void CleanUp() => SaveSystem.Wipe();

        Vector3 Beside(RoomNode node)
        {
            Vector3 near = _floors.RoomAt(node.cell).Centre + new Vector3(RoomShape.TileSize, 0f, 0f);
            Assert.IsTrue(NavMesh.SamplePosition(near, out var hit, 2f, NavMesh.AllAreas));
            return hit.position + Vector3.up * 0.05f;
        }

        RoomNode FightRoom(int skip = 0)
        {
            foreach (var node in _floors.Layout.ordered)
            {
                if (node.type != RoomType.Normal || node.cleared || _floors.Layout.InArena(node.cell)) continue;
                if (skip-- > 0) continue;
                return node;
            }
            Assert.Fail("no fight room on this floor");
            return null;
        }

        // Builds floors until one has a dare room: not every layout has a third dead end to put it in.
        IEnumerator BuildFloorWithDare(int floor)
        {
            for (int seed = 1; seed <= 60; seed++)
            {
                _floors.BuildFloor(floor, seed);
                yield return null;
                if (_floors.Dare != null) yield break;
            }
            Assert.Fail("sixty floors and not one dare room");
        }

        IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.deltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        IEnumerator EnterFight(RoomNode node)
        {
            _player.Teleport(Beside(node));
            yield return WaitUntil(() => _floors.IsLocked, 2f, "the doors to shut");
        }

        IEnumerator KillAll()
        {
            for (int round = 0; round < 40 && _floors.IsLocked; round++)
            {
                foreach (var enemy in new List<Enemy>(_floors.LiveEnemies))
                {
                    if (enemy.gameObject.activeSelf && !enemy.Health.IsDead) enemy.Health.Kill();
                }
                yield return null;
            }
            Assert.IsFalse(_floors.IsLocked, "the room would not clear");
        }

        [UnityTest]
        public IEnumerator A_world_is_six_floors_with_its_bosses_on_the_third_and_sixth()
        {
            Assert.AreEqual(10, FloorLayout.FloorsPerBiome, "the tests are not running at ten floors a world");
            FloorLayout.FloorsPerBiome = 6;
            try
            {
                // The six floors stand for the first, third, fifth, seventh, ninth and tenth of the old ten.
                int[] stages = { 1, 3, 5, 7, 9, 10 };
                for (int floor = 1; floor <= 18; floor++)
                {
                    Assert.AreEqual((floor - 1) % 6 + 1, FloorLayout.FloorOfBiome(floor));
                    Assert.AreEqual(stages[(floor - 1) % 6], FloorLayout.FloorInBiome(floor));
                    Assert.AreEqual(floor % 3 == 0, FloorLayout.IsBossFloor(floor), "floor " + floor);
                }
                Assert.AreEqual(5, FloorLayout.RoomCountForFloor(3), "boss floors are short");
                Assert.Greater(FloorLayout.RoomCountForFloor(5), FloorLayout.RoomCountForFloor(1));

                // Everything a world has turns up before its boss: the last enemy to join is there on floor five.
                int latest = 0;
                foreach (var entry in _floors.biome.spawnTable) latest = Mathf.Max(latest, entry.minFloor);
                Assert.LessOrEqual(latest, FloorLayout.FloorInBiome(5), "an enemy of this world never gets a floor to appear on");

                // Third floor: the mini boss. Sixth: the world's boss, and the portals. Seventh: the next world's first.
                _floors.BuildFloor(2);
                yield return null;
                Assert.AreEqual("FLOOR 2", ScreenCards.Instance.LastHeading);
                _floors.BuildFloor(3);
                yield return null;
                _player.Teleport(_floors.ArenaCentre + new Vector3(0f, 0.05f, -8f));
                yield return WaitUntil(() => _floors.Boss != null, 3f, "the mini boss");
                Assert.AreEqual(_floors.biome.miniBoss.displayName, _floors.Boss.displayName);
                for (float t = 0f; t < 8f && Time.timeScale <= 0f; t += Time.unscaledDeltaTime) yield return null;

                _floors.BuildFloor(6);
                yield return null;
                _player.Teleport(_floors.ArenaCentre + new Vector3(0f, 0.05f, -8f));
                yield return WaitUntil(() => _floors.Boss != null, 3f, "the boss");
                Assert.AreEqual(_floors.biome.boss.displayName, _floors.Boss.displayName);
                for (float t = 0f; t < 8f && Time.timeScale <= 0f; t += Time.unscaledDeltaTime) yield return null;
                _floors.Boss.Health.Kill();
                for (float t = 0f; t < 5f && _floors.ExtractPortal == null; t += Time.unscaledDeltaTime) yield return null;
                Assert.IsNotNull(_floors.ExtractPortal, "no way home after the sixth floor's boss");
                Assert.IsNotNull(_floors.DeeperPortal, "no way on after the sixth floor's boss");
                Assert.AreEqual(1, MachineParts.Count, "the world's boss kept its part");

                _floors.BuildFloor(7);
                yield return null;
                Assert.AreEqual(1, _floors.FloorInBiome);
                Assert.IsFalse(FloorLayout.IsBossFloor(7));

                // The two places with no floors on the way still open straight onto their boss.
                _floors.BuildBottom();
                yield return null;
                _player.Teleport(_floors.ArenaCentre + new Vector3(0f, 0.05f, -8f));
                yield return WaitUntil(() => _floors.Boss is CatBoss, 3f, "the cat");
                for (float t = 0f; t < 8f && Time.timeScale <= 0f; t += Time.unscaledDeltaTime) yield return null;
                _floors.BuildBottom(true);
                yield return null;
                _player.Teleport(_floors.ArenaCentre + new Vector3(0f, 0.05f, -12f));
                yield return WaitUntil(() => _floors.Boss is MachineBoss, 3f, "the machine");
                for (float t = 0f; t < 8f && Time.timeScale <= 0f; t += Time.unscaledDeltaTime) yield return null;
            }
            finally
            {
                FloorLayout.FloorsPerBiome = 10;
            }
        }

        [Test]
        public void Enough_floors_have_a_dare_room()
        {
            foreach (int floor in new[] { 2, 4, 8 })
            {
                int with = 0;
                for (int seed = 1; seed <= 1000; seed++)
                {
                    var layout = FloorLayout.Generate(seed * 31 + floor, FloorLayout.RoomCountForFloor(floor), false);
                    Assert.IsEmpty(layout.Validate(), "seed " + seed);
                    foreach (var node in layout.ordered) if (node.type == RoomType.Dare) with++;
                }
                Debug.Log($"[Shrunk] DARE ROOMS floor {floor}: {with / 10f:F0}% of floors have one");
                Assert.Greater(with, 900, $"too few floors at floor {floor} have a dare room");
            }
        }

        [Test]
        public void Twists_only_turn_up_where_they_are_allowed()
        {
            Assert.AreEqual(0f, FloorManager.TwistChanceForAll, "the tests are running with random twists on");
            Assert.AreEqual(0f, FloorManager.ExchangeChanceForAll, "the tests are running with random exchanges on");
            var biome = _floors.biome;
            for (int seed = 1; seed < 200; seed++)
            {
                Assert.AreEqual(FloorTwist.None, FloorTwists.Roll(seed, 1, biome, 1f));
                Assert.AreEqual(FloorTwist.None, FloorTwists.Roll(seed, 2, biome, 1f));
                Assert.AreEqual(FloorTwist.None, FloorTwists.Roll(seed, 5, biome, 1f), "a mini-boss floor got a twist");
                Assert.AreEqual(FloorTwist.None, FloorTwists.Roll(seed, 10, biome, 1f), "a boss floor got a twist");
                Assert.AreEqual(FloorTwist.None, FloorTwists.Roll(seed, 11, biome, 1f), "a biome's first floor got a twist");
                Assert.AreEqual(FloorTwist.None, FloorTwists.Roll(seed, 4, biome, 0f));
                Assert.AreNotEqual(FloorTwist.None, FloorTwists.Roll(seed, 4, biome, 1f));
                Assert.AreEqual(FloorTwists.Roll(seed, 7, biome, 0.5f), FloorTwists.Roll(seed, 7, biome, 0.5f), "the same floor came back different");
            }

            var seen = new HashSet<FloorTwist>();
            int twisted = 0;
            for (int seed = 1; seed <= 1000; seed++)
            {
                FloorTwist twist = FloorTwists.Roll(seed * 7919, 4, biome, 0.3f);
                if (twist != FloorTwist.None) twisted++;
                seen.Add(twist);
            }
            Assert.AreEqual(5, seen.Count, "some twist never turns up");
            Assert.That(twisted, Is.InRange(220, 380), "about three floors in ten should have one");
            foreach (var twist in FloorTwists.All)
            {
                Assert.IsNotEmpty(FloorTwists.Name(twist));
                Assert.IsNotEmpty(FloorTwists.Line(twist));
            }
        }

        [UnityTest]
        public IEnumerator A_crowded_floor_has_more_in_every_room_and_pays_for_it()
        {
            _floors.twistOverride = FloorTwist.Crowded;
            _floors.BuildFloor(4);
            yield return null;
            Assert.AreEqual(FloorTwist.Crowded, _floors.Twist);
            Assert.AreEqual(FloorTwists.CrowdedCoins, Loot.CoinBonus);

            var hud = Object.FindAnyObjectByType<Hud>();
            Assert.That(ScreenCards.Instance.LastLine, Does.StartWith("Crowded: "));
            yield return null;
            Assert.That(hud.floorText.text, Does.Contain("Crowded"));

            RoomNode node = FightRoom();
            yield return EnterFight(node);
            int usual = _floors.biome.EnemiesFor(_floors.FloorInBiome, _floors.RoomAt(node.cell).Shape.interior.Count);
            Assert.GreaterOrEqual(_floors.LiveEnemies.Count, usual + FloorTwists.CrowdedExtra);

            int coins = 0;
            yield return KillAll();
            foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Coin) coins += pickup.amount;
            Assert.GreaterOrEqual(coins + RunState.Coins, (usual + FloorTwists.CrowdedExtra) * (1 + FloorTwists.CrowdedCoins), "the crowd did not bring its wallets");

            // The next floor is ordinary again.
            _floors.twistOverride = FloorTwist.None;
            _floors.BuildFloor(5);
            yield return null;
            Assert.AreEqual(0, Loot.CoinBonus);
        }

        [UnityTest]
        public IEnumerator Promotions_put_an_elite_in_every_room()
        {
            _floors.twistOverride = FloorTwist.Promotions;
            _floors.eliteChanceOverride = 0f;
            _floors.BuildFloor(3);
            yield return null;

            for (int i = 0; i < 2; i++)
            {
                yield return EnterFight(FightRoom());
                int elites = 0;
                foreach (var enemy in _floors.LiveEnemies) if (enemy.IsElite) elites++;
                Assert.AreEqual(1, elites, "a room without its manager");
                yield return KillAll();
            }
        }

        [UnityTest]
        public IEnumerator A_power_cut_is_dark_and_worth_more()
        {
            _floors.twistOverride = FloorTwist.None;
            _floors.BuildFloor(3);
            yield return null;
            float lit = RenderSettings.ambientLight.grayscale;

            _floors.twistOverride = FloorTwist.PowerCut;
            _floors.BuildFloor(4);
            yield return null;
            Assert.Less(RenderSettings.ambientLight.grayscale, lit * 0.6f, "the light is still on");
            Assert.AreEqual(FloorTwists.PowerCutSamples, Loot.SampleBonus);

            yield return EnterFight(FightRoom());
            int enemies = _floors.LiveEnemies.Count;
            yield return KillAll();
            int samples = 0;
            foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Sample) samples += pickup.amount;
            Assert.GreaterOrEqual(samples + RunState.Samples, enemies * (Loot.SamplesPerEnemy + FloorTwists.PowerCutSamples));

            _floors.twistOverride = FloorTwist.None;
            _floors.BuildFloor(6);
            yield return null;
            Assert.AreEqual(lit, RenderSettings.ambientLight.grayscale, 0.001f, "the light never came back on");
            Assert.AreEqual(0, Loot.SampleBonus);
        }

        [UnityTest]
        public IEnumerator A_sale_halves_the_shop_and_the_bell_keeps_to_it()
        {
            _floors.twistOverride = FloorTwist.Sale;
            _floors.BuildFloor(4);
            yield return null;

            Room shop = null;
            foreach (var node in _floors.Layout.ordered) if (node.type == RoomType.Shop) shop = _floors.RoomAt(node.cell);
            Assert.IsNotNull(shop);

            int items = 0;
            foreach (var pickup in Pickup.All)
            {
                if (!shop.ContainsInside(pickup.transform.position, 0f)) continue;
                if (pickup.kind == PickupKind.Item)
                {
                    items++;
                    Assert.AreEqual(FloorTwists.Discount(pickup.item.price), pickup.price, pickup.item.displayName + " is not half price");
                    Assert.Less(pickup.price, pickup.item.price);
                }
                else if (pickup.kind == PickupKind.Health)
                {
                    Assert.AreEqual(FloorTwists.Discount(Pickup.CrumbPrice), pickup.price);
                }
            }
            Assert.AreEqual(3, items);

            RunState.AddCoins(50);
            var bell = shop.GetComponentInChildren<ShopReroll>();
            Assert.IsTrue(bell.TryReroll());
            yield return null;
            foreach (var pickup in Pickup.All)
            {
                if (pickup.kind == PickupKind.Item && shop.ContainsInside(pickup.transform.position, 0f))
                {
                    Assert.AreEqual(FloorTwists.Discount(pickup.item.price), pickup.price, "the bell put out something at full price");
                }
            }
        }

        [UnityTest]
        public IEnumerator Ringing_the_dare_brings_a_worse_wave_and_a_prize()
        {
            _floors.eliteChanceOverride = 0f;
            _floors.twistOverride = FloorTwist.None;
            SaveSystem.Data.meta.daresWon = Achievements.DaresNeeded - 1;
            yield return BuildFloorWithDare(3);

            // The dare room: a dead end, marked, quiet until asked.
            RoomNode node = null;
            int dareRooms = 0;
            foreach (var each in _floors.Layout.ordered) if (each.type == RoomType.Dare) { node = each; dareRooms++; }
            Assert.AreEqual(1, dareRooms);
            Assert.AreEqual(1, node.DoorCount, "the dare room is not a dead end");
            Room room = _floors.RoomAt(node.cell);
            Assert.AreEqual(room, _floors.Dare.room);
            Assert.AreNotEqual(Object.FindAnyObjectByType<Minimap>().normalColor, Object.FindAnyObjectByType<Minimap>().dareColor);
            _player.Teleport(Beside(node));
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(_floors.IsLocked, "the dare room started a fight nobody asked for");
            Assert.That(WorldPrompt.For(room.Centre), Does.Contain("dare").IgnoreCase);

            // Crossing the mat does not ring it.
            _player.Teleport(room.Centre + Vector3.up * 0.05f);
            yield return new WaitForSeconds(DareBell.HoldTime * 0.4f);
            _player.Teleport(Beside(node));
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(_floors.IsLocked, "walking past rang the bell");
            Assert.IsNotNull(_floors.Dare);

            // Standing on it does.
            _player.Teleport(room.Centre + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.IsLocked, DareBell.HoldTime + 1f, "the dare to start");
            Assert.IsTrue(_floors.DareFight);
            yield return null;
            Assert.IsTrue(_floors.Dare == null, "the bell is still there");
            int usual = _floors.biome.EnemiesFor(_floors.FloorInBiome, room.Shape.interior.Count);
            Assert.GreaterOrEqual(_floors.LiveEnemies.Count, usual + FloorManager.DareExtra);
            int elites = 0;
            foreach (var enemy in _floors.LiveEnemies) if (enemy.IsElite) elites++;
            Assert.AreEqual(1, elites, "the dare has no elite");
            foreach (var enemy in _floors.LiveEnemies)
            {
                Assert.Greater((enemy.transform.position - room.Centre).magnitude, 3f, enemy.name + " arrived on top of the player");
            }
            yield return WaitUntil(() => Radio.Instance.LastMoment == RadioMoment.Dare, 3f, "the radio to remark on it");

            _player.Teleport(Beside(node));
            Assert.IsFalse(Achievements.Has(AchievementId.AskedForIt));
            yield return KillAll();
            Assert.IsFalse(_floors.DareFight);
            bool prize = false;
            foreach (var pickup in Pickup.All)
            {
                Vector3 from = pickup.transform.position - room.Centre;
                from.y = 0f;
                if (pickup.kind == PickupKind.Item && pickup.price == 0 && from.magnitude < 0.8f) prize = true;
            }
            Assert.IsTrue(prize, "nothing was left where the bell stood");
            Assert.AreEqual(Achievements.DaresNeeded, SaveSystem.Data.meta.daresWon);
            Assert.IsTrue(Achievements.Has(AchievementId.AskedForIt));
            Assert.IsTrue(Achievements.StyleUnlocked("Brass"));

            // Hurt and nearly dead is allowed too: nothing is asked of the player before ringing.
            Assert.IsTrue(_floors.Dare == null, "the bell came back");
        }

        [UnityTest]
        public IEnumerator The_exchange_takes_samples_for_one_item_a_floor()
        {
            _floors.exchangeChanceOverride = 1f;
            _floors.BuildFloor(1);
            yield return null;
            Assert.IsTrue(_floors.Exchange == null, "an exchange on the first floor, before there is anything to trade");

            _floors.BuildFloor(2);
            yield return null;
            SampleExchange exchange = _floors.Exchange;
            Assert.IsNotNull(exchange, "no exchange");
            Vector3 mat = exchange.transform.position + Vector3.up * 0.05f;
            Assert.IsTrue(NavMesh.SamplePosition(mat, out var hit, 0.5f, NavMesh.AllAreas) && hit.position.y < 0.5f, "the exchange is not on open floor");
            Assert.Greater((_player.transform.position - exchange.transform.position).magnitude, exchange.radius + 1f, "the run starts on its mat");

            // Not enough: it says so, and standing there does nothing.
            RunState.AddSamples(SampleExchange.Price - 1 - RunState.Samples);
            _player.Teleport(mat);
            yield return new WaitForSeconds(SampleExchange.HoldTime + 0.4f);
            Assert.IsFalse(exchange.Shut);
            Assert.That(WorldPrompt.For(mat), Does.Contain("does not do credit"));

            // Enough: crossing it trades nothing, standing on it does.
            RunState.AddSamples(11);
            int carried = RunState.Samples;
            int itemsBefore = 0;
            foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Item) itemsBefore++;
            _player.Teleport(mat + new Vector3(-4f, 0f, 0f));
            yield return null;
            _player.Teleport(mat);
            yield return new WaitForSeconds(SampleExchange.HoldTime * 0.4f);
            Assert.IsFalse(exchange.Shut, "walking past traded");
            yield return new WaitForSeconds(SampleExchange.HoldTime);
            Assert.IsTrue(exchange.Shut);
            Assert.AreEqual(carried - SampleExchange.Price, RunState.Samples);
            Assert.AreEqual(RadioMoment.Exchange, Radio.Instance.LastMoment, "Finch let his samples go without a word");
            int itemsAfter = 0;
            foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Item) itemsAfter++;
            Assert.AreEqual(itemsBefore + 1, itemsAfter + _player.Items.Collected.Count, "nothing came out of the hatch");

            // Once a floor.
            RunState.AddSamples(100);
            Assert.IsFalse(exchange.Trade());
            Assert.That(WorldPrompt.For(mat), Does.Contain("shut"));
        }

        [UnityTest]
        public IEnumerator The_first_floor_never_dares_anybody()
        {
            for (int seed = 1; seed <= 40; seed++)
            {
                _floors.BuildFloor(1, seed);
                yield return null;
                Assert.IsTrue(_floors.Dare == null, "a dare on the first floor");
                foreach (var node in _floors.Layout.ordered) Assert.AreNotEqual(RoomType.Dare, node.type);
                _floors.BuildFloor(10, seed);
                yield return null;
                Assert.IsTrue(_floors.Dare == null, "a dare on a boss's floor");
            }
        }
    }
}
