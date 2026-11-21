using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Gauntlet, on the lab's north wall: every world boss with a jar on the shelf, one after another, a point of
    /// health back between them, nothing else. Opens once four bosses are in jars. The best is chalked up.
    /// </summary>
    public class GauntletStation : MonoBehaviour
    {
        public const int JarsNeeded = 4;
        public string runScene = "Run";
        public StepPad pad = new StepPad { radius = 1f };
        public TextMesh board;

        public static bool Open => Achievements.EverythingUnlocked || Specimens.Count >= JarsNeeded;

        void OnEnable()
        {
            WorldPrompt.Add(Prompt, transform);
            if (board != null) board.text = $"THE GAUNTLET\nBEST: {SaveSystem.Data.meta.gauntletBest} BOSSES";
        }

        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || !pad.SteppedOn(transform.position, player.transform.position)) return;
            Begin();
        }

        /// <summary>Starts a Gauntlet run, if it is open and nothing else is on the go. Returns whether it did.</summary>
        public bool Begin()
        {
            if (!Open || SaveSystem.Data.run.active)
            {
                Sfx.Play(SfxId.Blocked);
                return false;
            }
            RunDirector.Gauntlet = true;
            Fade.LoadScene(runScene);
            return true;
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 2.4f) return null;
            if (!Open) return $"The Gauntlet: every boss you have beaten, one after another. Opens with {JarsNeeded} in jars ({Specimens.Count} so far).";
            if (SaveSystem.Data.run.active) return "The Gauntlet: you have a run on the go. Finish it, or lose it, first.";
            return $"The Gauntlet: every boss with a jar, one after another, a point of health back between. Best: {SaveSystem.Data.meta.gauntletBest}. {Interact.Verb} to start.";
        }
    }
}
