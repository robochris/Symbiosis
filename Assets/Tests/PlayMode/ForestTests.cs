using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The Forest, behind the Librarian, and the Toad: hops that are marked before they land, and a tongue to step out of.</summary>
    public class ForestTests
    {
        PlayerController _player;
        FloorManager _floors;

        [UnitySetUp]
        public IEnumerator LoadRun()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
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
        public void CleanUp()
        {
            Time.timeScale = 1f;
            Achievements.EverythingUnlocked = false;
            SaveSystem.Wipe();
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        IEnumerator MeetBoss(int floor)
        {
            _floors.BuildFloor(floor);
            yield return null;
            _player.Teleport(_floors.ArenaCentre + new Vector3(0f, 0.05f, -9f));
            yield return WaitUntil(() => _floors.Boss != null, 4f, "the boss");
            yield return WaitUntil(() => Time.timeScale > 0f, 8f, "the title card to pass");
        }

        [UnityTest]
        public IEnumerator The_forest_is_behind_the_librarian_and_full_of_its_own_things()
        {
            BiomeDefinition forest = null;
            foreach (var biome in _floors.AllBiomes()) if (biome.id == "forest") forest = biome;
            Assert.IsNotNull(forest, "there is no forest");
            Assert.IsFalse(Achievements.BiomeUnlocked("forest"), "the forest is open before the Librarian is beaten");

            // The Trainee is not enough: it takes the Librarian.
            Assert.IsTrue(_floors.SetBiome("library"));
            yield return MeetBoss(5);
            _floors.Boss.Health.Kill();
            yield return null;
            Assert.IsFalse(Achievements.BiomeUnlocked("forest"), "the Trainee opened the forest");
            yield return MeetBoss(10);
            _floors.Boss.Health.Kill();
            yield return null;
            Assert.IsTrue(Achievements.BiomeUnlocked("forest"));

            Assert.IsTrue(_floors.SetBiome("forest"));
            Assert.AreEqual(10, forest.spawnTable.Length);
            foreach (var entry in forest.spawnTable) Assert.IsNotNull(entry.prefab, "the forest has an empty place at the table");
            Assert.IsNotNull(forest.portalProp);
            Assert.IsFalse(forest.ceiling, "the forest has a roof on it");
            Assert.That(ScreenCards.ForestLines, Does.Contain(ScreenCards.TipFor("forest", 4)));

            _floors.BuildFloor(9);
            yield return null;
            RoomNode fight = null;
            foreach (var node in _floors.Layout.ordered) if (node.type == RoomType.Normal && fight == null) fight = node;
            _player.Teleport(_floors.RoomAt(fight.cell).Centre + new Vector3(RoomShape.TileSize, 0.05f, 0f));
            yield return WaitUntil(() => _floors.IsLocked && _floors.LiveEnemies.Count > 0, 3f, "a fight");
            foreach (var enemy in _floors.LiveEnemies)
            {
                if (enemy.Health.IsDead) continue;
                bool local = false;
                foreach (var entry in forest.spawnTable) local |= enemy.name.StartsWith(entry.prefab.name);
                Assert.IsTrue(local, enemy.name + " does not live in the forest");
            }
        }

        [UnityTest]
        public IEnumerator The_toad_marks_where_it_will_land_and_lands_near_the_player()
        {
            Assert.IsTrue(_floors.SetBiome("forest"));
            yield return MeetBoss(10);
            Assert.IsInstanceOf<ToadBoss>(_floors.Boss);
            var toad = (ToadBoss)_floors.Boss;
            Assert.AreEqual("The Toad", toad.displayName);
            float before = Vector3.Distance(toad.transform.position, _player.transform.position);

            yield return WaitUntil(() => toad.Airborne, 6f, "a hop");
            Assert.Greater(GroundHazard.All.Count, 0, "it jumped with no mark on the floor");
            yield return WaitUntil(() => !toad.Airborne, 3f, "the landing");
            float after = Vector3.Distance(toad.transform.position, _player.transform.position);
            Assert.Less(after, before - 3f, "the hop did not bring it closer");
        }

        [UnityTest]
        public IEnumerator The_tongue_drags_in_a_player_who_stays_on_the_line()
        {
            Assert.IsTrue(_floors.SetBiome("forest"));
            yield return MeetBoss(10);
            var toad = (ToadBoss)_floors.Boss;
            toad.hopRange = 0f; // hops on the spot, so only the tongue reaches
            _player.Health.ClearInvulnerability();
            int health = _player.Health.Current;
            Vector3 start = _player.transform.position;

            yield return WaitUntil(() => toad.Lashes >= 1, 12f, "the tongue");
            Assert.AreEqual(1, toad.Caught, "the tongue missed a player standing still on its line");
            Assert.Less(_player.Health.Current, health);
            yield return new WaitForSeconds(0.3f);
            Assert.Less(Vector3.Distance(toad.transform.position, _player.transform.position),
                Vector3.Distance(toad.transform.position, start) - 1f, "the catch did not drag the player in");

            toad.Health.Kill();
            yield return null;
            Assert.IsFalse(Achievements.Has(AchievementId.TongueTied), "caught, and still Tongue-Tied");
        }

        [UnityTest]
        public IEnumerator Stepping_off_the_line_once_it_stops_turning_is_enough()
        {
            Assert.IsTrue(_floors.SetBiome("forest"));
            yield return MeetBoss(10);
            var toad = (ToadBoss)_floors.Boss;
            toad.hopRange = 0f;
            _player.Health.ClearInvulnerability();

            yield return WaitUntil(() => toad.Current == ToadBoss.State.Aim, 10f, "the wind-up");
            yield return null;
            Assert.IsTrue(toad.ChargeLineShowing, "the tongue has no line on the floor");
            yield return new WaitForSeconds(toad.tongueAim - toad.tongueHold + 0.05f);
            _player.Teleport(_player.transform.position + toad.transform.right * 3f + Vector3.up * 0.05f);
            yield return WaitUntil(() => toad.Lashes >= 1, 3f, "the tongue");
            Assert.AreEqual(0, toad.Caught, "it caught a player who had stepped aside");

            toad.Health.Kill();
            yield return null;
            Assert.IsTrue(Achievements.Has(AchievementId.TongueTied));
            Assert.IsTrue(Achievements.StyleUnlocked("Moss"));
        }

        [UnityTest]
        public IEnumerator A_pond_slows_the_wader_and_once_the_room_is_clear_can_be_dived()
        {
            ForestPond.ChanceForAll = 1f;
            try
            {
                Assert.IsTrue(_floors.SetBiome("forest"));
                _floors.BuildFloor(3);
                yield return null;
                var pond = _floors.Pond;
                Assert.IsNotNull(pond, "no pond on a forest floor");

                // Into the room: it locks, and the pond only slows.
                _player.Teleport(pond.transform.position + new Vector3(-3f, 0.05f, -3f));
                yield return WaitUntil(() => _floors.IsLocked, 3f, "the room to lock");
                _player.Teleport(pond.transform.position + new Vector3(1f, 0.05f, 0f));
                yield return null;
                yield return null;
                Assert.IsTrue(_player.IsSlowed, "wading was no slower");
                Assert.IsFalse(pond.glint.activeSelf, "something to dive for while the room is still fighting");
                Assert.IsFalse(pond.Dived);

                foreach (var enemy in new System.Collections.Generic.List<Enemy>(_floors.LiveEnemies)) enemy.Health.Kill();
                yield return WaitUntil(() => !_floors.IsLocked, 3f, "the room to open");
                yield return null;
                Assert.IsTrue(pond.glint.activeSelf, "no glint once the room was clear");
                _player.Teleport(pond.transform.position + new Vector3(-4f, 0.05f, 0f));
                yield return null;
                _player.Teleport(pond.transform.position + Vector3.up * 0.05f);
                yield return WaitUntil(() => pond.Dived, 3f, "the dive");
                bool item = false;
                foreach (var pickup in Pickup.All) item |= pickup.kind == PickupKind.Item && Vector3.Distance(pickup.transform.position, pond.transform.position) < 5f;
                Assert.IsTrue(item, "nothing came up from the bottom");
            }
            finally
            {
                ForestPond.ChanceForAll = 0f;
            }
        }

        [UnityTest]
        public IEnumerator The_froglet_hops_but_has_no_tongue_yet()
        {
            Assert.IsTrue(_floors.SetBiome("forest"));
            yield return MeetBoss(5);
            Assert.IsInstanceOf<ToadBoss>(_floors.Boss);
            var froglet = (ToadBoss)_floors.Boss;
            Assert.AreEqual("The Froglet", froglet.displayName);
            Time.timeScale = 3f;
            yield return WaitUntil(() => froglet.Hops >= 3, 15f, "hops");
            Assert.AreEqual(0, froglet.Lashes, "the froglet has a tongue");
            froglet.Health.Kill();
            yield return null;
            Assert.IsFalse(Achievements.Has(AchievementId.TongueTied), "the froglet gave out Tongue-Tied");
        }
    }
}
