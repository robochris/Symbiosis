using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Shrunk
{
    /// <summary>
    /// Twin sticks. The move stick stays where it is drawn: a thumb anywhere on its half of the screen is read from the
    /// stick's own centre. The look stick floats: it starts wherever the thumb lands on the other half.
    /// Hidden on PC. Lives on the HUD canvas, which must be Screen Space - Overlay.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        [Header("Sticks")]
        public RectTransform moveBase;
        public RectTransform moveKnob;
        public RectTransform aimBase;
        public RectTransform aimKnob;
        [Tooltip("How far the thumb travels for full tilt, in canvas units.")]
        public float stickRadius = 110f;
        [Tooltip("Swap sides for left-handed play.")]
        public bool leftHanded;

        [Header("Buttons")]
        public RectTransform dodgeButton;
        [Tooltip("Tap to switch camera view. Stays visible on PC as a label.")]
        public RectTransform cameraButton;
        [Tooltip("Tap to set off the hero power. Stays visible on PC as a meter.")]
        public RectTransform powerButton;

        [Header("Editor")]
        [Tooltip("Show the touch controls in the editor and drive them with the mouse.")]
        public bool simulateInEditor;

        public Vector2 Move { get; private set; }
        public Vector2 Aim { get; private set; }

        public bool Active => UnityEngine.Device.Application.isMobilePlatform || (Application.isEditor && simulateInEditor);

        Canvas _canvas;
        int _moveId = -1, _aimId = -1;
        Vector2 _moveOrigin, _aimOrigin;
        Vector3 _moveRest, _aimRest;
        bool _dodgeQueued;
        bool _cameraQueued;
        bool _powerQueued;
        bool _simulating;

        void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
        }

        void OnEnable()
        {
            bool active = Active;
            SetVisible(active);
            if (!active) return;

            EnhancedTouchSupport.Enable();
            if (Application.isEditor && simulateInEditor)
            {
                TouchSimulation.Enable();
                _simulating = true;
            }
        }

        void Start()
        {
            if (moveBase != null) _moveRest = moveBase.position;
            if (aimBase != null) _aimRest = aimBase.position;
        }

        void OnDisable()
        {
            if (_simulating) { TouchSimulation.Disable(); _simulating = false; }
            if (EnhancedTouchSupport.enabled) EnhancedTouchSupport.Disable();
        }

        void SetVisible(bool visible)
        {
            if (moveBase != null) moveBase.gameObject.SetActive(visible);
            if (aimBase != null) aimBase.gameObject.SetActive(visible);
            if (dodgeButton != null) dodgeButton.gameObject.SetActive(visible);
        }

        /// <summary>True once per dodge-button tap.</summary>
        public bool ConsumeDodge()
        {
            bool queued = _dodgeQueued;
            _dodgeQueued = false;
            return queued;
        }

        /// <summary>True once per hero-power tap.</summary>
        public bool ConsumePower()
        {
            bool queued = _powerQueued;
            _powerQueued = false;
            return queued;
        }

        /// <summary>True once per camera-button tap.</summary>
        public bool ConsumeCamera()
        {
            bool queued = _cameraQueued;
            _cameraQueued = false;
            return queued;
        }

        /// <summary>Reads this frame's touches. Called by PlayerInputReader so the order is always touches first, then the player.</summary>
        public void Poll()
        {
            if (!Active) return;

            float radius = stickRadius * (_canvas != null ? _canvas.scaleFactor : 1f);
            bool moveSeen = false, aimSeen = false;

            foreach (var touch in Touch.activeTouches)
            {
                Vector2 position = touch.screenPosition;
                bool ended = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;

                if (touch.phase == TouchPhase.Began)
                {
                    if (dodgeButton != null && RectTransformUtility.RectangleContainsScreenPoint(dodgeButton, position, null))
                    {
                        _dodgeQueued = true;
                        continue;
                    }
                    if (powerButton != null && RectTransformUtility.RectangleContainsScreenPoint(powerButton, position, null))
                    {
                        _powerQueued = true;
                        continue;
                    }
                    if (cameraButton != null && RectTransformUtility.RectangleContainsScreenPoint(cameraButton, position, null))
                    {
                        _cameraQueued = true;
                        continue;
                    }

                    bool onMoveSide = (position.x < Screen.width * 0.5f) != leftHanded;
                    // The move stick does not come to the thumb: its centre is where it is drawn.
                    if (onMoveSide && _moveId < 0) { _moveId = touch.touchId; _moveOrigin = _moveRest; }
                    else if (!onMoveSide && _aimId < 0) { _aimId = touch.touchId; _aimOrigin = GameSettings.FloatingAim ? position : (Vector2)_aimRest; }
                }

                if (touch.touchId == _moveId)
                {
                    if (ended) _moveId = -1;
                    else { moveSeen = true; Move = ReadFixedStick(position, _moveOrigin, radius); }
                }
                else if (touch.touchId == _aimId)
                {
                    if (ended) _aimId = -1;
                    else { aimSeen = true; Aim = GameSettings.FloatingAim ? ReadStick(position, ref _aimOrigin, radius) : ReadFixedStick(position, _aimOrigin, radius); }
                }
            }

            if (!moveSeen) { _moveId = -1; Move = Vector2.zero; }
            if (!aimSeen) { _aimId = -1; Aim = Vector2.zero; }

            Draw(moveBase, moveKnob, moveSeen, _moveOrigin, Move, radius, _moveRest);
            Draw(aimBase, aimKnob, aimSeen, _aimOrigin, Aim, radius, _aimRest);
        }

        // A stick that stays put: past full tilt it is simply full tilt.
        static Vector2 ReadFixedStick(Vector2 position, Vector2 origin, float radius) => Vector2.ClampMagnitude((position - origin) / radius, 1f);

        // The stick's centre follows the thumb once it is dragged past full tilt, so the thumb never "falls off" the stick.
        static Vector2 ReadStick(Vector2 position, ref Vector2 origin, float radius)
        {
            Vector2 offset = position - origin;
            if (offset.magnitude > radius)
            {
                origin = position - offset.normalized * radius;
                offset = position - origin;
            }
            return offset / radius;
        }

        static void Draw(RectTransform stickBase, RectTransform knob, bool held, Vector2 origin, Vector2 value, float radius, Vector3 rest)
        {
            if (stickBase == null) return;
            stickBase.position = held ? (Vector3)origin : rest;
            if (knob != null) knob.position = stickBase.position + (Vector3)(value * radius);
        }
    }
}
