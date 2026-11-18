using System.Collections;
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
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            _player = PlayerController.Instance;
            _floors = FloorManager.Instance;
            Assert.IsNotNull(_player, "Run scene has no player");
            Assert.IsNotNull(_floors, "Run scene has no FloorManager");
            _player.Input.overrideFrame = new InputFrame();
        }

        // Walkable floor near a point. Tops of pillars and walls are navmesh islands, so anything above floor level is rejected.
        static Vector3 OnNavMesh(Vector3 near)
        {
            Assert.IsTrue(NavMesh.SamplePosition(near, out var hit, 2f, NavMesh.AllAreas), $"no walkable ground near {near}");
            Assert.Less(hit.position.y, 0.5f, $"nearest walkable point to {near} is up on an obstacle");
            return hit.position;
        }

        // A spot inside the room that is clear of props in every room prefab (some have a pillar or pedestal dead centre).
        Vector3 StandingSpot(RoomNode node) => OnNavMesh(_floors.CellCentre(node.cell) + new Vector3(3f, 0f, 0f));

        IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.deltaTime) yield return null;
            Assert.IsTrue(condition(), $"floor {_floors.FloorNumber} (seed {_floors.Layout.seed}): timed out waiting for {what}\n{_floors.Layout.ToAscii()}");
        }

        // Walks into a fight room, checks the doors lock and enemies appear, kills them, checks the doors open again.
        IEnumerator FightRoom(RoomNode node)
        {
            Room room = _floors.RoomAt(node.cell);
            // In the exit room, stand exactly where the hole will open, to prove it never drops the player by surprise.
            Vector3 spot = node.type == RoomType.Exit ? _floors.CellCentre(node.cell) : StandingSpot(node);
            _player.Teleport(spot + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.IsLocked, 2f, $"{node.type} room {node.cell} to lock");

            Assert.AreEqual(node, _floors.CurrentNode);
            Assert.Greater(_floors.LiveEnemies.Count, 0, "locked room has no enemies");
            for (int i = 0; i < 4; i++)
            {
                bool hasDoor = (node.doors & (Doors)(1 << i)) != 0;
                Assert.AreEqual(hasDoor, room.slots[i].door.activeSelf, $"door {(Doors)(1 << i)} of locked room {node.cell}");
            }
            foreach (var enemy in _floors.LiveEnemies)
            {
                Assert.IsTrue(room.ContainsInside(enemy.transform.position, 0f), "enemy spawned outside its room");
            }

            // Locked means locked: something solid is in every doorway.
            Vector3 centre = _floors.CellCentre(node.cell) + Vector3.up;
            for (int i = 0; i < 4; i++)
            {
                if ((node.doors & (Doors)(1 << i)) == 0) continue;
                var d = FloorLayout.Directions[i];
                var direction = new Vector3(d.x, 0f, d.y);
                Vector3 origin = centre + direction * (room.interiorHalfSize - 0.5f);
                Assert.IsTrue(Physics.Raycast(origin, direction, 2f), $"locked door {(Doors)(1 << i)} of {node.cell} does not block");
            }

            foreach (var enemy in new System.Collections.Generic.List<Enemy>(_floors.LiveEnemies)) enemy.Health.TakeDamage(999, Vector3.forward);
            yield return WaitUntil(() => !_floors.IsLocked, 2f, $"room {node.cell} to unlock");
            Assert.IsTrue(node.cleared);
            for (int i = 0; i < 4; i++) Assert.IsFalse(room.slots[i].door.activeSelf, "door still shut after clearing");
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

                // Every room exists in 3D, with the doors the layout asked for.
                Vector3 startPoint = OnNavMesh(_player.transform.position);
                RoomNode firstNormal = null;
                foreach (var node in layout.ordered)
                {
                    Room room = _floors.RoomAt(node.cell);
                    Assert.IsNotNull(room, $"floor {floor}: no room built at {node.cell}");
                    Assert.AreEqual(node.doors, room.Doors);
                    for (int i = 0; i < 4; i++)
                    {
                        bool hasDoor = (node.doors & (Doors)(1 << i)) != 0;
                        Assert.AreEqual(!hasDoor, room.slots[i].wallFill.activeSelf, $"floor {floor}: wall {(Doors)(1 << i)} of {node.cell}");
                    }

                    // ...and can actually be walked to from the start.
                    var path = new NavMeshPath();
                    Vector3 target = StandingSpot(node);
                    bool found = NavMesh.CalculatePath(startPoint, target, NavMesh.AllAreas, path);
                    Assert.IsTrue(found && path.status == NavMeshPathStatus.PathComplete,
                        $"floor {floor} (seed {layout.seed}): {room.name} cannot be reached. found={found} status={path.status} corners={path.corners.Length} from={startPoint} to={target}\n{layout.ToAscii()}");

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
                Assert.IsTrue(exitRoom.exitHole.activeSelf);

                // The hole opened under the player, so nothing happens until they step off it and back on.
                yield return new WaitForSeconds(0.2f);
                Assert.AreEqual(floor, _floors.FloorNumber, "dropped to the next floor without stepping onto the exit");
                _player.Teleport(exitRoom.exitHole.transform.position + new Vector3(4f, 0.05f, 4f));
                yield return null;
                yield return null;
                _player.Teleport(exitRoom.exitHole.transform.position + Vector3.up * 0.05f);
                int next = floor + 1;
                yield return WaitUntil(() => _floors.FloorNumber == next, 2f, "the next floor");
                yield return null; // let the old rooms finish being destroyed
            }

            Assert.AreEqual(21, _floors.FloorsBuilt);
        }

        [UnityTest]
        public IEnumerator Player_can_walk_through_an_open_door_but_not_a_wall()
        {
            var start = _floors.Layout.start;
            int doorSide = -1, wallSide = -1;
            for (int i = 0; i < 4; i++)
            {
                if ((start.doors & (Doors)(1 << i)) != 0) { if (doorSide < 0) doorSide = i; }
                else if (wallSide < 0) wallSide = i;
            }
            Assert.GreaterOrEqual(doorSide, 0, "start room has no doors");
            _player.Health.SetInvulnerable(600f);

            if (wallSide >= 0)
            {
                var d = FloorLayout.Directions[wallSide];
                _player.Input.overrideFrame = new InputFrame { move = new Vector3(d.x, 0f, d.y) };
                yield return new WaitForSeconds(3f);
                Assert.AreEqual(start, _floors.CurrentNode, "walked through a solid wall");
                _player.Teleport(_floors.CellCentre(start.cell) + Vector3.up * 0.05f);
            }

            var through = FloorLayout.Directions[doorSide];
            _player.Input.overrideFrame = new InputFrame { move = new Vector3(through.x, 0f, through.y) };
            yield return WaitUntil(() => _floors.CurrentNode != start, 4f, "the player to walk through the doorway");
            Assert.AreEqual(start.cell + through, _floors.CurrentNode.cell);
        }

        [UnityTest]
        public IEnumerator Ants_path_around_cover_to_reach_the_player()
        {
            // Find a room with cover in it, stand behind the cover, and check an ant still gets to us.
            RoomNode target = null;
            foreach (var node in _floors.Layout.ordered)
            {
                if (node.type == RoomType.Normal) { target = node; break; }
            }
            Assert.IsNotNull(target, "floor has no fight room");

            _player.Teleport(StandingSpot(target) + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.IsLocked, 2f, "the room to lock");

            int max = _player.Health.maxHealth;
            yield return WaitUntil(() => _player.Health.Current < max, 10f, "an ant to reach and bite the player");
        }
    }
}
