using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shrunk
{
    /// <summary>
    /// The machine's panel, in the lab from the first day: four sockets, one for each missing part, lit as the parts
    /// come home. With all four in, stepping onto its mat switches the machine on, which is the fight with it. A run
    /// that was saved is left as it is.
    /// </summary>
    public class MachinePanel : MonoBehaviour
    {
        public string runScene = "Run";
        public StepPad pad = new StepPad { radius = 1f };
        [Tooltip("One per part, in order: shown once that part is held.")]
        public GameObject[] lamps;

        void OnEnable()
        {
            WorldPrompt.Add(Prompt, transform);
            Show();
        }

        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Show()
        {
            for (int i = 0; lamps != null && i < lamps.Length; i++)
            {
                if (lamps[i] != null) lamps[i].SetActive(i < MachineParts.All.Length && MachineParts.Has(MachineParts.All[i]));
            }
        }

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || !pad.SteppedOn(transform.position, player.transform.position)) return;
            SwitchOn();
        }

        /// <summary>Switches the machine on, if it has all its parts. Returns whether it did.</summary>
        public bool SwitchOn()
        {
            if (!MachineParts.Complete)
            {
                Sfx.Play(SfxId.Blocked);
                return false;
            }
            RunDirector.Descent = true;
            RunDirector.ToMachine = true;
            Fade.LoadScene(runScene);
            return true;
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 2.4f) return null;
            string cycle = MachineParts.Cycle > 0 ? $" Cycle {MachineParts.Cycle + 1}." : "";
            if (MachineParts.Complete)
            {
                return $"The machine: all {MachineParts.All.Length} parts are in.{cycle}\nStep on the mat to switch it on. It will not thank you.";
            }
            return $"The machine: {MachineParts.Count} of {MachineParts.All.Length} parts fitted ({MachineParts.Names(true)}).{cycle}" +
                   $"\nStill out there, each with the last boss of a world: {MachineParts.Names(false)}.";
        }
    }
}
