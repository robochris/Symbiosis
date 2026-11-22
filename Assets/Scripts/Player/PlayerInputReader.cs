using UnityEngine;
using UnityEngine.InputSystem;

namespace Shrunk
{
    /// <summary>One frame of player intent, already turned into world directions.</summary>
    public struct InputFrame
    {
        /// <summary>World-space move direction on the ground, length 0–1.</summary>
        public Vector3 move;
        /// <summary>World-space aim direction on the ground, or zero when not aiming.</summary>
        public Vector3 aim;
        /// <summary>How far away the thing being aimed at is, when known (the point under the crosshair). 0 = not known.</summary>
        public float aimDistance;
        public bool fire;
        public bool dodge;
        /// <summary>Set off the hero power.</summary>
        public bool power;
        /// <summary>Use whatever is in reach: a pad, a portal, a station.</summary>
        public bool use;
        /// <summary>Set in shoulder and first-person views: the character turns to face exactly this way.</summary>
        public float? faceYaw;
    }

    /// <summary>
    /// Merges touch, keyboard and mouse, and gamepad into one InputFrame.
    /// In shoulder and first-person views the right hand turns the view; in the top-down view it points where to shoot.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        public TouchControls touch;
        [Tooltip("Stick tilt needed before the aim stick registers.")]
        public float aimDeadzone = 0.25f;

        [Header("Looking (shoulder and first person)")]
        [Tooltip("Degrees turned per pixel of mouse movement.")]
        public float mouseSensitivity = 0.12f;
        [Tooltip("Degrees per second at full tilt of the right stick or right thumb.")]
        public float stickLookSpeed = 200f;
        [Tooltip("Degrees a second at full tilt of the on-screen look stick. Lower than a pad's: the first phone test found 200 far too quick.")]
        public float touchLookSpeed = 120f;
        public float aimPointRange = 40f;

        /// <summary>When set, replaces real input. Used by tests.</summary>
        [System.NonSerialized] public InputFrame? overrideFrame;

        Weapon _weapon;

        void Awake()
        {
            _weapon = GetComponent<Weapon>();
        }

        bool _fireLatched;

        // Held, or with the setting on, a click to start and a click to stop.
        bool MouseFire(UnityEngine.InputSystem.Mouse mouse)
        {
            if (!GameSettings.ToggleFire) return mouse.leftButton.isPressed;
            if (mouse.leftButton.wasPressedThisFrame) _fireLatched = !_fireLatched;
            return _fireLatched;
        }

