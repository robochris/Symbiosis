using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shrunk
{
    /// <summary>
    /// The door the radio insists is not there. It turns up in the lab once all of Dr. Finch's notebook has been
    /// found, and leads straight to the bottom: no floors on the way, only Finch. A run that was saved is left as it is.
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
            RunDirector.Descent = true;
            Fade.LoadScene(runScene);
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 3.5f) return null;
            return SaveSystem.Data.meta.finchBeaten > 0
                ? "The door. The cat comes back; cats do. Step in to go again."
                : "A door. It is not on the floor plan. Step in to go all the way down.";
        }
    }
}
