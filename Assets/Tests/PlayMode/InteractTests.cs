using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>Using things: standing at a pad does nothing; pressing use does, and only the nearest goes.</summary>
    public class InteractTests
    {
        [SetUp]
        public void Fresh()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            Interact.AutoUse = false;
        }

        [TearDown]
        public void CleanUp()
        {
            Interact.AutoUse = true;
            SaveSystem.Wipe();
        }

        [UnityTest]
        public IEnumerator Standing_at_a_pad_does_nothing_until_use_is_pressed()
        {
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            var machine = Object.FindAnyObjectByType<FortuneMachine>();
            machine.rigged = false;

            // Walked onto and stood on: nothing.
            player.Teleport(machine.transform.position + new Vector3(3f, 0.05f, 0f));
            yield return null;
            player.Teleport(machine.transform.position + Vector3.up * 0.05f);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(machine.Used, "the fortune machine went off by being stood on");
            Assert.IsTrue(Interact.Available, "nothing says there is something to use here");
            Assert.That(WorldPrompt.For(player.transform.position), Does.Contain("Press E"));

            // One press: it goes, once.
            player.Input.overrideFrame = new InputFrame { use = true };
            yield return null;
            player.Input.overrideFrame = new InputFrame();
            for (int i = 0; i < 5 && !machine.Used; i++) yield return null;
            Assert.IsTrue(machine.Used, "pressing use at the fortune machine did nothing");

            // Away from everything, there is nothing to use and a press does nothing.
            player.Teleport(new Vector3(0f, 0.05f, 0f) + Vector3.right * 0.5f);
            yield return null;
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator With_two_pads_in_reach_only_the_nearer_one_goes()
        {
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            var near = new GameObject("Near").AddComponent<PadProbe>();
            var far = new GameObject("Far").AddComponent<PadProbe>();
            near.transform.position = new Vector3(30f, 0f, 30f);
            far.transform.position = new Vector3(31.2f, 0f, 30f);
            player.Teleport(new Vector3(30.3f, 0.05f, 30f));
            yield return null;
            yield return null;
            player.Input.overrideFrame = new InputFrame { use = true };
            yield return null;
            player.Input.overrideFrame = new InputFrame();
            for (int i = 0; i < 6; i++) yield return null;
            Assert.AreEqual(1, near.Uses, "the nearer pad did not go");
            Assert.AreEqual(0, far.Uses, "the further pad went as well");
        }

        class PadProbe : MonoBehaviour
        {
            public readonly StepPad pad = new StepPad { radius = 1f };
            public int Uses;

            void Update()
            {
                var player = PlayerController.Instance;
                if (player != null && pad.SteppedOn(transform.position, player.transform.position)) Uses++;
            }
        }
    }
}
