using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>The lab's portal. Step into it to start a run, or to carry on one that was saved.</summary>
    public class LabPortal : MonoBehaviour
    {
        public string runScene = "Run";
        public StepPad pad = new StepPad { radius = 1.4f };

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            if (pad.SteppedOn(transform.position, player.transform.position))
            {
                Fade.LoadScene(runScene);
            }
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 3.5f) return null;
            RunSave run = SaveSystem.Data.run;
            return run.active
                ? $"Portal: carry on your run from floor {run.floorNumber}. Step in to go."
                : "Portal: start a run into the Backyard Ant Colony. Step in to go.";
        }
    }
}
