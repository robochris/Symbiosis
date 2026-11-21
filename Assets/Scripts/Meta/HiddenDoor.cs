using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shrunk
{
    /// <summary>
    /// The door the radio insists is not there: a cat flap in the lab, once all of Dr. Finch's notebook has been found.
    /// It is only a clue now. The cat keeps a shop out in the worlds (see CatFlap): now and then a shop has its box.
    /// </summary>
    public class HiddenDoor : MonoBehaviour
    {
        public string runScene = "Run";
        public StepPad pad = new StepPad { radius = 1.3f };

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            if (!pad.SteppedOn(transform.position, player.transform.position)) return;
            Sfx.Play(SfxId.Blocked);
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 3.5f) return null;
            return SaveSystem.Data.meta.finchBeaten > 0
                ? "The cat flap, latched from the other side. The cat comes back; cats do. Look in the shops."
                : "A cat flap. It is not on the floor plan, and it is latched from the other side. It smells of the shops.";
        }
    }
}
