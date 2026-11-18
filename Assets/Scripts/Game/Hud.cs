using UnityEngine;
using UnityEngine.UI;

namespace Shrunk
{
    /// <summary>Health pips, kill count and the dodge cooldown ring.</summary>
    public class Hud : MonoBehaviour
    {
        public Image[] healthPips;
        public Text killsText;
        [Tooltip("Radial fill over the dodge button; full when the dodge is ready.")]
        public Image dodgeFill;
        [Tooltip("Keyboard hint, shown only when touch controls are off.")]
        public GameObject desktopHint;
        public TouchControls touch;

        int _shownKills = -1;

        void Start()
        {
            if (desktopHint != null) desktopHint.SetActive(touch == null || !touch.Active);
        }

        void Update()
        {
            var player = PlayerController.Instance;
            if (player != null)
            {
                int health = player.Health.Current;
                for (int i = 0; i < healthPips.Length; i++)
                {
                    if (healthPips[i] != null) healthPips[i].enabled = i < health;
                }
                if (dodgeFill != null) dodgeFill.fillAmount = player.DodgeReady01;
            }

            if (killsText != null && _shownKills != GameLoop.Kills)
            {
                _shownKills = GameLoop.Kills;
                killsText.text = "Ants: " + _shownKills;
            }
        }
    }
}