        public InputFrame Read()
        {
            if (overrideFrame.HasValue) return overrideFrame.Value;

            // Nothing gets through while the pause menu is up.
            if (PauseMenu.IsOpen || ScreenCards.Blocking) return new InputFrame();

            var follow = CameraFollow.Instance;
            bool usingTouch = touch != null && touch.Active;
            if (usingTouch) touch.Poll();

            var keyboard = Keyboard.current;
            bool cycle = usingTouch ? touch.ConsumeCamera() : keyboard != null && keyboard.vKey.wasPressedThisFrame;
            if (cycle && follow != null)
            {
                follow.CycleMode();
                GameSettings.Camera = follow.mode; // remembered for next time
            }

            bool look = follow != null && follow.IsLookMode;
            InputFrame frame = look ? ReadLook(follow, usingTouch) : ReadTopDown(usingTouch);
            Interact.OnTouch = usingTouch;
            if (usingTouch)
            {
                frame.dodge = touch.ConsumeDodge();
                frame.power = touch.ConsumePower();
                frame.use = touch.ConsumeUse();
            }
            else
            {
                frame.power = keyboard != null && keyboard.qKey.wasPressedThisFrame;
                frame.use = keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame);
                var pad = Gamepad.current;
                if (pad != null)
                {
                    frame.power |= pad.buttonNorth.wasPressedThisFrame;
                    frame.use |= pad.buttonWest.wasPressedThisFrame;
                }
            }
            return frame;
        }

        // ---------- shoulder and first person ----------

        InputFrame ReadLook(CameraFollow follow, bool usingTouch)
        {
            var frame = new InputFrame();
            Vector2 move = Vector2.zero;

            if (usingTouch)
            {
                // Right thumb turns the view. Shots fire by themselves when an enemy is in the sights.
                // Gentler than a pad's stick, and gentlest near the middle: a small nudge of the thumb is a small turn.
                Vector2 stick = touch.Aim;
                float lookSpeed = touchLookSpeed * GameSettings.LookSpeed * stick.magnitude;
                if (stick.magnitude > aimDeadzone) follow.AddLook(stick.x * lookSpeed * Time.deltaTime, stick.y * lookSpeed * 0.5f * Time.deltaTime);
                move = touch.Move;
            }
            else
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    move = ReadKeys(keyboard);
                    frame.dodge |= keyboard.spaceKey.wasPressedThisFrame;
                }

                var mouse = Mouse.current;
                if (mouse != null)
                {
                    if (Cursor.lockState != CursorLockMode.Locked)
                    {
                        // First click grabs the mouse; Esc lets it go again.
                        if (mouse.leftButton.wasPressedThisFrame) Cursor.lockState = CursorLockMode.Locked;
                    }
                    else
                    {
                        Vector2 delta = mouse.delta.ReadValue() * (mouseSensitivity * GameSettings.LookSpeed);
                        follow.AddLook(delta.x, delta.y);
                        frame.fire = MouseFire(mouse);
                    }
                }

                var pad = Gamepad.current;
                if (pad != null)
                {
                    move += pad.leftStick.ReadValue();
                    Vector2 stick = pad.rightStick.ReadValue();
                    float padLookSpeed = stickLookSpeed * GameSettings.LookSpeed;
                    if (stick.magnitude > aimDeadzone) follow.AddLook(stick.x * padLookSpeed * Time.deltaTime, stick.y * padLookSpeed * 0.5f * Time.deltaTime);
                    frame.fire |= pad.rightTrigger.isPressed;
                    frame.dodge |= pad.buttonSouth.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame;
                }
            }

            var yaw = Quaternion.Euler(0f, follow.LookYaw, 0f);
            frame.faceYaw = follow.LookYaw;
            frame.move = yaw * ToGround(Vector2.ClampMagnitude(move, 1f));

            // Shoot at whatever is under the crosshair, not just straight ahead of the body.
            Vector3 toAim = follow.AimPoint(aimPointRange) - transform.position;
            toAim.y = 0f;
            frame.aim = toAim.sqrMagnitude > 1f ? toAim.normalized : yaw * Vector3.forward;
            frame.aimDistance = toAim.magnitude;

            if (usingTouch) frame.fire = _weapon != null && _weapon.HasTarget(frame.aim);
            return frame;
        }

        // ---------- top-down ----------

        InputFrame ReadTopDown(bool usingTouch)
        {
            var frame = new InputFrame();
            var cam = Camera.main;
            var cameraYaw = Quaternion.Euler(0f, cam != null ? cam.transform.eulerAngles.y : 0f, 0f);

            if (usingTouch)
            {
                frame.move = cameraYaw * ToGround(touch.Move);
                if (touch.Aim.magnitude > aimDeadzone)
                {
                    frame.aim = (cameraYaw * ToGround(touch.Aim)).normalized;
                    frame.fire = true;
                }
                return frame;
            }

            if (Cursor.lockState == CursorLockMode.Locked) Cursor.lockState = CursorLockMode.None;

            Vector2 move = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                move = ReadKeys(keyboard);
                frame.dodge |= keyboard.spaceKey.wasPressedThisFrame;
            }

            bool padAiming = false;
            var pad = Gamepad.current;
            if (pad != null)
            {
                move += pad.leftStick.ReadValue();
                Vector2 stick = pad.rightStick.ReadValue();
                if (stick.magnitude > aimDeadzone)
                {
                    padAiming = true;
                    frame.aim = (cameraYaw * ToGround(stick)).normalized;
                    frame.fire = true;
                }
                frame.dodge |= pad.buttonSouth.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame;
            }

            var mouse = Mouse.current;
            if (!padAiming && mouse != null && cam != null)
            {
                // Aim at the point on the ground under the cursor.
                var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
                var ground = new Plane(Vector3.up, transform.position);
                if (ground.Raycast(ray, out float distance))
                {
                    Vector3 to = ray.GetPoint(distance) - transform.position;
                    to.y = 0f;
                    if (to.sqrMagnitude > 0.01f) frame.aim = to.normalized;
                }
                frame.fire = MouseFire(mouse);
            }

            frame.move = cameraYaw * ToGround(Vector2.ClampMagnitude(move, 1f));
            return frame;
        }

        static Vector2 ReadKeys(Keyboard keyboard)
        {
            Vector2 move = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            return move;
        }

        static Vector3 ToGround(Vector2 v) => new Vector3(v.x, 0f, v.y);
    }
}
