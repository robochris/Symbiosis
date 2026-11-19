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
            return $"The mirror: you are wearing {style.CurrentStyle.displayName} ({style.Current + 1} of {style.styles.Length}). Step off and on again to try the next style.";
        }
    }
}
