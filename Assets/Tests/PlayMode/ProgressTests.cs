using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>XP and levels: where XP comes from, and what each level hands out.</summary>
    public class ProgressTests
    {
        [UnitySetUp]
        public IEnumerator Fresh()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            Progress.LevelOverride = null;
            Progress.PerksOverride = null;
            yield return null;
        }

        [TearDown]
        public void CleanUp()
        {
            Progress.LevelOverride = Progress.TopLevel;
            Progress.PerksOverride = false;
            Time.timeScale = 1f;
            SaveSystem.Wipe();
        }

        [UnityTest]
        public IEnumerator Levels_come_quick_at_first_and_slower_later()
        {
            Assert.AreEqual(1, Progress.Level);
            Assert.AreEqual(0, Progress.XpFor(1));
            Assert.AreEqual(60, Progress.XpFor(2));
            Assert.Greater(Progress.ToNext(10), Progress.ToNext(2));
            Assert.AreEqual(1, Progress.LevelFor(59));
            Assert.AreEqual(2, Progress.LevelFor(60));
            for (int level = 1; level < 40; level++) Assert.AreEqual(level, Progress.LevelFor(Progress.XpFor(level)));

            // The ladder is one thing a level, in order, from level 2 to the top.
            for (int i = 0; i < Progress.Ladder.Length; i++) Assert.AreEqual(i + 2, Progress.Ladder[i].level);
            Assert.That(Progress.Summary(), Does.Contain("Level 1").And.Contain("0/60 XP").And.Contain("Rubber Band"));
            yield break;
        }

        [UnityTest]
        public IEnumerator The_ladder_holds_back_items_styles_and_perks_until_their_level()
        {
            Assert.IsFalse(Achievements.ItemUnlocked(ItemType.RubberBand));
            Assert.IsFalse(Achievements.StyleUnlocked("Sprout"));
            Assert.IsFalse(Progress.Has(Perk.LooseChange));
            Assert.IsTrue(Achievements.ItemUnlocked(ItemType.SugarCube), "an item off the ladder was held back");
            Assert.AreEqual(20, Progress.ShopPrice(20));

            int samples = SaveSystem.Data.meta.samples;
            Progress.Add(Progress.XpFor(5));
            Assert.AreEqual(5, Progress.Level);
            Assert.IsTrue(Achievements.ItemUnlocked(ItemType.RubberBand));
            Assert.IsTrue(Achievements.ItemUnlocked(ItemType.Sprinkles));
            Assert.IsFalse(Achievements.ItemUnlocked(ItemType.IceCube));
            Assert.IsTrue(Achievements.StyleUnlocked("Sprout"));
            Assert.IsTrue(Progress.Has(Perk.LooseChange));
            Assert.AreEqual(samples, SaveSystem.Data.meta.samples, "a ladder level paid samples");

            // Everything, then a level past the top: a box of samples.
            Progress.Add(Progress.XpFor(Progress.TopLevel) - Progress.Xp);
            Assert.AreEqual(Progress.TopLevel, Progress.Level);
            Assert.IsNull(Progress.Next);
            Assert.AreEqual(18, Progress.ShopPrice(20));
            Assert.IsTrue(Progress.Has(Perk.SecondOpinion));
            Progress.Add(Progress.ToNext(Progress.TopLevel));
            Assert.AreEqual(samples + Progress.SamplesPastTheTop, SaveSystem.Data.meta.samples);
            yield break;
        }

        [UnityTest]
        public IEnumerator A_run_earns_xp_for_going_down_for_bosses_and_for_getting_home()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            var floors = FloorManager.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            Assert.AreEqual(0, Progress.RunXp, "a new run started with XP on it");

            int before = Progress.Xp;
            floors.BuildFloor(2);
            yield return null;
            Assert.AreEqual(before + Progress.FloorXp, Progress.Xp, "going down a floor earned nothing");

            // A mini-boss and a boss.
            floors.BuildFloor(5);
            yield return null;
            int afterFloors = Progress.Xp;
            player.Teleport(floors.ArenaCentre + new Vector3(0f, 0.05f, -9f));
            for (float t = 0f; t < 4f && floors.Boss == null; t += Time.unscaledDeltaTime) yield return null;
            floors.Boss.Health.Kill();
            yield return null;
            Assert.AreEqual(afterFloors + Progress.MiniBossXp, Progress.Xp);

            floors.BuildFloor(10);
            yield return null;
            int afterMini = Progress.Xp;
            player.Teleport(floors.ArenaCentre + new Vector3(0f, 0.05f, -9f));
            for (float t = 0f; t < 4f && floors.Boss == null; t += Time.unscaledDeltaTime) yield return null;
            floors.Boss.Health.Kill();
            yield return null;
            Assert.AreEqual(afterMini + Progress.BossXp, Progress.Xp, "the boss was not worth a boss's XP");

            // Home: XP for that too, and the lab says how much the run was worth.
            int beforeHome = Progress.Xp;
            Object.FindAnyObjectByType<RunDirector>().Extract();
            Assert.AreEqual(beforeHome + Progress.ExtractXp, Progress.Xp);
            Assert.That(RunDirector.LastRunMessage, Does.Contain(Progress.RunXp + " XP"));
            Assert.AreEqual("HOME", RunDirector.LastSummaryHeading);
            Assert.That(RunDirector.LastSummary, Does.Contain("Floor 10").And.Contain("kills").And.Contain("+" + Progress.RunXp + " XP"));

            // The pause menu says what is being carried, and the level.
            var carried = PauseMenu.CarryingText();
            Assert.That(carried, Does.Contain("Level"));
            yield return new WaitForSecondsRealtime(1.5f);
        }

        [UnityTest]
        public IEnumerator Second_opinion_leaves_two_items_and_taking_one_clears_the_other()
        {
            Progress.LevelOverride = Progress.TopLevel;
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            var floors = FloorManager.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            floors.BuildFloor(5);
            yield return null;
            player.Teleport(floors.ArenaCentre + new Vector3(0f, 0.05f, -9f));
            for (float t = 0f; t < 4f && floors.Boss == null; t += Time.unscaledDeltaTime) yield return null;
            Vector3 at = floors.Boss.transform.position;
            floors.Boss.Health.Kill();
            yield return null;

            // Only what the boss left: the shop on this floor has items too.
            Pickup first = null;
            int items = 0;
            foreach (var pickup in Pickup.All)
            {
                if (pickup.kind != PickupKind.Item || Vector3.Distance(pickup.transform.position, at) > 6f) continue;
                items++;
                if (first == null) first = pickup;
            }
            Assert.AreEqual(2, items, "Second Opinion should leave two items");
            player.Teleport(first.transform.position + Vector3.up * 0.05f);
            for (float t = 0f; t < 2f && first != null; t += Time.deltaTime) yield return null;
            items = 0;
            foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Item && Vector3.Distance(pickup.transform.position, at) <= 6f) items++;
            Assert.AreEqual(0, items, "the other one stayed after one was taken");
        }
    }
}
