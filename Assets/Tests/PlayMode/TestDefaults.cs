using NUnit.Framework;

namespace Shrunk.Tests
{
    /// <summary>
    /// Runs once before any test here. Twists and exchanges are rolled at random in the game; left on, they would change
    /// enemy counts, prices and elites under tests that were not written with them in mind. Tests about them ask for
    /// them by name on the floor manager.
    /// </summary>
    [SetUpFixture]
    public class TestDefaults
    {
        [OneTimeSetUp]
        public void NoSurprises()
        {
            // The tests were written for worlds of ten floors (mini boss on 5, boss on 10) and still run that way.
            // What a six-floor world changes has tests of its own.
            FloorLayout.FloorsPerBiome = 10;
            FloorManager.TwistChanceForAll = 0f;
            FloorManager.ExchangeChanceForAll = 0f;
            FloorManager.ClawChanceForAll = 0f;
            FloorManager.GoldBugChanceForAll = 0f;
            // Every enemy pays, so tests that count coins are not a coin flip.
            Loot.CoinChanceOverride = 1f;
            // Every level reached, so tests written before levels still see every item and style.
            Progress.LevelOverride = Progress.TopLevel;
            Progress.PerksOverride = false;
            // Older tests walk onto pads to use them; the button is tested on its own.
            Interact.AutoUse = true;
            ForestPond.ChanceForAll = 0f;
            SecretWall.ChanceForAll = 0f;
            FieldGuideWatcher.Quiet = true;
            DailyParcel.Off = true;
            FirstTips.Off = true;
            KillCombo.Off = true;
            Shrine.ChanceForAll = 0f;
            RunDirector.BeginnersLuckOff = true;
        }

        [OneTimeTearDown]
        public void Restore()
        {
            FloorLayout.FloorsPerBiome = 6;
            FloorManager.TwistChanceForAll = null;
            FloorManager.ExchangeChanceForAll = null;
            FloorManager.ClawChanceForAll = null;
            FloorManager.GoldBugChanceForAll = null;
            Loot.CoinChanceOverride = null;
            Progress.LevelOverride = null;
            Progress.PerksOverride = null;
            Interact.AutoUse = false;
            ForestPond.ChanceForAll = null;
            SecretWall.ChanceForAll = null;
            FieldGuideWatcher.Quiet = false;
            DailyParcel.Off = false;
            FirstTips.Off = false;
            KillCombo.Off = false;
            Shrine.ChanceForAll = null;
            RunDirector.BeginnersLuckOff = false;
        }
    }
}
