using NUnit.Framework;

namespace Shrunk.Tests
{
    /// <summary>
    /// Runs once before any test here. Twists and dares are rolled at random in the game; left on, they would change
    /// enemy counts, prices and elites under tests that were not written with them in mind. Tests about them ask for
    /// them by name on the floor manager.
    /// </summary>
    [SetUpFixture]
    public class TestDefaults
    {
        [OneTimeSetUp]
        public void NoSurprises()
        {
            FloorManager.TwistChanceForAll = 0f;
            FloorManager.DareChanceForAll = 0f;
            FloorManager.ExchangeChanceForAll = 0f;
        }

        [OneTimeTearDown]
        public void Restore()
        {
            FloorManager.TwistChanceForAll = null;
            FloorManager.DareChanceForAll = null;
            FloorManager.ExchangeChanceForAll = null;
        }
    }
}
