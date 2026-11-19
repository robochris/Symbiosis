using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>Dr. Finch's notebook: pages hidden in the biomes, and the evidence board they end up on.</summary>
    public class NoteTests
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
            Radio.ChanceOverride = 0f;
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
            Radio.Forget();
            Radio.ChanceOverride = null;
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator A_biome_gives_up_one_page_a_visit_in_order()
        {
            // Never on the first floor or a boss floor.
            Assert.IsNull(_floors.Note, "a page on floor 1");
            _floors.BuildFloor(5);
            yield return null;
            Assert.IsNull(_floors.Note, "a page on a boss floor");

            // Always on floor 9, tucked inside a fight room.
            _floors.BuildFloor(9);
            yield return null;
            NotePickup page = _floors.Note;
            Assert.IsNotNull(page, "no page on floor 9");
            Assert.AreEqual(1, page.note.id);
            Room room = _floors.RoomAt(_floors.CellOf(page.transform.position));
            Assert.IsNotNull(room);
            Assert.IsTrue(room.ContainsInside(page.transform.position, 0f), "the page is inside a wall");

            // Walking onto it takes it: it is read out, kept in the save, and Finch has something to say.
            var hud = Object.FindAnyObjectByType<Hud>();
            _player.Teleport(page.transform.position + Vector3.up * 0.05f);
            yield return null;
            yield return null;
            Assert.IsTrue(FinchNotes.Has(1));
            Assert.That(hud.LastToast, Does.Contain("Day 1."));
            Assert.AreEqual(RadioMoment.Note, Radio.Instance.LastMoment);
            SaveSystem.Load();
            Assert.AreEqual(1, FinchNotes.Count);

            // One page a visit: the same floor again has none, however the dice fall.
            _floors.noteChance = 1f;
            _floors.BuildFloor(9);
            yield return null;
            Assert.IsNull(_floors.Note, "a second page in the same visit");

            // A new visit (the next depth) gives the next page; a different biome keeps its own count.
            _floors.depth = 2;
            _floors.BuildFloor(4);
            yield return null;
            Assert.IsNotNull(_floors.Note);
            Assert.AreEqual(2, _floors.Note.note.id);
            Assert.AreEqual(4, FinchNotes.NextFor("cake").id);

            // With every page of a biome found, it has none left to hide.
            foreach (var note in FinchNotes.All) FinchNotes.Collect(note);
            _floors.depth = 3;
            _floors.BuildFloor(9);
            yield return null;
            Assert.IsNull(_floors.Note);
        }

        [UnityTest]
        public IEnumerator The_evidence_board_arrives_with_the_first_page()
        {
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var board = Object.FindAnyObjectByType<EvidenceBoard>(FindObjectsInactive.Include);
            Assert.IsNotNull(board, "the lab has no evidence board");
            Assert.IsFalse(board.gameObject.activeInHierarchy, "the board is there before there is anything to pin to it");

            FinchNotes.Collect(FinchNotes.Get(1));
            FinchNotes.Collect(FinchNotes.Get(2));
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            board = Object.FindAnyObjectByType<EvidenceBoard>(FindObjectsInactive.Include);
            Assert.IsTrue(board.gameObject.activeInHierarchy);
            Assert.IsTrue(board.pages[0].activeSelf && board.pages[1].activeSelf && !board.pages[2].activeSelf);

            // Walking up reads the newest page; stepping away and back turns to the next.
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            Vector3 at = board.transform.position + Vector3.up * 0.05f;
            Assert.That(WorldPrompt.For(at), Does.Contain("2 of 9 pages").And.Contain("Day 9."));
            Assert.IsNull(WorldPrompt.For(at + Vector3.left * 8f));
            Assert.That(WorldPrompt.For(at), Does.Contain("Day 1."));
        }
    }
}
