using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The secret ending: the door that turns up with the last page, the bottom, and Dr. Finch.</summary>
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
        public IEnumerator The_door_is_not_there_until_every_page_is_found()
        {
            SaveSystem.Data.meta.runsStarted = 5;
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
            Assert.That(WorldPrompt.For(door.transform.position), Does.Contain("not on the floor plan"));
        }

        [UnityTest]
        public IEnumerator The_door_leads_to_Finch_and_beating_them_leads_home()
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
            yield return WaitUntil(() => floors.Boss != null, 5f, "Finch");
            Assert.IsInstanceOf<FinchBoss>(floors.Boss);
            var finch = (FinchBoss)floors.Boss;
            Assert.AreEqual(1, floors.LiveEnemies.Count);
            yield return WaitUntil(() => Time.timeScale > 0f, 8f, "the title card to pass");
            Time.timeScale = 3f;

            // They shoot in bursts.
            yield return WaitUntil(() => finch.Bursts > 0, 8f, "a burst");

            // First dose: stands still and shrinks, then comes out smaller with company.
            finch.Health.TakeDamage(Mathf.CeilToInt(finch.Health.maxHealth * 0.36f), Vector3.forward);
            yield return WaitUntil(() => finch.Current == FinchBoss.State.Dose, 2f, "the first dose");
            Assert.AreEqual(2, finch.Phase);
            Assert.AreEqual(RadioMoment.FinchDose, Radio.Instance.LastMoment);
            yield return WaitUntil(() => finch.Current != FinchBoss.State.Dose, 6f, "the dose to finish");
            Assert.Less(finch.BaseScale, 0.75f, "no smaller after a dose");
            Assert.Greater(floors.LiveEnemies.Count, 1, "nobody came to help");

            // Flasks after the first dose.
            yield return WaitUntil(() => finch.FlasksThrown > 0, 10f, "a flask");

            // Second dose.
            finch.Health.TakeDamage(Mathf.CeilToInt(finch.Health.maxHealth * 0.33f), Vector3.forward);
            yield return WaitUntil(() => finch.Doses == 2 && finch.Current != FinchBoss.State.Dose, 8f, "the second dose");
            Assert.AreEqual(3, finch.Phase);
            Assert.Less(finch.BaseScale, 0.5f);

            // Beaten: it is written down, the company leaves, and the only way on is up.
            finch.Health.Kill();
            yield return WaitUntil(() => floors.ExtractPortal != null, 6f, "the way back up");
            Assert.AreEqual(1, SaveSystem.Data.meta.finchBeaten);
            Assert.IsTrue(Achievements.Has(AchievementId.PerformanceReview));
            Assert.IsTrue(Achievements.StyleUnlocked("Lab Coat"));
            Assert.IsNull(floors.DeeperPortal, "there is something under the bottom");
            bool dose = false;
            foreach (var pickup in Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None)) dose |= pickup.kind == PickupKind.Item && pickup.item.type == ItemType.FinchsDose;
            Assert.IsTrue(dose, "Finch left nothing in the flask");
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
            Assert.That(WorldPrompt.For(again.transform.position), Does.Contain("only smaller"));

            // Back in the lab another day: no second ending.
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            Assert.AreNotEqual(LabDirector.EndingHeading, ScreenCards.Instance.LastHeading, "the ending was shown twice");
        }
    }
}
