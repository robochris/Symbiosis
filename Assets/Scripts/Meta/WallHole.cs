using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// A mouse hole in the lab's north wall, with a light on inside. Finch's radio stands beside it, which is not a
    /// coincidence. Nothing to use: walk up and read it. What it says moves on as the story does.
    /// </summary>
    public class WallHole : MonoBehaviour
    {
        public float readDistance = 2.6f;

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        string Prompt(Vector3 playerPosition)
        {
            Vector3 to = transform.position - playerPosition;
            to.y = 0f;
            if (to.magnitude > readDistance) return null;
            MetaSave meta = SaveSystem.Data.meta;
            if (meta.finchBeaten > 0) return "The hole in the skirting. The light is on. He says he is comfortable. He says it a lot.";
            if (Achievements.Has(AchievementId.PaperTrail)) return "The hole in the skirting. Somebody in there has stopped pretending to be a draught.";
            if (meta.runsStarted > 0) return "A hole in the skirting, with a light on inside. The radio's cable runs into it.";
            return "A hole in the skirting, with a light on inside. Probably mice. Probably.";
        }
    }
}
