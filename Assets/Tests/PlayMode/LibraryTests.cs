using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The Library: the first world that is not in the house, and the Librarian, whose fight is about when not to shoot.</summary>
    public class LibraryTests
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
        public IEnumerator The_library_is_behind_the_ice_maker_and_full_of_its_own_things()
        {
            BiomeDefinition library = null;
            foreach (var biome in _floors.AllBiomes()) if (biome.id == "library") library = biome;
            Assert.IsNotNull(library, "there is no library");
            Assert.IsFalse(Achievements.BiomeUnlocked("library"), "the library is open before the Ice Maker is beaten");
            Achievements.Unlock(AchievementId.Defrosted);
            Assert.IsTrue(Achievements.BiomeUnlocked("library"));

            Assert.IsTrue(_floors.SetBiome("library"));
            Assert.AreEqual(7, library.spawnTable.Length);
            foreach (var entry in library.spawnTable) Assert.IsNotNull(entry.prefab, "the library has an empty place at the table");
            Assert.IsNotNull(library.portalProp);
            Assert.That(ScreenCards.LibraryLines, Does.Contain(ScreenCards.TipFor("library", 4)));

            // A late floor: every room that fights is fought by things that live here.
            _floors.BuildFloor(9);
            yield return null;
            RoomNode fight = null;
            foreach (var node in _floors.Layout.ordered) if (node.type == RoomType.Normal && fight == null) fight = node;
            _player.Teleport(_floors.RoomAt(fight.cell).Centre + new Vector3(RoomShape.TileSize, 0.05f, 0f));
            yield return WaitUntil(() => _floors.IsLocked && _floors.LiveEnemies.Count > 0, 3f, "a fight");
            // One with a sequel: put down, two silverfish turn up where it fell, and they have no sequel of their own.
            MutationDefinition sequel = library.Mutation(MutationType.Sequel);
            Assert.IsNotNull(sequel, "the library has no Sequel mutation");
            Enemy author = _floors.LiveEnemies[0];
            author.Mutations.Apply(new System.Collections.Generic.List<MutationDefinition> { sequel });
            int alive = 0;
            foreach (var enemy in _floors.LiveEnemies) if (enemy.gameObject.activeSelf && !enemy.Health.IsDead) alive++;
            author.Health.Kill();
            yield return null;
            int after = 0;
            foreach (var enemy in _floors.LiveEnemies) if (enemy.gameObject.activeSelf && !enemy.Health.IsDead) after++;
            Assert.AreEqual(alive - 1 + EnemyMutations.SequelCount, after, "the sequel never came out");
            Assert.IsTrue(_floors.IsLocked, "the room opened with the sequel still in it");

            foreach (var enemy in _floors.LiveEnemies)
            {
                if (enemy.Health.IsDead) continue;
                bool local = false;
                foreach (var entry in library.spawnTable) local |= enemy.name.StartsWith(entry.prefab.name);
                Assert.IsTrue(local, enemy.name + " does not live in the library");
            }
        }

        [UnityTest]
        public IEnumerator Keeping_quiet_all_fight_is_noticed()
        {
            Assert.IsTrue(_floors.SetBiome("library"));
            yield return MeetBoss(10);
            var librarian = (LibrarianBoss)_floors.Boss;
            Assert.AreEqual(0, librarian.Punished);
            librarian.Health.Kill();
            yield return null;
            Assert.IsTrue(Achievements.Has(AchievementId.QuietPlease));
            Assert.IsTrue(Achievements.StyleUnlocked("Ink"));

            // And the trainee, who cannot shush, earns nobody anything for it.
            SaveSystem.Data.meta.achievements.Remove((int)AchievementId.QuietPlease);
            yield return MeetBoss(5);
            _floors.Boss.Health.Kill();
            yield return null;
            Assert.IsFalse(Achievements.Has(AchievementId.QuietPlease));
        }

        [UnityTest]
        public IEnumerator The_trainee_throws_books_and_never_shushes()
        {
            Assert.IsTrue(_floors.SetBiome("library"));
            yield return MeetBoss(5);
            Assert.IsInstanceOf<LibrarianBoss>(_floors.Boss);
            var trainee = (LibrarianBoss)_floors.Boss;
            Assert.AreEqual("The Trainee", trainee.displayName);
            Time.timeScale = 3f;
            yield return WaitUntil(() => trainee.BooksThrown >= 2, 10f, "books");
            Assert.Greater(GroundHazard.All.Count, 0, "a book came with no mark on the floor");
            yield return new WaitForSeconds(20f);
            Assert.AreEqual(0, trainee.Shushes, "the trainee has been shushing");
        }

        [UnityTest]
        public IEnumerator The_librarian_punishes_shooting_in_the_quiet_and_nothing_else()
        {
            Assert.IsTrue(_floors.SetBiome("library"));
            yield return MeetBoss(10);
            var librarian = (LibrarianBoss)_floors.Boss;
            Assert.AreEqual("The Librarian", librarian.displayName);
            Assert.IsFalse(Weapons.Unlocked(WeaponKind.SprayBottle), "the Spray Bottle is issued before the Librarian is beaten");
            Assert.IsTrue(librarian.canShush);
            var hud = Object.FindAnyObjectByType<Hud>();
            Time.timeScale = 3f;

            // Shooting outside the quiet costs nothing.
            _player.weapon.Tick(true, Vector3.forward);
            yield return null;
            Assert.AreEqual(0, librarian.Punished);

            // The warning comes first, and says what to do.
            yield return WaitUntil(() => librarian.Warning, 30f, "a shush");
            Assert.That(hud.LastToast, Does.Contain("SHH"));
            Assert.IsFalse(librarian.IsQuiet);

            // A quiet kept: no books for it.
            yield return WaitUntil(() => librarian.IsQuiet, 5f, "the quiet");
            yield return WaitUntil(() => !librarian.IsQuiet, 10f, "the quiet to end");
            Assert.AreEqual(0, librarian.Punished, "a book was thrown at somebody who kept quiet");
            Assert.That(hud.LastToast, Does.Contain("may shoot"));

            // A quiet broken: a book for it.
            yield return WaitUntil(() => librarian.IsQuiet, 40f, "the next quiet");
            int marks = GroundHazard.All.Count;
            for (float t = 0f; t < 1.5f && librarian.Punished == 0; t += Time.deltaTime)
            {
                _player.weapon.Tick(true, Vector3.forward);
                yield return null;
            }
            Assert.Greater(librarian.Punished, 0, "shooting in the quiet cost nothing");
            Assert.Greater(GroundHazard.All.Count, marks);

            // Two thirds: quicker, and overdue notices arrive.
            librarian.Health.TakeDamage(Mathf.CeilToInt(librarian.Health.maxHealth * 0.36f), Vector3.forward);
            yield return WaitUntil(() => librarian.Phase == 2, 3f, "the second phase");
            yield return null;
            Assert.Greater(_floors.LiveEnemies.Count, 1, "nobody was sent for");

            // Beaten: the portals, a jar, and no part of the machine (the machine has its four).
            librarian.Health.Kill();
            yield return WaitUntil(() => _floors.ExtractPortal != null, 6f, "the portals");
            Time.timeScale = 1f;
            Assert.IsTrue(Specimens.Has("TheLibrarian"));
            // This fight broke the quiet, so no prize for manners.
            Assert.IsFalse(Achievements.Has(AchievementId.QuietPlease));
            // And what the lab issues for it: a fourth weapon, twice as quick and half as far.
            Assert.IsTrue(Weapons.Unlocked(WeaponKind.SprayBottle), "beating the Librarian issued nothing");
            WeaponInfo spray = Weapons.Info(WeaponKind.SprayBottle);
            Assert.Less(spray.intervalMultiplier, 0.6f);
            Assert.Less(spray.rangeMultiplier, 0.6f);
            Assert.AreEqual(0, MachineParts.Count);
        }
    }
}
