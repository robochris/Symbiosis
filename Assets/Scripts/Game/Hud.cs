using UnityEngine;
using UnityEngine.UI;

namespace Shrunk
{
    /// <summary>Health pips, kill count and the dodge cooldown ring.</summary>
    public class Hud : MonoBehaviour
    {
        public Image[] healthPips;
        public Text killsText;
        [Tooltip("Optional; only shown in scenes with a FloorManager.")]
        public Text floorText;
        [Tooltip("Radial fill over the dodge button; full when the dodge is ready.")]
        public Image dodgeFill;
        [Tooltip("Keyboard hint, shown only when touch controls are off.")]
        public GameObject desktopHint;
        [Tooltip("Dot in the middle of the screen; shown in shoulder and first-person views.")]
        public Image crosshair;
        [Tooltip("Text on the camera button.")]
        public Text cameraLabel;
        public TouchControls touch;

        int _shownKills = -1;
        int _shownFloor = -1;
        int _shownMode = -1;
        Text _hintText;

        void Start()
        {
            if (desktopHint != null)
            {
                desktopHint.SetActive(touch == null || !touch.Active);
                _hintText = desktopHint.GetComponent<Text>();
            }
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

            var follow = CameraFollow.Instance;
            if (follow != null && _shownMode != (int)follow.mode)
            {
                _shownMode = (int)follow.mode;
                bool onTouch = touch != null && touch.Active;
                string name = follow.mode == CameraMode.Shoulder ? "Shoulder" : follow.mode == CameraMode.FirstPerson ? "First person" : "Top-down";
                if (crosshair != null) crosshair.enabled = follow.IsLookMode;
                if (cameraLabel != null) cameraLabel.text = onTouch ? "CAM: " + name : "V  camera: " + name;
                if (_hintText != null)
                {
                    _hintText.text = follow.IsLookMode
                        ? "WASD move  ·  mouse look (click to grab, Esc to release)  ·  hold click to fire  ·  Space dodge  ·  V camera"
                        : "WASD move  ·  mouse aim  ·  hold click to fire  ·  Space dodge  ·  V camera";
                }
            }

            var floors = FloorManager.Instance;
            if (floorText != null && floors != null && _shownFloor != floors.FloorNumber)
            {
                _shownFloor = floors.FloorNumber;
                floorText.text = "Floor " + _shownFloor;
            }

            if (killsText != null && _shownKills != GameLoop.Kills)
            {
                _shownKills = GameLoop.Kills;
                killsText.text = "Ants: " + _shownKills;
            }
        }
    }
}
