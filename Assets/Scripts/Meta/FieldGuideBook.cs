using UnityEngine;

namespace Shrunk
{
    /// <summary>The field guide in the office: how many have been met, and one page at a time to read.</summary>
    public class FieldGuideBook : MonoBehaviour
    {
        public StepPad pad = new StepPad { radius = 1f };
        int _page = -1;

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || !pad.SteppedOn(transform.position, player.transform.position)) return;
            Turn();
        }

        /// <summary>The next page among those met. Returns what it says, or null if nothing has been met yet.</summary>
        public string Turn()
        {
            var seen = SaveSystem.Data.meta.creaturesSeen;
            if (seen.Count == 0) return null;
            _page = (_page + 1) % seen.Count;
            string page = FieldGuide.Page(seen[_page]);
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast($"{_page + 1} of {seen.Count}. {page}", 5f);
            Sfx.Play(SfxId.Coin, 0.5f);
            return page;
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 2.2f) return null;
            int seen = FieldGuide.SeenCount;
            return seen == 0
                ? "Dr. Finch's field guide. Blank so far: meet something first."
                : $"Dr. Finch's field guide: {seen} of {FieldGuide.Total} creatures met. {Interact.Verb} to turn a page.";
        }
    }
}
