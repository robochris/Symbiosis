using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The record board in the lab: how the runs have gone so far. Walk up to read it. A gold peg is added for each
    /// boss beaten, so the board fills up as the player gets further.
    /// </summary>
    public class TrophyBoard : MonoBehaviour
    {
        [Tooltip("One per boss beaten; they switch on in order.")]
        public GameObject[] pegs;
        public float readDistance = 2.6f;

        void OnEnable()
        {
            WorldPrompt.Add(Prompt, transform);
            Refresh();
        }

        void OnDisable() => WorldPrompt.Remove(Prompt);

        public void Refresh()
        {
            if (pegs == null) return;
            int beaten = SaveSystem.Data.meta.bossesBeaten;
            for (int i = 0; i < pegs.Length; i++) if (pegs[i] != null) pegs[i].SetActive(i < beaten);
        }

        string Prompt(Vector3 playerPosition)
        {
            Vector3 to = playerPosition - transform.position;
            to.y = 0f;
            if (to.magnitude > readDistance) return null;

            MetaSave meta = SaveSystem.Data.meta;
            if (meta.runsStarted == 0) return "The record board: nothing on it yet. Your runs will be marked up here.";
            string text = $"The record board: {meta.runsStarted} runs  ·  deepest floor {meta.deepestFloor}  ·  {meta.bossesBeaten} bosses beaten  ·  " +
                          $"{meta.extractions} extractions  ·  {meta.deaths} deaths  ·  {Achievements.Count} of {Achievements.All.Length} achievements";
            // One thing to aim for next.
            foreach (var achievement in Achievements.All)
            {
                if (Achievements.Has(achievement.id)) continue;
                text += $"\nNext: {achievement.name}. {achievement.how}";
                break;
            }
            return text;
        }
    }
}
