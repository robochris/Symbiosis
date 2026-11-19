using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>
    /// A bot plays whole floors on foot, the way a person would: out of the start room, down the tunnels, through
    /// every doorway, fighting whatever locks it in, until every room is cleared. Nothing is teleported, so this is
    /// what catches a doorway that can't be walked through, an ant that can't be reached, or a fight that never ends.
    /// </summary>
    public class SoakTests
    {
        PlayerController _player;
        FloorManager _floors;
        readonly NavMeshPath _path = new NavMeshPath();
        float _strafe = 1f, _nextSwap;

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
            // Top-down input: the bot aims by direction, with no camera to turn.
            CameraFollow.Instance.SetMode(CameraMode.TopDown);
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            GameSettings.Reset();
            SaveSystem.Wipe();
        }

        string Where(RoomNode node) =>
            $"floor {_floors.FloorNumber} (seed {_floors.Layout.seed}), {node.type} room {node.cell}, {RoomDressing.NameOf(_floors.RoomAt(node.cell).Shape.feature)}\n" +
            $"{_floors.Layout.ToAscii()}\n{_floors.RoomAt(node.cell).Shape.ToAscii()}";

        // Who is still standing, and why the bot might not be able to finish them.
        string Survivors()
        {
            var text = new System.Text.StringBuilder();
            foreach (var enemy in Enemy.Active)
            {
                string mutations = "";
                if (enemy.Mutations != null) foreach (var mutation in enemy.Mutations.Active) mutations += " " + mutation.type;
                bool onMesh = NavMesh.SamplePosition(enemy.transform.position, out var hit, 1.5f, NavMesh.AllAreas);
                text.AppendLine($"  {enemy.name}{(enemy.IsElite ? " ELITE" : "")} health {enemy.Health.Current}/{enemy.Health.maxHealth} at {enemy.transform.position}" +
                                $" (player at {_player.transform.position}) mutations:{mutations} targetable={enemy.Targetable} active={enemy.gameObject.activeSelf}" +
                                $" onNavMesh={onMesh} inSight={CanSee(enemy)} sinceHit={Time.time - enemy.Health.LastDamageTime:F1}s");
            }
            return text.ToString();
        }

        static Vector3 OnNavMesh(Vector3 near) => NavMesh.SamplePosition(near, out var hit, 3f, NavMesh.AllAreas) ? hit.position : near;

        Vector3 NextStep(Vector3 destination)
        {
            Vector3 position = _player.transform.position;
            if (NavMesh.CalculatePath(position, destination, NavMesh.AllAreas, _path) && _path.corners.Length > 1) return _path.corners[1];
            return destination;
        }

        bool CanSee(Enemy enemy)
        {
            Vector3 from = _player.transform.position + Vector3.up * 0.7f;
            Vector3 to = enemy.transform.position + Vector3.up * 0.7f;
            foreach (var hit in Physics.RaycastAll(from, (to - from).normalized, (to - from).magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<Health>() == null) return false;
            }
            return true;
        }

        // One frame of fighting: shoot the nearest ant in sight while circling it; walk round cover to one that isn't.
        void FightStep()
        {
            Vector3 position = _player.transform.position;
            Enemy nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (var enemy in Enemy.Active)
            {
                if (enemy.Health.IsDead || !enemy.Targetable || !enemy.gameObject.activeSelf) continue;
                float d = (enemy.transform.position - position).magnitude;
                if (d < nearestDistance) { nearestDistance = d; nearest = enemy; }
            }
            if (nearest == null)
            {
                _player.Input.overrideFrame = new InputFrame();
                return;
            }

            Vector3 to = nearest.transform.position - position;
            to.y = 0f;
            Vector3 aim = to.normalized;
            Vector3 move;
            if (CanSee(nearest))
            {
                if (Time.time >= _nextSwap)
                {
                    _nextSwap = Time.time + Random.Range(1f, 2.5f);
                    _strafe = -_strafe;
                }
                move = Vector3.Cross(Vector3.up, aim) * _strafe;
                if (nearestDistance < 5f) move -= aim * 1.2f;
                else if (nearestDistance > 10f) move += aim;
            }
            else
            {
                move = NextStep(OnNavMesh(nearest.transform.position)) - position;
                move.y = 0f;
            }
            // Something in its face that it can't hurt (a shell, or a wall at its back): roll out sideways, as a person would,
            // and come round behind it.
            bool pinned = nearestDistance < 3f && Time.time - nearest.Health.LastDamageTime > 2.5f;
            bool dodge = pinned && _player.DodgeCharges > 0 && Time.time >= _nextDodge;
            if (pinned) move = Vector3.Cross(Vector3.up, aim) * _strafe - aim * 0.3f;
            if (dodge)
            {
                _nextDodge = Time.time + 1.2f;
                _strafe = -_strafe;
            }
            _player.Input.overrideFrame = new InputFrame { move = move.normalized, aim = aim, fire = true, dodge = dodge };
        }

        float _nextDodge;

        // Walks to a room on foot, fighting through anything that locks on the way. Fails if it can't get there.
        IEnumerator GoTo(RoomNode node)
        {
            Room room = _floors.RoomAt(node.cell);
            Vector3 destination = OnNavMesh(room.Centre + new Vector3(RoomShape.TileSize, 0f, 0f));
            float walked = 0f, fought = 0f;
            Vector3 stuckAt = _player.transform.position;
            float stuckFor = 0f;

            while (true)
            {
                Vector3 position = _player.transform.position;
                if (_floors.IsLocked)
                {
                    fought += Time.deltaTime;
                    if (fought > 150f) Assert.Fail("a fight that never ends: " + Enemy.Active.Count + " left\n" + Survivors() + Where(_floors.CurrentNode));
                    FightStep();
                    yield return null;
                    continue;
                }

                Vector3 left = destination - position;
                left.y = 0f;
                if (node.cleared && (left.magnitude < 0.8f || node == _floors.Layout.exit)) break; // never walk on over the way down

                // Standing on the spot in a room that should have locked: step further in.
                Vector3 next = left.magnitude < 0.8f ? room.Centre : NextStep(destination);
                Vector3 direction = next - position;
                direction.y = 0f;
                _player.Input.overrideFrame = new InputFrame { move = direction.normalized, aim = direction.normalized };

                walked += Time.deltaTime;
                if (walked > 90f) Assert.Fail("could not walk to the room\n" + Where(node));
                stuckFor += Time.deltaTime;
                if ((position - stuckAt).magnitude > 1f)
                {
                    stuckAt = position;
                    stuckFor = 0f;
                }
                if (stuckFor > 6f) Assert.Fail($"stuck at {position} on the way to the room\n" + Where(node));
                yield return null;
            }
            _player.Input.overrideFrame = new InputFrame();
        }

        IEnumerator PlayFloor(int floor, int seed)
        {
            _floors.BuildFloor(floor, seed);
            yield return null;
            _player.Health.SetInvulnerable(100000f);

            // Nearest rooms first, so the walk is the one a player would take.
            var order = new List<RoomNode>(_floors.Layout.ordered);
            // The exit goes last: once it is cleared the way down opens, and walking over it would end the floor.
            RoomNode exit = _floors.Layout.exit;
            order.Sort((a, b) => a == b ? 0 : a == exit ? 1 : b == exit ? -1 : a.distance.CompareTo(b.distance));
            foreach (var node in order)
            {
                if (node == _floors.Layout.start) continue;
                yield return GoTo(node);
                Assert.IsTrue(node.visited, "walked to a room without entering it\n" + Where(node));
                Assert.IsTrue(node.cleared, "left a room uncleared\n" + Where(node));
            }
            Assert.IsTrue(_floors.ExitOpen, $"floor {floor} (seed {seed}): every room cleared but the way down is shut");
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator A_bot_clears_three_whole_floors_on_foot()
        {
            Time.timeScale = 4f;
            yield return PlayFloor(2, 1001);
            yield return PlayFloor(5, 1002);
            yield return PlayFloor(8, 1003);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator A_bot_clears_two_Birthday_Cake_floors_on_foot()
        {
            Assert.IsTrue(_floors.SetBiome("cake"));
            Time.timeScale = 4f;
            yield return PlayFloor(4, 2001);
            yield return PlayFloor(5, 2002);
        }

        // Walks into the boss room of a floor and fights with no invulnerability, counting what lands.
        IEnumerator MeasureBoss(int floor, int seed, string label)
        {
            _floors.BuildFloor(floor, seed);
            yield return null;
            Room arena = _floors.RoomAt(_floors.Layout.exit.cell);
            Vector3 door = arena.Centre;
            for (int side = 0; side < 4; side++)
            {
                if (arena.DoorObject(side) == null) continue;
                Vector2Int d = FloorLayout.Directions[side];
                door = arena.Centre + new Vector3(d.x, 0f, d.y) * (RoomShape.Size * 0.5f * RoomShape.TileSize - 6f);
                break;
            }
            _player.Health.ClearInvulnerability();
            _player.Health.SetMaxHealth(999, true);
            _player.Teleport(OnNavMesh(door) + Vector3.up * 0.05f);

            int hits = 0, pools = 0;
            System.Action<int, Vector3> count = (amount, direction) => { hits += amount; if (direction == Vector3.zero) pools += amount; };
            _player.Health.Damaged += count;
            float started = Time.time;
            for (float t = 0f; t < 3f && !_floors.IsLocked; t += Time.deltaTime) yield return null;
            Assert.IsTrue(_floors.IsLocked, "the boss room did not lock");
            while (_floors.IsLocked && Time.time - started < 400f)
            {
                FightStep();
                yield return null;
            }
            _player.Health.Damaged -= count;
            _player.Input.overrideFrame = new InputFrame();
            Debug.Log($"[Shrunk] MEASURE {label}: {(_floors.IsLocked ? "NOT BEATEN" : "beaten")} in {Time.time - started:F0} s, {hits} hits taken ({pools} from pools and blasts)");
        }

        /// <summary>Not a test: how hard the two bosses are for a bot that strafes but never dodges. Read the log.</summary>
        [UnityTest, Explicit, Timeout(1800000)]
        public IEnumerator Measure_boss_difficulty()
        {
            // At normal speed: sped up, the bot misses far more and the numbers mean nothing.
            for (int i = 0; i < 2; i++) yield return MeasureBoss(5, 500 + i, "Brood Mother");
            for (int i = 0; i < 2; i++) yield return MeasureBoss(10, 900 + i, "Ant Queen");
        }

        /// <summary>The long version, by name only: ten floors in a row, new ones every time.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator A_bot_clears_a_whole_biome_on_foot()
        {
            Time.timeScale = 4f;
            for (int floor = 1; floor <= 10; floor++) yield return PlayFloor(floor, Random.Range(1, int.MaxValue));
        }

        /// <summary>The same in the Birthday Cake.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator A_bot_clears_a_whole_cake_biome_on_foot()
        {
            Assert.IsTrue(_floors.SetBiome("cake"));
            Time.timeScale = 4f;
            for (int floor = 1; floor <= 10; floor++) yield return PlayFloor(floor, Random.Range(1, int.MaxValue));
        }
    }
}
