using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>Plays the Run scene. The headline check is Milestone 2's finish line: 20 generated floors in a row with nothing broken.</summary>
    public class FloorTests
    {
        PlayerController _player;
        FloorManager _floors;

        [UnitySetUp]
        public IEnumerator LoadRun()
        {
            // Its own save file, wiped, so the player's real progress and upgrades never affect the tests.
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            _player = PlayerController.Instance;
            _floors = FloorManager.Instance;
            Assert.IsNotNull(_player, "Run scene has no player");
            Assert.IsNotNull(_floors, "Run scene has no FloorManager");
            _player.Input.overrideFrame = new InputFrame();
        }

        // Walkable floor near a point. Anything above floor level (the top of a prop) is rejected.
        static Vector3 OnNavMesh(Vector3 near)
        {
            Assert.IsTrue(NavMesh.SamplePosition(near, out var hit, 2f, NavMesh.AllAreas), $"no walkable ground near {near}");
            Assert.Less(hit.position.y, 0.5f, $"nearest walkable point to {near} is up on an obstacle");
            return hit.position;
        }

        // One tile east of the room's centre: always clear floor, and never on top of the centre prop.
        Vector3 StandingSpot(RoomNode node) => OnNavMesh(_floors.RoomAt(node.cell).Centre + new Vector3(RoomShape.TileSize, 0f, 0f));

        string Describe(RoomNode node) =>
            $"floor {_floors.FloorNumber} (seed {_floors.Layout.seed}), {node.type} room {node.cell}\n{_floors.Layout.ToAscii()}\n{_floors.RoomAt(node.cell).Shape.ToAscii()}";

        IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.deltaTime) yield return null;
            Assert.IsTrue(condition(), $"floor {_floors.FloorNumber} (seed {_floors.Layout.seed}): timed out waiting for {what}\n{_floors.Layout.ToAscii()}");
        }

        // Steers the player along the navmesh path to a point, the way a person would walk it.
        IEnumerator WalkTo(Vector3 destination, float timeout, string what)
        {
            var path = new NavMeshPath();
            for (float t = 0f; t < timeout; t += Time.deltaTime)
            {
                Vector3 position = _player.transform.position;
                Vector3 remaining = destination - position;
                remaining.y = 0f;
                if (remaining.magnitude < 0.6f) break;

                Vector3 next = destination;
                if (NavMesh.CalculatePath(position, destination, NavMesh.AllAreas, path) && path.corners.Length > 1) next = path.corners[1];
                Vector3 direction = next - position;
                direction.y = 0f;
                _player.Input.overrideFrame = new InputFrame { move = direction.normalized };
                yield return null;
            }
            _player.Input.overrideFrame = new InputFrame();
            Vector3 left = destination - _player.transform.position;
            left.y = 0f;
            Assert.Less(left.magnitude, 0.6f, $"could not walk to {what}");
        }

        // Stands in a fight room, checks the doors lock and enemies appear, kills them, checks the doors open again.
        IEnumerator FightRoom(RoomNode node)
        {
            Room room = _floors.RoomAt(node.cell);

            // In the exit room, stand exactly where the hole will open, to prove it never drops the player by surprise.
            Vector3 spot = node.type == RoomType.Exit ? room.Centre : StandingSpot(node);
            _player.Teleport(spot + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.IsLocked, 2f, $"{node.type} room {node.cell} to lock");

            Assert.AreEqual(node, _floors.CurrentNode);
            Assert.Greater(_floors.LiveEnemies.Count, 0, "locked room has no enemies");
            foreach (var enemy in _floors.LiveEnemies)
            {
                if (!_floors.Layout.InArena(node.cell)) Assert.IsTrue(room.ContainsInside(enemy.transform.position, 0f), "enemy spawned outside its room\n" + Describe(node));
                bool onFloor = NavMesh.SamplePosition(enemy.transform.position, out var hit, 1f, NavMesh.AllAreas);
                Assert.IsTrue(onFloor && hit.position.y < 0.5f,
                    $"enemy spawned inside solid earth: {enemy.name} at {enemy.transform.position}, nearest floor {(onFloor ? hit.position.ToString() : "none within a metre")}\n" + Describe(node));
            }

            // Locked means locked: every doorway has its barrier up, and it is solid.
            float inner = (RoomShape.Size * 0.5f - 1.5f) * RoomShape.TileSize;
            for (int side = 0; side < 4; side++)
            {
                bool hasDoor = (room.Doors & (Doors)(1 << side)) != 0; // the doorways that lead out of the room (for an arena quarter, not the open sides)
                GameObject door = room.DoorObject(side);
                Assert.AreEqual(hasDoor, door != null, $"door object on side {(Doors)(1 << side)}");
                if (!hasDoor) continue;

                Assert.IsTrue(door.activeSelf, $"door {(Doors)(1 << side)} of locked room {node.cell} is open");
                var d = FloorLayout.Directions[side];
                var direction = new Vector3(d.x, 0f, d.y);
                Vector3 origin = room.transform.position + Vector3.up + direction * inner;
                Assert.IsTrue(Physics.Raycast(origin, direction, out var blocked, 3f) && blocked.collider.gameObject == door,
                    $"locked door {(Doors)(1 << side)} of {node.cell} does not block");
            }

            foreach (var enemy in new List<Enemy>(_floors.LiveEnemies)) enemy.Health.Kill();
            yield return WaitUntil(() => !_floors.IsLocked, 2f, $"room {node.cell} to unlock");
            Assert.IsTrue(node.cleared);
            for (int side = 0; side < 4; side++)
            {
                if (room.DoorObject(side) != null) Assert.IsFalse(room.DoorObject(side).activeSelf, "door still shut after clearing");
            }
        }

        /// <summary>Not a test: hunts for the rare floor where something is put down off the floor. Read the failure, if any.</summary>
        [UnityTest, Explicit, Timeout(1800000)]
        public IEnumerator Hunt_for_bad_spawns()
        {
            for (int round = 0; round < 120; round++)
            {
                int floor = 6 + round % 4;
                _floors.BuildFloor(floor);
                yield return null;
                _player.Health.SetInvulnerable(600f);
                foreach (var node in _floors.Layout.ordered)
                {
                    if (node.type != RoomType.Normal) continue;
                    _player.Teleport(StandingSpot(node) + Vector3.up * 0.05f);
                    for (float t = 0f; t < 2f && !_floors.IsLocked; t += Time.deltaTime) yield return null;
                    if (!_floors.IsLocked) continue;
                    foreach (var enemy in _floors.LiveEnemies)
                    {
                        bool onFloor = NavMesh.SamplePosition(enemy.transform.position, out var hit, 1f, NavMesh.AllAreas);
                        Assert.IsTrue(onFloor && hit.position.y < 0.5f,
                            $"round {round}: {enemy.name} at {enemy.transform.position}, nearest floor {(onFloor ? hit.position.ToString() : "none within a metre")}, seed {_floors.Layout.seed}\n" + Describe(node));
                    }
                    foreach (var enemy in new List<Enemy>(_floors.LiveEnemies)) enemy.Health.Kill();
                    for (float t = 0f; t < 2f && _floors.IsLocked; t += Time.deltaTime) yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator Twenty_floors_in_a_row_build_and_play_through()
        {
            var minimap = Object.FindAnyObjectByType<Minimap>();
            Assert.IsNotNull(minimap, "no minimap in the Run scene");

            for (int floor = 1; floor <= 20; floor++)
            {
                _player.Health.SetInvulnerable(600f);
                var layout = _floors.Layout;
                Assert.AreEqual(floor, _floors.FloorNumber);
                Assert.IsEmpty(layout.Validate(), $"floor {floor} layout");
                Assert.AreEqual(layout.start, _floors.CurrentNode, "player did not start in the start room");
                Assert.IsFalse(_floors.IsLocked, "start room locked");
                Assert.IsFalse(_floors.ExitOpen, "exit open before the exit room was cleared");

                // Every room exists in 3D with a valid shape and the doorways the layout asked for...
                Vector3 startPoint = OnNavMesh(_player.transform.position);
                RoomNode firstNormal = null;
                foreach (var node in layout.ordered)
                {
                    Room room = _floors.RoomAt(node.cell);
                    Assert.IsNotNull(room, $"floor {floor}: no room built at {node.cell}");
                    if (layout.InArena(node.cell)) Assert.AreEqual(room.Doors, node.doors & room.Doors, "an arena quarter with a doorway the layout never asked for"); else Assert.AreEqual(node.doors, room.Doors);
                    Assert.IsEmpty(room.Shape.Validate(), "room shape\n" + Describe(node));

                    // ...and can actually be walked to from the start.
                    var path = new NavMeshPath();
                    Vector3 target = StandingSpot(node);
                    bool found = NavMesh.CalculatePath(startPoint, target, NavMesh.AllAreas, path);
                    Assert.IsTrue(found && path.status == NavMeshPathStatus.PathComplete,
                        $"cannot be reached: found={found} status={path.status} from={startPoint} to={target}\n" + Describe(node));

                    if (firstNormal == null && node.type == RoomType.Normal) firstNormal = node;
                }

                // Nothing is left over from the previous floor.
                Assert.AreEqual(layout.rooms.Count, Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Length, "stray rooms left over from the last floor");

                yield return null;
                Assert.GreaterOrEqual(minimap.VisibleTiles, 2, "minimap shows nothing");

                if (firstNormal != null) yield return FightRoom(firstNormal);
                yield return FightRoom(layout.exit);

                Assert.IsTrue(_floors.ExitOpen, "exit did not open after the exit room was cleared");
                Room exitRoom = _floors.RoomAt(layout.exit.cell);
                int next = floor + 1;

                // Floor 10 of a biome ends in the portal room instead of a hole: take the portal deeper.
                if (_floors.FloorInBiome == 10)
                {
                    Assert.IsNotNull(_floors.DeeperPortal, "no portal after the biome boss");
                    int depthBefore = _floors.depth;
                    _player.Teleport(_floors.DeeperPortal.transform.position + Vector3.up * 0.05f);
                    yield return WaitUntil(() => _floors.FloorNumber == next, 2f, "the next biome");
                    Assert.AreEqual(depthBefore + 1, _floors.depth);
                    yield return null;
                    continue;
                }
                Assert.IsTrue(exitRoom.ExitHole.activeSelf);

                // The hole opened under the player, so nothing happens until they step off it and back on.
                yield return new WaitForSeconds(0.2f);
                Assert.AreEqual(floor, _floors.FloorNumber, "dropped to the next floor without stepping onto the exit");
                _player.Teleport(OffTheHole(exitRoom) + Vector3.up * 0.05f);
                yield return null;
                yield return null;
                _player.Teleport(exitRoom.ExitHole.transform.position + Vector3.up * 0.05f);
                yield return WaitUntil(() => _floors.FloorNumber == next, 2f, "the next floor");
                yield return null; // let the old rooms finish being destroyed
            }

            Assert.AreEqual(21, _floors.FloorsBuilt);
        }

        [UnityTest]
        public IEnumerator The_hole_opens_a_results_screen_and_its_card_drops_to_the_next_floor()
        {
            FloorResults.ShowInTests = true;
            try
            {
                _player.Health.SetInvulnerable(600f);
                int floor = _floors.FloorNumber;
                yield return FightRoom(_floors.Layout.exit);
                Room exitRoom = _floors.RoomAt(_floors.Layout.exit.cell);
                Assert.IsTrue(exitRoom.ExitHole.activeSelf, "an ordinary floor should end in a hole");
                Assert.IsNull(_floors.ExtractPortal, "an ordinary floor should not end in portals");
                yield return new WaitForSeconds(0.3f);
                Assert.IsFalse(FloorResults.IsOpen, "the results came up before the hole was used");

                _player.Teleport(OffTheHole(exitRoom) + Vector3.up * 0.05f);
                yield return null;
                yield return null;
                _player.Teleport(exitRoom.ExitHole.transform.position + Vector3.up * 0.05f);
                yield return WaitUntil(() => FloorResults.IsOpen, 2f, "the results screen at the hole");
                var results = FloorResults.Instance;
                Assert.AreEqual(1, results.Choices.Count, "the hole has one way on");
                Assert.IsTrue(results.Choices[0].down);
                StringAssert.Contains("FLOOR", results.Choices[0].title);
                Assert.AreEqual(0f, Time.timeScale);
                Assert.AreEqual(floor, _floors.FloorNumber);

                results.Choose(0);
                Assert.AreEqual(1f, Time.timeScale);
                yield return WaitUntil(() => _floors.FloorNumber == floor + 1, 2f, "the next floor");
            }
            finally
            {
                FloorResults.ShowInTests = false;
                Time.timeScale = 1f;
            }
        }

        // A clear spot in the room that is well off the exit hole: the corner of the 3Ã—3 clear middle.
        static Vector3 OffTheHole(Room room) => room.Centre + new Vector3(RoomShape.TileSize, 0f, RoomShape.TileSize);

        [UnityTest]
        public IEnumerator Player_can_walk_from_the_start_to_the_exit_room()
        {
            _player.Health.SetInvulnerable(600f);
            var layout = _floors.Layout;

            // Rooms from the exit back to the start, following the distance numbers down.
            var route = new List<RoomNode> { layout.exit };
            while (route[route.Count - 1] != layout.start)
            {
                RoomNode here = route[route.Count - 1], closer = null;
                foreach (var neighbour in layout.Neighbours(here))
                {
                    if (neighbour.distance == here.distance - 1) { closer = neighbour; break; }
                }
                Assert.IsNotNull(closer, "layout distances are broken");
                route.Add(closer);
            }
            route.Reverse();

            for (int i = 1; i < route.Count; i++)
            {
                RoomNode node = route[i];
                yield return WalkTo(StandingSpot(node), 20f, Describe(node));
                Assert.AreEqual(node, _floors.CurrentNode);

                // Fight rooms lock on the way in; clear them to carry on.
                if (_floors.IsLocked)
                {
                    foreach (var enemy in new List<Enemy>(_floors.LiveEnemies)) enemy.Health.Kill();
                    yield return WaitUntil(() => !_floors.IsLocked, 2f, "the room to unlock");
                }
            }

            Assert.AreEqual(layout.exit, _floors.CurrentNode);
            Assert.IsTrue(_floors.ExitOpen);
        }

        [UnityTest]
        public IEnumerator Walls_keep_the_player_inside_the_floor()
        {
            // Push blindly in all four directions from the start room's centre. Wherever the player ends up
            // must still be real floor: never inside solid earth, never outside the map.
            _player.Health.SetInvulnerable(600f);
            var start = _floors.Layout.start;
            Room room = _floors.RoomAt(start.cell);

            for (int side = 0; side < 4; side++)
            {
                _player.Teleport(room.Centre + Vector3.up * 0.05f);
                var d = FloorLayout.Directions[side];
                _player.Input.overrideFrame = new InputFrame { move = new Vector3(d.x, 0f, d.y) };
                yield return new WaitForSeconds(2.5f);

                Vector3 position = _player.transform.position;
                Assert.Greater(position.y, -0.5f, "fell through the floor");
                Assert.IsTrue(NavMesh.SamplePosition(position, out var hit, 1f, NavMesh.AllAreas) && hit.position.y < 0.5f,
                    $"player ended up inside solid earth or outside the floor at {position}\n" + Describe(start));

                // Walking may have locked a neighbouring fight room; clear it so the next push starts clean.
                foreach (var enemy in new List<Enemy>(_floors.LiveEnemies)) enemy.Health.Kill();
                yield return null;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Deep_floors_spawn_mixed_ants_mutated_within_budget()
        {
            _player.Health.SetInvulnerable(600f);
            _floors.depth = 5;
            _floors.BuildFloor(7);
            yield return null;

            int budget = MutationBudget.MaxPoints(5, 7);
            var kinds = new HashSet<string>();
            var mutationsSeen = new HashSet<MutationType>();
            int enemies = 0, mutated = 0;

            foreach (var node in _floors.Layout.ordered)
            {
                if (node.type != RoomType.Normal) continue;
                _player.Teleport(StandingSpot(node) + Vector3.up * 0.05f);
                yield return WaitUntil(() => _floors.IsLocked, 2f, "the room to lock");

                foreach (var enemy in _floors.LiveEnemies)
                {
                    enemies++;
                    kinds.Add(enemy.GetType().Name);
                    Assert.IsNotNull(enemy.Mutations, enemy.name + " cannot carry mutations");
                    Assert.LessOrEqual(enemy.Mutations.TotalCost, budget + (enemy.IsElite ? 1 : 0), enemy.name + " is over the mutation budget");
                    if (enemy.Mutations.Active.Count > 0) mutated++;
                    foreach (var mutation in enemy.Mutations.Active) mutationsSeen.Add(mutation.type);
                }

                foreach (var enemy in new List<Enemy>(_floors.LiveEnemies)) enemy.Health.Kill();
                yield return null;
                // A Swarm Call or two may have brought more in; keep clearing until the room opens.
                for (int i = 0; i < 20 && _floors.IsLocked; i++)
                {
                    foreach (var enemy in new List<Enemy>(_floors.LiveEnemies)) enemy.Health.Kill();
                    yield return null;
                }
                Assert.IsFalse(_floors.IsLocked, "room never unlocked");
            }

            Debug.Log($"[Shrunk] Depth 5, floor 7: {enemies} enemies, {mutated} mutated, kinds: {string.Join(", ", kinds)}; mutations: {string.Join(", ", mutationsSeen)}");
            Assert.Greater(enemies, 8);
            Assert.Greater(mutated, enemies / 2, "too few enemies were mutated at depth 5");
            Assert.GreaterOrEqual(kinds.Count, 3, "floor 7 should mix at least three kinds of ant");
            Assert.GreaterOrEqual(mutationsSeen.Count, 5, "too little variety in mutations");
        }

        RoomNode RoomOfType(RoomType type)
        {
            foreach (var node in _floors.Layout.ordered) if (node.type == type) return node;
            return null;
        }

        [UnityTest]
        public IEnumerator Treasure_room_gives_a_free_item()
        {
            var node = RoomOfType(RoomType.Treasure);
            Room room = _floors.RoomAt(node.cell);

            Pickup offer = null;
            foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Item && pickup.treasureRoom == room) offer = pickup;
            Assert.IsNotNull(offer, "treasure room has no item");
            Assert.AreEqual(0, offer.price);
            Assert.IsFalse(offer.item.rare, "a rare item turned up in a treasure room");
            ItemDefinition item = offer.item;

            // Standing a couple of metres off does nothing; walking up to the pedestal takes the item.
            _player.Teleport(StandingSpot(node) + Vector3.up * 0.05f);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(0, _player.Items.Count(item.type), "picked up from too far away");
            _player.Input.overrideFrame = new InputFrame { move = (room.Centre - _player.transform.position).normalized };
            yield return WaitUntil(() => _player.Items.Count(item.type) == 1, 2f, "the item to be picked up");
            _player.Input.overrideFrame = new InputFrame();
            Assert.AreEqual(node, _floors.CurrentNode);
            Assert.IsFalse(_floors.IsLocked, "treasure room locked");
        }

        [UnityTest]
        public IEnumerator The_shop_sells_a_crumb_that_only_the_hurt_can_buy()
        {
            var node = RoomOfType(RoomType.Shop);
            Room room = _floors.RoomAt(node.cell);
            Pickup crumb = null;
            foreach (var pickup in Pickup.All)
            {
                if (pickup.kind == PickupKind.Health && room.ContainsInside(pickup.transform.position, 0f)) crumb = pickup;
            }
            Assert.IsNotNull(crumb, "the shop has nothing to eat");
            Assert.AreEqual(Pickup.CrumbPrice, crumb.price);
            Vector3 spot = crumb.transform.position;
            spot.y = 0.05f;

            // Unhurt, with coins: it stays on the shelf and the coins stay in the pocket.
            RunState.AddCoins(20);
            _player.Teleport(spot);
            yield return new WaitForSeconds(0.3f);
            Assert.IsNotNull(crumb, "sold to somebody who was not hurt");
            Assert.AreEqual(20, RunState.Coins);
            Assert.That(WorldPrompt.For(spot), Does.Contain("not hungry"));

            // Hurt: bought, eaten, paid for.
            _player.Health.ClearInvulnerability();
            _player.Health.TakeDamage(1, Vector3.forward);
            int hurt = _player.Health.Current;
            yield return WaitUntil(() => _player.Health.Current == hurt + 1, 1f, "the crumb to be eaten");
            Assert.AreEqual(20 - Pickup.CrumbPrice, RunState.Coins);
            yield return null;
            Assert.IsTrue(crumb == null, "the crumb is still on the shelf");
        }

        [UnityTest]
        public IEnumerator The_shop_bell_restocks_the_shelf_for_a_rising_price()
        {
            var node = RoomOfType(RoomType.Shop);
            Room room = _floors.RoomAt(node.cell);
            var bell = room.GetComponentInChildren<ShopReroll>();
            Assert.IsNotNull(bell, "the shop has no bell");

            System.Func<List<ItemType>> shelf = () =>
            {
                var types = new List<ItemType>();
                foreach (var pickup in Pickup.All)
                {
                    if (pickup != null && pickup.kind == PickupKind.Item && pickup.price > 0 && room.ContainsInside(pickup.transform.position, 0f)) types.Add(pickup.item.type);
                }
                types.Sort();
                return types;
            };
            List<ItemType> first = shelf();
            Assert.AreEqual(3, first.Count);

            // No coins: nothing happens.
            Assert.IsFalse(bell.TryReroll());
            Assert.AreEqual(ShopReroll.FirstPrice, bell.price);

            // Stepping onto the mat with coins: paid, a different three, and dearer next time.
            RunState.AddCoins(30);
            _player.Teleport(bell.transform.position + new Vector3(0f, 0.05f, -4f));
            yield return null;
            yield return null;
            _player.Teleport(bell.transform.position + Vector3.up * 0.05f);
            yield return WaitUntil(() => bell.Rerolls == 1, 1f, "the bell to be rung");
            yield return null;
            Assert.AreEqual(30 - ShopReroll.FirstPrice, RunState.Coins);
            List<ItemType> second = shelf();
            Assert.AreEqual(3, second.Count, "the shelf was not restocked with three");
            CollectionAssert.AreNotEqual(first, second, "the same three things came back");
            Assert.AreEqual(ShopReroll.FirstPrice + ShopReroll.PriceStep, bell.price);
            Assert.That(WorldPrompt.For(bell.transform.position), Does.Contain(bell.price + " coins"));
        }

        [UnityTest]
        public IEnumerator Shop_sells_three_items_for_coins()
        {
            var node = RoomOfType(RoomType.Shop);
            Room room = _floors.RoomAt(node.cell);

            var stock = new List<Pickup>();
            foreach (var pickup in Pickup.All)
            {
                if (pickup.kind == PickupKind.Item && pickup.price > 0 && room.ContainsInside(pickup.transform.position, 0f)) stock.Add(pickup);
            }
            Assert.AreEqual(3, stock.Count, "shop should stock three items");
            Assert.AreEqual(3, new HashSet<ItemType>(stock.ConvertAll(p => p.item.type)).Count, "shop stocked the same item twice");

            Pickup wanted = stock[0];
            ItemDefinition item = wanted.item;
            Vector3 spot = wanted.transform.position;
            spot.y = 0.05f;

            // No coins: standing on it does nothing.
            _player.Teleport(spot);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0, _player.Items.Count(item.type), "got a shop item for free");
            Assert.IsNotNull(wanted);

            // With coins: it is bought and the price comes off.
            RunState.AddCoins(item.price + 5);
            yield return WaitUntil(() => _player.Items.Count(item.type) == 1, 1f, "the purchase");
            Assert.AreEqual(5, RunState.Coins);
            yield return null;
            Assert.AreEqual(2, stock.FindAll(p => p != null).Count, "the bought item is still on the shelf");
        }

        [UnityTest]
        public IEnumerator Chitin_Plate_recharges_when_entering_a_new_room()
        {
            _player.Items.Add(GameAssets.Instance.items.Get(ItemType.ChitinPlate));
            _player.Health.TakeDamage(1, Vector3.forward);
            Assert.AreEqual(0, _player.Items.ShieldCharges);

            RoomNode other = null;
            foreach (var neighbour in _floors.Layout.Neighbours(_floors.Layout.start)) { other = neighbour; break; }
            _player.Health.ClearInvulnerability();
            _player.Teleport(StandingSpot(other) + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.CurrentNode == other, 1f, "the room change");
            Assert.AreEqual(1, _player.Items.ShieldCharges);
        }

        [UnityTest]
        public IEnumerator Floor_5_has_the_Brood_Mother_and_floor_10_the_Ant_Queen()
        {
            _player.Health.SetInvulnerable(600f);
            foreach (int floor in new[] { 5, 10 })
            {
                _floors.BuildFloor(floor);
                yield return null;
                var layout = _floors.Layout;
                // Five rooms, up to three dead ends added for the treasure, shop and dare rooms, and three more squares of arena.
                Assert.LessOrEqual(layout.rooms.Count, 11, "boss floors should be short");

                Room arena = _floors.RoomAt(layout.exit.cell);
                Assert.IsTrue(arena.Shape.isArena, "the boss room should be a wide-open arena");
                if (layout.hasArena)
                {
                    // Four squares, one room: walk from quarter to quarter with no wall in the way, and far more floor than any fight room.
                    int tiles = 0;
                    var path = new NavMeshPath();
                    foreach (var node in layout.ordered)
                    {
                        if (!layout.InArena(node.cell)) continue;
                        Room part = _floors.RoomAt(node.cell);
                        tiles += part.Shape.interior.Count;
                        Assert.IsTrue(NavMesh.CalculatePath(OnNavMesh(arena.Centre), OnNavMesh(part.Centre), NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete,
                            "a quarter of the arena is walled off\n" + Describe(node));
                        Assert.IsFalse(Physics.Linecast(arena.Centre + Vector3.up, part.Centre + Vector3.up), "a wall between two quarters of the arena");
                    }
                    Assert.Greater(tiles, 400, "the arena is no bigger than a fight room");
                    Assert.Less((_floors.ArenaCentre - arena.Centre).magnitude, 8f);
                }
                else
                {
                    Assert.AreEqual((RoomShape.Size - 2) * (RoomShape.Size - 2), arena.Shape.interior.Count);
                }
                Assert.IsNull(_floors.Boss, "boss appeared before the player walked in");

                _player.Teleport(arena.Centre + new Vector3(0f, 0.05f, -6f));
                yield return WaitUntil(() => _floors.IsLocked, 2f, "the boss room to lock");
                Assert.IsNotNull(_floors.Boss);
                Assert.Less((_floors.Boss.transform.position - _floors.ArenaCentre).magnitude, 7f, "the boss should start in the middle of its arena");

                // Shut means shut, on every quarter: the player can't walk out of any doorway.
                foreach (var node in layout.ordered)
                {
                    if (node != layout.exit && !layout.InArena(node.cell)) continue;
                    Room part = _floors.RoomAt(node.cell);
                    Assert.IsTrue(part.Locked, "a quarter of the arena was left open");
                    for (int side = 0; side < 4; side++)
                    {
                        bool leadsOut = (part.Shape.doors & (Doors)(1 << side)) != 0;
                        Assert.AreEqual(leadsOut, part.DoorObject(side) != null, "a barrier where two quarters meet, or none on a real doorway");
                        if (leadsOut) Assert.IsTrue(part.DoorObject(side).activeSelf);
                    }
                }
                if (floor == 5) Assert.IsInstanceOf<BroodMother>(_floors.Boss);
                else Assert.IsInstanceOf<AntQueen>(_floors.Boss);
                Assert.AreEqual(1, _floors.LiveEnemies.Count, "the boss should start alone");
                Assert.Greater((_floors.Boss.transform.position - _player.transform.position).magnitude, 4f, "boss appeared on top of the player");

                // Let it bring its company in, then kill only the boss: everything else should go with it.
                yield return new WaitForSeconds(floor == 5 ? 5f : 0.5f);
                Assert.Greater(_floors.LiveEnemies.Count, 1, "the boss brought nothing with it");
                _floors.Boss.Health.Kill();
                yield return WaitUntil(() => !_floors.IsLocked, 2f, "the room to open once the boss is dead");
                Assert.IsTrue(_floors.ExitOpen);
                Assert.AreEqual(0, Enemy.Active.Count, "the boss's company outlived it");
            }
        }

        [UnityTest]
        public IEnumerator Ants_reach_the_player_inside_a_carved_room()
        {
            RoomNode target = null;
            foreach (var node in _floors.Layout.ordered)
            {
                if (node.type == RoomType.Normal) { target = node; break; }
            }
            Assert.IsNotNull(target, "floor has no fight room");

            _player.Teleport(StandingSpot(target) + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.IsLocked, 2f, "the room to lock");

            int max = _player.Health.maxHealth;
            yield return WaitUntil(() => _player.Health.Current < max, 12f, "an ant to reach and bite the player\n" + Describe(target));
        }

        [UnityTest]
        public IEnumerator Auto_camera_eases_back_for_a_fight_and_in_again_after()
        {
            var follow = CameraFollow.Instance;
            Assert.AreEqual(CameraMode.Auto, follow.mode, "a run should start on the Auto camera");
            Assert.AreEqual(0f, follow.Pull, 0.01f, "quiet room: should sit at the shoulder");
            Assert.IsFalse(follow.BodyHidden);

            RoomNode target = null;
            foreach (var node in _floors.Layout.ordered)
            {
                if (node.type == RoomType.Normal) { target = node; break; }
            }
            _player.Health.SetInvulnerable(600f);
            _player.Teleport(StandingSpot(target) + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.IsLocked, 2f, "the room to lock");
            yield return new WaitForSeconds(0.1f);
            Assert.Greater(follow.Pull, 0f, "the view did not start moving");
            Assert.Less(follow.Pull, 0.9f, "the view jumped instead of easing");
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(1f, follow.Pull, 0.03f, "fight: should be pulled back to the wide view");

            foreach (var enemy in new List<Enemy>(_floors.LiveEnemies)) enemy.Health.Kill();
            yield return WaitUntil(() => !_floors.IsLocked, 2f, "the room to unlock");
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(0f, follow.Pull, 0.03f, "fight over: should be back at the shoulder");

            // A boss gets the furthest view.
            _floors.BuildFloor(5);
            yield return null;
            _player.Health.SetInvulnerable(600f);
            _player.Teleport(_floors.RoomAt(_floors.Layout.exit.cell).Centre + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.Boss != null, 2f, "the boss to appear");
            yield return new WaitForSeconds(3f);
            Assert.AreEqual(2f, follow.Pull, 0.05f, "boss fight: should be pulled back furthest");

            // The fixed views never move.
            follow.SetMode(CameraMode.Shoulder);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0f, follow.Pull, 0.001f);
        }

        [UnityTest]
        public IEnumerator Pebbles_are_solid_cover_and_never_wall_anything_off()
        {
            int pebbles = 0;
            for (int floor = 6; floor <= 8; floor++)
            {
                _floors.BuildFloor(floor);
                yield return null;
                var path = new NavMeshPath();
                foreach (var node in _floors.Layout.ordered)
                {
                    Room room = _floors.RoomAt(node.cell);
                    foreach (var tile in room.Shape.cover)
                    {
                        pebbles++;
                        Vector3 at = room.TileWorld(tile);
                        Assert.IsTrue(Physics.CheckSphere(at + Vector3.up * 0.6f, 0.3f, ~0), "nothing solid on a cover tile\n" + Describe(node));
                        if (room.Shape.feature != RoomFeature.RootHall)
                        {
                            Assert.IsFalse(Physics.CheckSphere(at + Vector3.up * 2.6f, 0.3f, ~0), "cover is too tall to be low cover\n" + Describe(node));
                        }
                        Assert.IsFalse(NavMesh.SamplePosition(at, out var on, 0.3f, NavMesh.AllAreas) && on.position.y < 0.5f,
                            "ants can walk through a pebble\n" + Describe(node));
                    }

                    // Every clear tile can still be walked to from the middle of the room.
                    Vector3 centre = OnNavMesh(room.Centre);
                    foreach (var tile in room.Shape.free)
                    {
                        if (!NavMesh.SamplePosition(room.TileWorld(tile), out var hit, 1f, NavMesh.AllAreas)) continue;
                        Assert.IsTrue(NavMesh.CalculatePath(centre, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete,
                            $"tile {tile} is cut off\n" + Describe(node));
                    }
                }
            }
            Assert.Greater(pebbles, 10, "three deep floors had almost no cover");
        }

        [UnityTest]
        public IEnumerator Every_room_is_lit_by_a_few_glowing_mushrooms_and_no_more()
        {
            _floors.BuildFloor(8, 606);
            yield return null;
            foreach (var node in _floors.Layout.ordered)
            {
                Room room = _floors.RoomAt(node.cell);
                var lights = room.GetComponentsInChildren<Light>(true);
                Assert.AreEqual(room.Shape.glows.Count, lights.Length);
                Assert.That(lights.Length, Is.InRange(1, 3), "the renderer lights at most four lamps per room\n" + Describe(node));
                foreach (var light in lights)
                {
                    Assert.AreEqual(room.Drawn, light.enabled, "a lamp is burning in a room that is not drawn");
                    Vector3 local = light.transform.position - room.transform.position;
                    Assert.IsFalse(Physics.CheckSphere(light.transform.position, 0.2f, ~0), "a lamp is inside a wall\n" + Describe(node));
                    Assert.Less(Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.z)), RoomShape.Size * RoomShape.TileSize * 0.5f);
                }
            }
        }

        [UnityTest]
        public IEnumerator The_minimap_pointer_follows_the_player_and_points_where_they_look()
        {
            var minimap = Object.FindAnyObjectByType<Minimap>();
            yield return null;
            Assert.IsNotNull(minimap.Pointer, "no pointer on the minimap");
            Vector2 atStart = minimap.Pointer.anchoredPosition;

            // East on the floor is right on the map; north is up.
            _player.Teleport(_player.transform.position + new Vector3(4f, 0f, 0f));
            yield return null;
            yield return null;
            Assert.Greater(minimap.Pointer.anchoredPosition.x, atStart.x + 0.5f);
            Assert.AreEqual(atStart.y, minimap.Pointer.anchoredPosition.y, 0.01f);
            _player.Teleport(_player.transform.position + new Vector3(0f, 0f, 4f));
            yield return null;
            yield return null;
            Assert.Greater(minimap.Pointer.anchoredPosition.y, atStart.y + 0.5f);

            // Looking east turns the pointer a quarter turn clockwise.
            var follow = CameraFollow.Instance;
            follow.AddLook(90f - follow.LookYaw, 0f);
            yield return null;
            Assert.AreEqual(90f, follow.LookYaw, 0.1f);
            Vector3 nose = minimap.Pointer.up;
            Assert.Greater(nose.x, 0.99f, "the pointer's nose should point right when looking east");

            // A new floor gets a new pointer, not a second one.
            _floors.BuildFloor(2);
            yield return null;
            yield return null;
            int pointers = 0;
            foreach (Transform child in minimap.container) if (child.name == "You") pointers++;
            Assert.AreEqual(1, pointers, "pointers piling up");
        }

        [UnityTest]
        public IEnumerator Only_rooms_near_the_player_are_drawn()
        {
            _floors.BuildFloor(9, 2024);
            yield return null;
            _player.Health.SetInvulnerable(600f);
            int hidden = 0;
            foreach (var node in _floors.Layout.ordered)
            {
                Room room = _floors.RoomAt(node.cell);
                Vector2Int away = node.cell - _floors.CurrentNode.cell;
                bool near = Mathf.Abs(away.x) + Mathf.Abs(away.y) <= _floors.drawDistance;
                Assert.AreEqual(near, room.Drawn, $"room {node.cell}, player in {_floors.CurrentNode.cell}");
                Assert.AreEqual(near, room.GetComponent<MeshRenderer>().enabled);
                Assert.IsTrue(room.GetComponent<MeshCollider>().enabled, "a room that is not drawn must still be solid");
                if (!near) hidden++;
            }
            Assert.Greater(hidden, 0, "a 15-room floor should have rooms too far to see");

            // Walk into the farthest room: it and its neighbours come on, the start goes off.
            Room far = _floors.RoomAt(_floors.Layout.exit.cell);
            Assert.IsFalse(far.Drawn);
            _player.Teleport(far.Centre + Vector3.up * 0.05f);
            yield return null;
            yield return null;
            Assert.IsTrue(far.Drawn);
            Assert.IsFalse(_floors.RoomAt(_floors.Layout.start.cell).Drawn);
        }

        [UnityTest]
        public IEnumerator An_elite_is_bigger_tougher_fully_mutated_and_worth_more()
        {
            Assert.AreEqual(0f, MutationBudget.EliteChance(1, 3), "no elites early in the first biome");
            Assert.AreEqual(0.25f, MutationBudget.EliteChance(1, 7), 0.001f);
            Assert.AreEqual(0.2f, MutationBudget.EliteChance(2, 1), 0.001f);
            Assert.AreEqual(0.9f, MutationBudget.EliteChance(9, 9), 0.001f, "never certain");

            _floors.eliteChanceOverride = 1f;
            RoomNode target = null;
            foreach (var node in _floors.Layout.ordered)
            {
                if (node.type == RoomType.Normal) { target = node; break; }
            }
            _player.Health.SetInvulnerable(600f);
            _player.Teleport(StandingSpot(target) + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.IsLocked, 2f, "the room to lock");

            Enemy elite = null;
            int elites = 0;
            foreach (var enemy in _floors.LiveEnemies)
            {
                if (!enemy.IsElite) continue;
                elite = enemy;
                elites++;
            }
            Assert.AreEqual(1, elites, "exactly one elite in the room");
            Assert.IsNotNull(elite.transform.Find("Elite crown"), "an elite must be marked");
            Assert.Greater(elite.Mutations.TotalCost, 0, "an elite always carries mutations");
            Assert.AreEqual(Enemy.EliteSize, elite.BaseScale / (elite.Mutations.Has(MutationType.Armored) ? 1.1f : 1f) / (elite.Mutations.Has(MutationType.ExplodesOnDeath) ? 1.2f : 1f), 0.01f);

            Enemy prefab = _floors.biome.EnemyNamed(elite.name.Replace("(Clone)", "").Trim());
            Assert.IsNotNull(prefab, "could not find the elite's prefab: " + elite.name);
            int plain = prefab.GetComponent<Health>().maxHealth;
            Assert.AreEqual(Mathf.CeilToInt(plain * Enemy.EliteHealth), elite.Health.maxHealth);
            Assert.AreEqual(elite.Health.maxHealth, elite.Health.Current);

            // Dead, it leaves the extra samples; back in the pool, it is a plain ant again.
            int samplesBefore = 0;
            foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Sample) samplesBefore += pickup.amount;
            int cost = elite.Mutations.TotalCost;
            elite.Health.Kill();
            yield return new WaitForSeconds(0.4f);
            int samples = 0;
            foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Sample) samples += pickup.amount;
            Assert.AreEqual(Loot.SamplesPerEnemy + cost + Loot.EliteSamples, samples - samplesBefore);
            Assert.IsFalse(elite.IsElite);
            Assert.AreEqual(plain, elite.Health.maxHealth, "the pooled ant kept its elite health");
        }

        // Rebuilds the floor with every fight room of one kind, and returns the first of them.
        IEnumerator FloorOf(RoomFeature feature, System.Action<RoomNode> found)
        {
            RoomShape.ForceFeature = feature;
            _floors.BuildFloor(7, 9001);
            RoomShape.ForceFeature = null;
            yield return null;
            _player.Health.SetInvulnerable(600f);
            foreach (var node in _floors.Layout.ordered)
            {
                if (node.type == RoomType.Normal && _floors.RoomAt(node.cell).Shape.feature == feature) { found(node); yield break; }
            }
            Assert.Fail("no " + feature + " on the floor");
        }

        [UnityTest]
        public IEnumerator A_tunnel_bend_locks_fights_and_clears_like_any_room()
        {
            // Look through floors until one has a bend, then play it.
            RoomNode bend = null;
            for (int seed = 5000; seed < 5060 && bend == null; seed++)
            {
                _floors.BuildFloor(4, seed);
                yield return null;
                foreach (var node in _floors.Layout.ordered)
                {
                    if (_floors.RoomAt(node.cell).Shape.feature == RoomFeature.TunnelBend) { bend = node; break; }
                }
            }
            Assert.IsNotNull(bend, "sixty floors without a single tunnel bend");
            _player.Health.SetInvulnerable(600f);
            Room room = _floors.RoomAt(bend.cell);
            Assert.AreEqual(2, bend.DoorCount);

            // Both doorways lead to each other on foot.
            var ends = new List<Vector3>();
            for (int side = 0; side < 4; side++)
            {
                if (room.DoorObject(side) == null) continue;
                Vector2Int d = FloorLayout.Directions[side];
                ends.Add(OnNavMesh(room.transform.position + new Vector3(d.x, 0f, d.y) * ((RoomShape.Size * 0.5f - 1.5f) * RoomShape.TileSize)));
            }
            Assert.AreEqual(2, ends.Count);
            var path = new NavMeshPath();
            Assert.IsTrue(NavMesh.CalculatePath(ends[0], ends[1], NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "no way through the bend\n" + Describe(bend));
            Assert.Greater(path.corners.Length, 2, "a bend should turn at least one corner\n" + Describe(bend));

            yield return FightRoom(bend);
        }

        [UnityTest]
        public IEnumerator Nursery_pods_pop_for_loot_and_live_ones_hatch_into_the_fight()
        {
            RoomNode nursery = null;
            yield return FloorOf(RoomFeature.Nursery, node => nursery = node);
            Room room = _floors.RoomAt(nursery.cell);
            var pods = room.GetComponentsInChildren<LarvaPod>();
            var hud = Object.FindAnyObjectByType<Hud>();
            Assert.That(hud.LastToast ?? "", Does.Not.Contain("Nursery"), "the room was announced before the player got there");
            Assert.AreEqual(room.Shape.cover.Count, pods.Length, "one pod per cover tile");

            // Shot open: gone, and counted. With the dice loaded, it leaves a coin.
            int popped = LarvaPod.Popped, pickups = Pickup.All.Count;
            pods[0].coinChance = 1f;
            var health = pods[0].GetComponent<Health>();
            Assert.AreEqual(Team.Enemy, health.team, "the player's shots must be able to hit a pod");
            health.TakeDamage(1, Vector3.forward);
            Assert.IsFalse(health.IsDead, "a pod should take two shots");
            health.TakeDamage(1, Vector3.forward);
            yield return null;
            Assert.AreEqual(popped + 1, LarvaPod.Popped);
            Assert.Greater(Pickup.All.Count, pickups, "a popped pod left nothing");
            Assert.IsTrue(pods[0] == null, "the popped pod is still there");

            if (pods.Length < 2) yield break;

            // A live one does nothing until there is a fight, then hatches a larva that joins it.
            LarvaPod live = pods[1];
            live.hatches = true;
            live.hatchAfter = 0.6f;
            for (int i = 2; i < pods.Length; i++) pods[i].hatches = false;
            int hatched = LarvaPod.Hatched;
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(hatched, LarvaPod.Hatched, "a pod hatched with no fight going on");

            _player.Teleport(StandingSpot(nursery) + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.IsLocked, 2f, "the nursery to lock");
            Assert.That(hud.LastToast, Does.Contain("Nursery"), "walking in should say what kind of room it is");
            int before = _floors.LiveEnemies.Count;
            yield return WaitUntil(() => LarvaPod.Hatched > hatched, 3f, "the live pod to hatch");
            Assert.AreEqual(before + 1, _floors.LiveEnemies.Count, "the hatchling did not join the fight");
            Assert.IsInstanceOf<LarvaEnemy>(_floors.LiveEnemies[_floors.LiveEnemies.Count - 1]);

            foreach (var enemy in new List<Enemy>(_floors.LiveEnemies)) enemy.Health.Kill();
            yield return WaitUntil(() => !_floors.IsLocked, 2f, "the nursery to open");
        }

        [UnityTest]
        public IEnumerator Dew_drops_slow_the_player_under_them_and_only_them()
        {
            RoomNode dewRoom = null;
            yield return FloorOf(RoomFeature.DewRoom, node => dewRoom = node);
            Room room = _floors.RoomAt(dewRoom.cell);
            var drips = room.GetComponentsInChildren<DewDrip>();
            Assert.AreEqual(room.Shape.spots.Count, drips.Length);
            Assert.Greater(drips.Length, 0);

            // Not in the room: nothing falls.
            yield return new WaitForSeconds(1f);
            foreach (var drip in drips) Assert.AreEqual(0, drip.Landed, "dew fell with nobody in the room");

            // Standing clear of every spot: drops land, the player stays dry.
            foreach (var drip in drips) drip.interval = 1.2f;
            _player.Teleport(room.Centre + Vector3.up * 0.05f);
            yield return WaitUntil(() => drips[0].Landed >= 1, 4f, "a drop to land");
            Assert.IsFalse(_player.IsSlowed, "soaked while standing clear of the wet patches");

            // Under one: soaked, and walking covers about half the ground.
            _player.Teleport(drips[0].transform.position + Vector3.up * 0.05f);
            int landed = drips[0].Landed;
            yield return WaitUntil(() => drips[0].Landed > landed, 4f, "the next drop to land");
            Assert.IsTrue(_player.IsSlowed, "a drop landed on the player and did nothing");

            foreach (var enemy in new List<Enemy>(_floors.LiveEnemies)) enemy.Health.Kill();
            foreach (var drip in drips) drip.enabled = false;
            _player.Teleport(room.Centre + Vector3.up * 0.05f);
            _player.Slow(0.5f, 5f);
            Vector3 from = _player.transform.position;
            _player.Input.overrideFrame = new InputFrame { move = Vector3.forward };
            yield return new WaitForSeconds(0.5f);
            float slow = (_player.transform.position - from).magnitude;
            _player.Slow(1f, 0f);
            yield return null;
            Assert.IsFalse(_player.IsSlowed);
            _player.Teleport(room.Centre + Vector3.up * 0.05f);
            from = _player.transform.position;
            yield return new WaitForSeconds(0.5f);
            float normal = (_player.transform.position - from).magnitude;
            _player.Input.overrideFrame = new InputFrame();
            Assert.Less(slow, normal * 0.7f, $"soaked {slow:F2} m against {normal:F2} m dry");
        }

        [UnityTest]
        public IEnumerator Food_stores_and_root_halls_are_built_from_their_own_pieces()
        {
            RoomNode found = null;
            yield return FloorOf(RoomFeature.FoodStore, node => found = node);
            Transform store = _floors.RoomAt(found.cell).transform;
            Assert.IsNotNull(store.Find("Sugar Cube"), "a food store with no sugar cube");
            Assert.IsNull(store.Find("Pebble"), "pebbles in a food store");

            yield return FloorOf(RoomFeature.RootHall, node => found = node);
            Room hall = _floors.RoomAt(found.cell);
            Assert.IsNotNull(hall.transform.Find("Root"));
            // A root blocks sight all the way up, which pebbles don't.
            Vector3 at = hall.TileWorld(hall.Shape.cover[0]);
            Assert.IsTrue(Physics.CheckSphere(at + Vector3.up * 4f, 0.2f, ~0), "the root does not reach up to the ceiling");
        }
    }
}
