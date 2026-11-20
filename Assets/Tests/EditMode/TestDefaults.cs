using NUnit.Framework;

namespace Shrunk.Tests
{
    /// <summary>Runs once before any test here: these tests were written for worlds of ten floors, and still run that way.</summary>
    [SetUpFixture]
    public class EditModeDefaults
    {
        [OneTimeSetUp]
        public void TenFloorWorlds() => FloorLayout.FloorsPerBiome = 10;

        [OneTimeTearDown]
        public void Restore() => FloorLayout.FloorsPerBiome = 6;
    }
}
