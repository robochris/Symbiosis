using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The ending: the door that turns up with the last page, the bottom, and the cat Dr. Finch is hiding from.</summary>
    public class FinchTests
    {
        [SetUp]
        public void Fresh()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            Radio.Forget();
            Radio.ChanceOverride = 0f;
            RunDirector.Descent = false;
            RunDirector.LastRunMessage = null;
        }

        [TearDown]
        public void CleanUp()
        {
            SaveSystem.Wipe();
            Radio.Forget();
            Radio.ChanceOverride = null;
            RunDirector.Descent = false;
            RunDirector.LastRunMessage = null;
            Time.timeScale = 1f;
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        [UnityTest]
        public IEnumerator The_cat_flap_is_not_there_until_every_page_is_found()
        {
            // All four parts of the machine and a page missing: the parts are not the key to this door.
            SaveSystem.Data.meta.runsStarted = 5;
            foreach (var part in MachineParts.All) SaveSystem.Data.meta.parts.Add(part.boss);
            for (int i = 0; i < FinchNotes.All.Length - 1; i++) FinchNotes.Collect(FinchNotes.All[i]);
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var door = Object.FindAnyObjectByType<HiddenDoor>(FindObjectsInactive.Include);
            Assert.IsNotNull(door, "the lab has no hidden door");
            Assert.IsFalse(door.gameObject.activeInHierarchy, "the door is there with a page still missing");

            FinchNotes.Collect(FinchNotes.All[FinchNotes.All.Length - 1]);
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            door = Object.FindAnyObjectByType<HiddenDoor>(FindObjectsInactive.Include);
            Assert.IsTrue(door.gameObject.activeInHierarchy);
            Assert.AreEqual(RadioMoment.Door, Radio.Instance.LastMoment, "Finch did not deny the door");
            Assert.That(WorldPrompt.For(door.transform.position), Does.Contain("cat flap").And.Contain("not on the floor plan"));
        }

        [UnityTest]
        public IEnumerator The_door_leads_to_the_cat_and_boring_it_leads_home()
        {
            // A run is saved part-way: the trip down must leave it exactly as it is.
            foreach (var note in FinchNotes.All) FinchNotes.Collect(note);
            SaveSystem.Data.meta.runsStarted = 4;
            SaveSystem.Data.run.active = true;
            SaveSystem.Data.run.floorNumber = 4;
            SaveSystem.Data.run.depth = 1;
            SaveSystem.Data.run.biome = "ants";
            SaveSystem.Data.run.seed = 1234;
            SaveSystem.Data.run.health = 3;
            SaveSystem.Save();

            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var door = Object.FindAnyObjectByType<HiddenDoor>();
            Assert.IsNotNull(door);
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            yield return null;
            player.Teleport(door.transform.position + Vector3.up * 0.05f);
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == "Run" && FloorManager.Instance != null && FloorManager.Instance.Layout != null, 5f, "the bottom to load");
            yield return null;

            var floors = FloorManager.Instance;
            var director = RunDirector.Instance;
            player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            Assert.IsTrue(director.IsDescent);
            Assert.IsTrue(floors.biome.isBottom, "the door led to " + floors.biome.displayName);
            Assert.AreEqual(director.descentItems, player.Items.Collected.Count, "no kit for the way down");
            Assert.AreEqual(RadioMoment.Bottom, Radio.Instance.LastMoment);
            foreach (var node in floors.Layout.ordered)
            {
                if (node.type == RoomType.Normal) Assert.IsTrue(node.cleared, "something is waiting in a room at the bottom");
            }
            Assert.AreEqual(4, SaveSystem.Data.run.floorNumber, "the saved run was overwritten");

            // The end of the floor: Finch, and nobody else.
            player.Teleport(floors.ArenaCentre + Vector3.up * 0.05f);
            yield return WaitUntil(() => floors.Boss != null, 5f, "the cat");
            Assert.IsInstanceOf<CatBoss>(floors.Boss);
            var cat = (CatBoss)floors.Boss;
            Assert.AreEqual("The Cat", cat.displayName);
            Assert.AreEqual(1, floors.LiveEnemies.Count);
            Assert.AreEqual(RadioMoment.FinchFight, Radio.Instance.LastMoment, "Finch said nothing about the cat");
            yield return WaitUntil(() => Time.timeScale > 0f, 8f, "the title card to pass");
            Time.timeScale = 3f;

            // It bats at where the player stands, and now and then pounces. Both are warned of on the floor.
            player.Teleport(cat.transform.position + new Vector3(0f, 0.05f, -5f));
            yield return WaitUntil(() => cat.Swipes > 0, 10f, "a paw");
            Assert.Greater(GroundHazard.All.Count, 0, "the paw came with no warning");
            yield return WaitUntil(() => cat.Pounces > 0, 20f, "a pounce");

            // First wash: it sits still, and gets up quicker and starts knocking things off shelves.
            yield return WaitUntil(() => cat.Current == CatBoss.State.Stalk || cat.Current == CatBoss.State.Swipe, 5f, "it to land");
            cat.Health.TakeDamage(Mathf.CeilToInt(cat.Health.maxHealth * 0.36f), Vector3.forward);
            yield return WaitUntil(() => cat.Current == CatBoss.State.Groom, 4f, "the first wash");
            Assert.AreEqual(2, cat.Phase);
            Assert.AreEqual(RadioMoment.FinchDose, Radio.Instance.LastMoment);
            Vector3 sat = cat.transform.position;
            yield return new WaitForSeconds(1f);
            Assert.Less((cat.transform.position - sat).magnitude, 0.3f, "it wandered off while washing");
            yield return WaitUntil(() => cat.Current != CatBoss.State.Groom, 8f, "the wash to finish");
            Assert.Greater(cat.SpeedMultiplier, 1f, "no quicker after a wash");
            yield return WaitUntil(() => cat.ThingsKnockedOff > 0, 12f, "something off a shelf");

            // Second wash.
            yield return WaitUntil(() => cat.Current == CatBoss.State.Stalk || cat.Current == CatBoss.State.Swipe, 5f, "it to land");
            cat.Health.TakeDamage(Mathf.CeilToInt(cat.Health.maxHealth * 0.33f), Vector3.forward);
            yield return WaitUntil(() => cat.Washes == 2 && cat.Current != CatBoss.State.Groom, 12f, "the second wash");
            Assert.AreEqual(3, cat.Phase);

            // Out of interest: it is not killed. It turns its back and walks off, and the only way on is up.
            Vector3 lost = cat.transform.position;
            cat.Health.Kill();
            yield return null;
            yield return null;
            CatLeaving leaving = CatBoss.LastSeen;
            Assert.IsNotNull(leaving, "the cat fell over instead of leaving");
            float start = (leaving.transform.position - player.transform.position).magnitude;
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(leaving != null, "it vanished on the spot");
            Assert.Greater(leaving.Walked, 2f, "it did not walk away");
            Assert.Greater((leaving.transform.position - player.transform.position).magnitude, start, "it left toward the player");
            yield return WaitUntil(() => floors.ExtractPortal != null, 6f, "the way back up");
            Assert.AreEqual(1, SaveSystem.Data.meta.finchBeaten);
            Assert.IsTrue(Achievements.Has(AchievementId.PerformanceReview));
            Assert.IsTrue(Achievements.StyleUnlocked("Lab Coat"));
            Assert.IsNull(floors.DeeperPortal, "there is something under the bottom");
            bool dose = false;
            foreach (var pickup in Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None)) dose |= pickup.kind == PickupKind.Item && pickup.item.type == ItemType.FinchsDose;
            Assert.IsTrue(dose, "Finch pushed nothing out of the wall");
            Assert.AreEqual("BACK UP", floors.ExtractPortal.GetComponentInChildren<PortalTag>().Shown);
            // Finch has the last word on being beaten, unless the flask has already been picked up, which gets one too.
            Assert.That(Radio.Instance.LastMoment, Is.EqualTo(RadioMoment.FinchDown).Or.EqualTo(RadioMoment.Dose));
            Time.timeScale = 1f;

            player.Teleport(floors.ExtractPortal.transform.position + new Vector3(5f, 0.05f, 0f));
            yield return null;
            yield return null;
            player.Teleport(floors.ExtractPortal.transform.position + Vector3.up * 0.05f);
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == "Lab" && Radio.Instance != null && Radio.Instance.SaidCount > 0, 5f, "the lab");
            yield return null;

            Assert.AreEqual(RadioMoment.FinchHome, Radio.Instance.LastMoment);
            // The ending, once.
            Assert.AreEqual(LabDirector.EndingHeading, ScreenCards.Instance.LastHeading);
            Assert.IsTrue(ScreenCards.Instance.CardShowing, "no ending card");
            Assert.IsTrue(SaveSystem.Data.meta.endingSeen);
            Assert.That(Object.FindAnyObjectByType<Hud>().LastToast, Does.Contain("bottom"));
            Assert.IsTrue(SaveSystem.Data.run.active, "the saved run was thrown away");
            Assert.AreEqual(4, SaveSystem.Data.run.floorNumber);
            var again = Object.FindAnyObjectByType<HiddenDoor>();
            Assert.That(WorldPrompt.For(again.transform.position), Does.Contain("cats do"));

            // The time clock is there now. Clocked in, the next run starts a depth down and is worth more.
            var clock = Object.FindAnyObjectByType<TimeClock>();
            Assert.IsNotNull(clock, "no time clock after the ending");
            Assert.IsFalse(TimeClock.On);
            Assert.That(WorldPrompt.For(clock.transform.position), Does.Contain("ordinary hours"));
            Assert.IsTrue(clock.Toggle());
            Assert.AreEqual(RadioMoment.Overtime, Radio.Instance.LastMoment, "Finch had nothing to say about overtime");
            Assert.That(WorldPrompt.For(clock.transform.position), Does.Contain("on overtime"));
            SaveSystem.Data.run = new RunSave();
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            Assert.AreEqual(TimeClock.OvertimeDepth, FloorManager.Instance.depth, "an overtime run did not start a depth down");
            Assert.AreEqual(RadioMoment.RunStart, Radio.Instance.LastMoment, "an overtime run should still open like a new run");
            Assert.AreEqual(1, FloorManager.Instance.FloorNumber);

            // Back in the lab another day: no second ending.
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            Assert.AreNotEqual(LabDirector.EndingHeading, ScreenCards.Instance.LastHeading, "the ending was shown twice");
        }
    }
}
