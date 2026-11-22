using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The Bookshelf, behind the Lawnmower: a second storey of ledges, shots that tilt to reach them, and the Encyclopedia.</summary>
    public class BookshelfTests
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
        public IEnumerator The_bookshelf_is_behind_the_lawnmower_and_has_a_second_storey()
        {
            BiomeDefinition shelf = null;
            foreach (var biome in _floors.AllBiomes()) if (biome.id == "bookshelf") shelf = biome;
            Assert.IsNotNull(shelf, "there is no bookshelf");
            Assert.IsTrue(shelf.ledges);
            Assert.IsFalse(Achievements.BiomeUnlocked("bookshelf"));
            Achievements.Unlock(AchievementId.LawnOrder);
            Assert.IsTrue(Achievements.BiomeUnlocked("bookshelf"));
            Assert.IsTrue(_floors.SetBiome("bookshelf"));
            Assert.That(ScreenCards.BookshelfLines, Does.Contain(ScreenCards.TipFor("bookshelf", 4)));

            // Ledges in the fight rooms, walkable on top, with a way up.
            int ledges = 0;
            for (int floor = 2; floor <= 4 && ledges == 0; floor++)
            {
                _floors.BuildFloor(floor);
                yield return null;
                ledges = Ledges.Count;
            }
            Assert.Greater(ledges, 0, "no ledges on three floors of the bookshelf");
            Ledge first = null;
            foreach (var each in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (each.name == "Ledge") { first = new Ledge { at = each.position }; break; }
            Assert.IsNotNull(first, "no ledge in the scene");
            Vector3 top = new Vector3(first.at.x, Ledges.Height, first.at.z);
            Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(top, out var hit, 0.6f, UnityEngine.AI.NavMesh.AllAreas), "nothing can stand on the ledge");
            Assert.Greater(hit.position.y, Ledges.Height - 0.4f);
            Assert.AreEqual(Ledges.Height, Ledges.SurfaceAt(top), 0.01f);

            // A mark put down on the floor at the ledge lands on its top; and only hurts up there.
            var blast = GroundHazard.SpawnBlast(new Vector3(top.x, 0f, top.z), 1f, 5f, null);
            Assert.Greater(blast.transform.position.y, Ledges.Height - 0.1f, "the mark went under the ledge");
            Object.Destroy(blast.gameObject);

            // Elsewhere, no ledges.
            Assert.IsTrue(_floors.SetBiome("shed"));
            _floors.BuildFloor(2);
            yield return null;
            Assert.AreEqual(0, Ledges.Count, "ledges outside the bookshelf");
        }

        class Ledge { public Vector3 at; }

        [UnityTest]
        public IEnumerator Shots_tilt_up_at_something_on_a_ledge()
        {
            Assert.IsTrue(_floors.SetBiome("bookshelf"));
            _floors.BuildFloor(2);
            yield return null;
            // Something to stand on, well above the player, and an ink pot on top of it.
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.transform.position = _player.transform.position + new Vector3(0f, 1.5f, 8f);
            block.transform.localScale = new Vector3(3f, 3f, 3f);
            Physics.SyncTransforms();
            var enemy = Object.Instantiate(_floors.biome.spawnTable[1].prefab, _player.transform.position + new Vector3(0f, 3.1f, 8f), Quaternion.identity);
            yield return null;
            yield return null;
            Assert.Greater(enemy.transform.position.y, _player.transform.position.y + 1f, "the enemy is not up on the block");
            Vector3 aim = Vector3.forward;
            int fired = _player.weapon.ShotsFired;
            for (int i = 0; i < 5; i++) { _player.weapon.Tick(true, aim); yield return null; }
            Assert.Greater(_player.weapon.ShotsFired, fired);
            bool rising = false;
            foreach (var bullet in Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None))
                if (bullet.gameObject.activeSelf && bullet.transform.forward.y > 0.1f) rising = true;
            Assert.IsTrue(rising, "no shot tilted up at the enemy above");
            Object.Destroy(enemy.gameObject);
            Object.Destroy(block);
        }

        [UnityTest]
        public IEnumerator The_encyclopedia_topples_along_its_marks_and_throws_pages()
        {
            Assert.IsTrue(_floors.SetBiome("bookshelf"));
            yield return MeetBoss(10);
            var tome = (BookBoss)_floors.Boss;
            Assert.AreEqual("The Encyclopedia", tome.displayName);
            Time.timeScale = 2f;
            yield return WaitUntil(() => tome.Topples >= 1, 12f, "a topple");
            Assert.Greater(GroundHazard.All.Count, 5, "a topple with no strip marked");
            yield return WaitUntil(() => tome.Fans >= 1, 15f, "pages");
            tome.Health.Kill();
            yield return null;
            Assert.IsTrue(Achievements.Has(AchievementId.Shelved));

            yield return MeetBoss(5);
            var bookend = (BookBoss)_floors.Boss;
            Assert.AreEqual("The Bookend", bookend.displayName);
            yield return WaitUntil(() => bookend.Topples >= 2, 15f, "topples");
            Assert.AreEqual(0, bookend.Fans, "the bookend threw pages");
        }
    }
}
