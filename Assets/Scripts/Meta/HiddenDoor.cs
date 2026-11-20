using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shrunk
{
    /// <summary>
    /// The door the radio insists is not there. It is a cat flap: it turns up in the lab with the first piece of
    /// the cat's collar and opens only for all four, one from the last boss of each biome. Then it leads straight to
    /// the bottom: no floors on the way, only the cat. A run that was saved is left as it is.
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
            if (!Collar.Complete)
            {
                Sfx.Play(SfxId.Blocked);
                return;
            }
            RunDirector.Descent = true;
            Fade.LoadScene(runScene);
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 3.5f) return null;
            if (!Collar.Complete)
            {
                return $"A cat flap. It is not on the floor plan. It only opens for a collar, and you have {Collar.Count} of {Collar.Pieces.Length} pieces: {Collar.Names(true)}." +
                       $"\nStill out there, with somebody large: {Collar.Names(false)}.";
            }
            return SaveSystem.Data.meta.finchBeaten > 0
                ? "The cat flap. The cat comes back; cats do. Step in to go again."
                : "A cat flap. It is not on the floor plan. It opens for the collar. Step in to go all the way down.";
        }
    }
}
