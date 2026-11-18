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
                Assert.IsTrue(room.ContainsInside(enemy.transform.position, 0f), "enemy spawned outside its room\n" + Describe(node));
                Assert.IsTrue(NavMesh.SamplePosition(enemy.transform.position, out var hit, 1f, NavMesh.AllAreas) && hit.position.y < 0.5f,
                    "enemy spawned inside solid earth\n" + Describe(node));
            }

            // Locked means locked: every doorway has its barrier up, and it is solid.
            float inner = (RoomShape.Size * 0.5f - 1.5f) * RoomShape.TileSize;
            for (int side = 0; side < 4; side++)
            {
                bool hasDoor = (node.doors & (Doors)(1 << side)) != 0;
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
                    Assert.AreEqual(node.doors, room.Doors);
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

        // A clear spot in the room that is well off the exit hole: the corner of the 3×3 clear middle.
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
                    Assert.LessOrEqual(enemy.Mutations.TotalCost, budget, enemy.name + " is over the mutation budget");
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
                Assert.LessOrEqual(layout.rooms.Count, 7, "boss floors should be short");

                Room arena = _floors.RoomAt(layout.exit.cell);
                Assert.IsTrue(arena.Shape.isArena, "the boss room should be a wide-open arena");
                Assert.AreEqual((RoomShape.Size - 2) * (RoomShape.Size - 2), arena.Shape.interior.Count);
                Assert.IsNull(_floors.Boss, "boss appeared before the player walked in");

                _player.Teleport(arena.Centre + new Vector3(0f, 0.05f, -6f));
                yield return WaitUntil(() => _floors.IsLocked, 2f, "the boss room to lock");
                Assert.IsNotNull(_floors.Boss);
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
    }
}
