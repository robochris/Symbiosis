using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The vent at the back of the firing range, behind the crates: not signposted, and with Dr. Finch's petty cash
    /// in it. It pays out once. It is there for whoever goes poking about.
    /// </summary>
    public class RangeVent : MonoBehaviour
    {
        public const int PettyCash = 30;

        public StepPad pad = new StepPad { radius = 0.9f };

        public static bool Emptied => SaveSystem.Data.meta.rangeSecret;

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || !pad.SteppedOn(transform.position, player.transform.position)) return;
            Take();
        }

        /// <summary>Takes what is in the vent. Returns false if it has been taken already.</summary>
        public bool Take()
        {
            MetaSave meta = SaveSystem.Data.meta;
            if (meta.rangeSecret) return false;
            meta.rangeSecret = true;
            meta.samples += PettyCash;
            SaveSystem.Save();
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast($"An envelope marked PETTY CASH. {PettyCash} samples. Dr. Finch has not noticed.", 5f);
            Sfx.Play(SfxId.Buy);
            return true;
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 1.6f) return null;
            return Emptied
                ? "A vent. Empty. You have already been through the petty cash."
                : "A vent, with the cover off. There is an envelope in it. Step in.";
        }
    }
}
