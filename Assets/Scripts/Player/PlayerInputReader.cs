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
        public bool fire;
        public bool dodge;
    }

    /// <summary>Merges touch sticks, keyboard and mouse, and gamepad into one InputFrame.</summary>
    public class PlayerInputReader : MonoBehaviour
    {
        public TouchControls touch;
        [Tooltip("Stick tilt needed before the aim stick starts firing.")]
        public float aimDeadzone = 0.25f;

        /// <summary>When set, replaces real input. Used by tests.</summary>
        [System.NonSerialized] public InputFrame? overrideFrame;

        public InputFrame Read()
        {
            if (overrideFrame.HasValue) return overrideFrame.Value;

            var frame = new InputFrame();
            var cam = Camera.main;
            var cameraYaw = Quaternion.Euler(0f, cam != null ? cam.transform.eulerAngles.y : 0f, 0f);

            if (touch != null && touch.Active)
            {
                touch.Poll();
                frame.move = cameraYaw * ToGround(touch.Move);
                if (touch.Aim.magnitude > aimDeadzone)
                {
                    frame.aim = (cameraYaw * ToGround(touch.Aim)).normalized;
                    frame.fire = true;
                }
                frame.dodge = touch.ConsumeDodge();
                return frame;
            }

            Vector2 move = Vector2.zero;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
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
                frame.fire = mouse.leftButton.isPressed;
            }

            frame.move = cameraYaw * ToGround(Vector2.ClampMagnitude(move, 1f));
            return frame;
        }

        static Vector3 ToGround(Vector2 v) => new Vector3(v.x, 0f, v.y);
    }
}
