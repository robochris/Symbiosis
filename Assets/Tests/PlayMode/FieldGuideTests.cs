using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The field guide: each kind named on first meeting, once, and kept in the book in the office.</summary>
    public class FieldGuideTests
    {
        [SetUp]
        public void Fresh()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            FieldGuideWatcher.Quiet = false;
        }

        [TearDown]
        public void CleanUp()
        {
            FieldGuideWatcher.Quiet = true;
            SaveSystem.Wipe();
        }

        [UnityTest]
        public IEnumerator Every_creature_has_a_note_and_a_first_meeting_is_named_once()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var floors = FloorManager.Instance;
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);

            // Every creature in every world's table has a note of its own.
            var missing = new List<string>();
            foreach (var biome in floors.AllBiomes())
            {
                foreach (var entry in biome.spawnTable)
                {
                    if (entry.prefab == null) continue;
                    string key = FieldGuide.Key(entry.prefab);
                    if (!FieldGuide.Notes.ContainsKey(key)) missing.Add(key);
                }
            }
            Assert.IsEmpty(missing, "no note for: " + string.Join(", ", missing));

            // A fight room: what is in it gets named, one at a time, and goes in the book.
            Assert.AreEqual(0, FieldGuide.SeenCount);
            RoomNode fight = null;
            foreach (var node in floors.Layout.ordered) if (node.type == RoomType.Normal && fight == null) fight = node;
            player.Teleport(floors.RoomAt(fight.cell).Centre + new Vector3(RoomShape.TileSize, 0.05f, 0f));
            for (float t = 0f; t < 3f && !floors.IsLocked; t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(FieldGuide.SeenCount, 0, "nothing was put in the field guide");
            Assert.That(Object.FindAnyObjectByType<Hud>().LastToast, Does.StartWith("New: "));
            int seen = FieldGuide.SeenCount;

            // The same kinds again: nothing new.
            foreach (var enemy in new List<Enemy>(floors.LiveEnemies)) enemy.Health.Kill();
            floors.BuildFloor(1);
            yield return null;
            Assert.AreEqual(seen, FieldGuide.SeenCount, "an old acquaintance was counted again");
        }

        [UnityTest]
        public IEnumerator The_book_in_the_office_reads_back_what_has_been_met()
        {
            SaveSystem.Data.meta.creaturesSeen.Add("WorkerAnt");
            SaveSystem.Data.meta.creaturesSeen.Add("StagBeetle");
            SceneManager.LoadScene("Lab");
            FieldGuideBook book = null;
            for (float t = 0f; t < 3f && book == null; t += Time.unscaledDeltaTime)
            {
                yield return null;
                book = Object.FindAnyObjectByType<FieldGuideBook>();
            }
            if (book == null)
            {
                var hidden = Object.FindAnyObjectByType<FieldGuideBook>(FindObjectsInactive.Include);
                string where = hidden == null ? "not in the scene at all" : $"switched off (self {hidden.gameObject.activeSelf}, parent {(hidden.transform.parent != null ? hidden.transform.parent.name : "none")})";
                Assert.Fail("no field guide in the office: " + where + ", scene " + SceneManager.GetActiveScene().name);
            }
            Assert.That(WorldPrompt.For(book.transform.position), Does.Contain("2 of " + FieldGuide.Total));
            Assert.That(book.Turn(), Does.StartWith("Worker Ant"));
            Assert.That(book.Turn(), Does.StartWith("Stag Beetle"));
            Assert.That(book.Turn(), Does.StartWith("Worker Ant"), "the book did not go round again");
        }
    }
}
