using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The dev menu: a run started straight into a world and floor, and a jump from one world to another.</summary>
    public class DevMenuTests
    {
        [TearDown]
        public void CleanUp()
        {
            DevMenu.God = false;
            DevMenu.Open = false;
            RunDirector.DevWorld = null;
            Time.timeScale = 1f;
            SaveSystem.Wipe();
        }

        [UnityTest]
        public IEnumerator A_run_can_start_on_any_floor_of_any_world_and_jump_to_another()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            RunDirector.DevWorld = "shed";
            RunDirector.DevStage = FloorLayout.FloorsPerBiome;
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var floors = FloorManager.Instance;
            Assert.AreEqual("shed", floors.biome.id);
            Assert.IsTrue(FloorLayout.IsBossFloor(floors.FloorNumber), "not the boss floor");
            Assert.IsNull(RunDirector.DevWorld, "the dev start was not used up");

            DevMenu.JumpTo("attic", 3);
            yield return null;
            Assert.AreEqual("attic", floors.biome.id);
            Assert.AreEqual(3, FloorLayout.FloorOfBiome(floors.FloorNumber));

            DevMenu.God = true;
            yield return null;
            var player = PlayerController.Instance;
            int before = player.Health.Current;
            player.Health.TakeDamage(1, Vector3.forward);
            Assert.AreEqual(before, player.Health.Current, "god mode let a hit through");
        }
    }
}
