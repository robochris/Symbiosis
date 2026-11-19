using UnityEngine;

namespace Shrunk
{
    /// <summary>The mirror in the lab. Step onto the mat in front of it to try the next character style.</summary>
    public class StyleMirror : MonoBehaviour
    {
        public StepPad pad = new StepPad { radius = 1f };

        void OnEnable() => WorldPrompt.Add(Prompt);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || !pad.SteppedOn(transform.position, player.transform.position)) return;
            var style = player.GetComponent<PlayerStyle>();
            if (style == null) return;
            style.Next();
            Sfx.Play(SfxId.Item);
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 2.4f) return null;
            var player = PlayerController.Instance;
            var style = player != null ? player.GetComponent<PlayerStyle>() : null;
            if (style == null || style.CurrentStyle == null) return null;
            int have = style.UnlockedCount, all = style.styles.Length;
            string text = $"The mirror: you are wearing {style.CurrentStyle.displayName}. {have} of {all} styles unlocked.";
            if (have > 1) text += " Step off and on again to try the next one.";
            // Say how to get the next locked one.
            for (int i = 0; i < all; i++)
            {
                if (style.Unlocked(i)) continue;
                var needs = Achievements.ForStyle(style.styles[i].displayName);
                if (needs != null) text += $" {style.styles[i].displayName}: {needs.how}";
                break;
            }
            return text;
        }
    }
}
