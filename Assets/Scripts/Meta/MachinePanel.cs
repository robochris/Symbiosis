using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shrunk
{
    /// <summary>
    /// The machine's panel, in the lab from the first day: four sockets, one for each missing part, lit as the parts
    /// come home. It only keeps count: the machine itself is built out in a run (see MachineRoom), on a boss floor from
    /// the second world on, once all four are in hand.
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
            Sfx.Play(SfxId.Blocked);
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 2.4f) return null;
            string cycle = MachineParts.Cycle > 0 ? $" Cycle {MachineParts.Cycle + 1}." : "";
            if (MachineParts.Complete)
            {
                return $"The machine: all {MachineParts.All.Length} parts in hand.{cycle}\nIt gets built out there: from the second world of a run on, look for a room off the map on a boss floor.";
            }
            return $"The machine: {MachineParts.Count} of {MachineParts.All.Length} parts fitted ({MachineParts.Names(true)}).{cycle}" +
                   $"\nStill out there, each with the last boss of a world: {MachineParts.Names(false)}.";
        }
    }
}
