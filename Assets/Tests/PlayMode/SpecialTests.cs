using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The machine and the cat, met inside a run: a room off a boss floor, a box in a shop, and the way back.</summary>
    public class SpecialTests
    {
        PlayerController _player;
        FloorManager _floors;

        [UnitySetUp]
        public IEnumerator LoadRun()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
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
            FloorManager.CatFlapChanceForAll = null;
            FloorManager.MachineRoomForAll = null;
            Time.timeScale = 1f;
            SaveSystem.Wipe();
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        IEnumerator BeatTheBoss()
        {
            _player.Teleport(_floors.ArenaCentre + new Vector3(0f, 0.05f, -9f));
            yield return WaitUntil(() => _floors.Boss != null, 6f, "the boss");
            yield return WaitUntil(() => Time.timeScale > 0f, 8f, "the title card to pass");
            _floors.Boss.Health.Kill();
            yield return WaitUntil(() => _floors.ExitOpen, 8f, "the way out to open");
        }

        FloorManager.BiomePortal BackToTheRun()
        {
            foreach (var portal in _floors.Portals) if (portal.resumeFloor > 0) return portal;
            return null;
        }

        [UnityTest]
        public IEnumerator The_cat_keeps_a_rare_box_in_the_shop_and_the_run_goes_on_after()
        {
            // Not before the notebook is complete, however many shops.
            for (int floor = 2; floor < 5; floor++)
            {
                _floors.BuildFloor(floor);
                yield return null;
                Assert.IsNull(_floors.CatFlap, "the cat's box turned up before the notebook was found");
            }

            FloorManager.CatFlapChanceForAll = 1f;
            _floors.BuildFloor(3);
            yield return null;
            Assert.IsNotNull(_floors.CatFlap, "no box in the shop");
            BiomeDefinition home = _floors.biome;
            int depth = _floors.depth;
            _player.Teleport(_floors.CatFlap.transform.position + new Vector3(0f, 0.05f, -3f));
            yield return null;
            _player.Teleport(_floors.CatFlap.transform.position + new Vector3(0f, 0.05f, -1f));
            yield return WaitUntil(() => _floors.InSpecial, 3f, "the flap to lead somewhere");
            Assert.AreEqual(_floors.bottomBiome, _floors.biome);

            yield return BeatTheBoss();
            var back = BackToTheRun();
            Assert.IsNotNull(back, "no way back to the run after the cat");
            Assert.IsNotNull(_floors.ExtractPortal, "no way home after the cat");
            Assert.AreEqual(4, back.resumeFloor, "the run should go on from the floor after the shop");

            _player.Teleport(back.gate.transform.position + new Vector3(0f, 0.05f, -4f));
            yield return null;
            _player.Teleport(back.gate.transform.position + Vector3.up * 0.05f);
            yield return WaitUntil(() => !_floors.InSpecial, 3f, "the way back");
            Assert.AreEqual(home, _floors.biome);
            Assert.AreEqual(4, _floors.FloorNumber);
            Assert.AreEqual(depth, _floors.depth);
        }

        [UnityTest]
        public IEnumerator The_machine_is_built_off_a_boss_floor_and_runs_once_the_boss_is_down()
        {
            // Not on a first world's boss floor, even with every part.
            foreach (var part in MachineParts.All) MachineParts.Take(part);
            Assert.IsTrue(MachineParts.Complete);
            _floors.BuildFloor(10);
            yield return null;
            Assert.IsNull(_floors.MachineRoom, "the machine turned up in the first world of a run");

            // A world in: there it is, off the map.
            _floors.WorldsBeaten = 1;
            _floors.BuildFloor(10);
            yield return null;
            var machine = _floors.MachineRoom;
            Assert.IsNotNull(machine, "no machine room on the second world's boss floor");
            RoomNode node = null;
            foreach (var each in _floors.Layout.ordered) if (each.type == RoomType.Machine) node = each;
            Assert.IsNotNull(node);
            Assert.AreEqual(1, node.DoorCount, "the machine is not in a dead end");

            _player.Teleport(machine.transform.position + new Vector3(3f, 0.05f, -2f));
            yield return WaitUntil(() => machine.Built, 3f, "the parts to go in");
            Assert.IsFalse(machine.Ready, "the machine runs before the boss is down");

            BiomeDefinition home = _floors.biome;
            int depth = _floors.depth;
            yield return BeatTheBoss();
            Assert.IsTrue(machine.Ready);
            _player.Teleport(machine.transform.position + new Vector3(3f, 0.05f, -2f));
            yield return null;
            _player.Teleport(machine.transform.position + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.InSpecial, 3f, "the machine's portal");
            Assert.AreEqual(_floors.machineBiome, _floors.biome);

            yield return BeatTheBoss();
            var back = BackToTheRun();
            Assert.IsNotNull(back, "no way back to the run after the machine");
            Assert.AreEqual(11, back.resumeFloor);
            Assert.AreEqual(depth + 1, back.resumeDepth, "from a boss floor the run should go on deeper");
            Assert.AreEqual(home, back.leadsTo);
        }
    }
}
