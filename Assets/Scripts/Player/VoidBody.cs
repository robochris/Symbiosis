using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Moves the Void's arms and legs: legs swing with the ground covered (in whichever direction the feet are going,
    /// so strafing and backing up look right), arms sway a little, and the shooting arm kicks with every shot.
    /// The hands stay roughly where items expect them, so hand items never drift off.
    /// </summary>
    public class VoidBody : MonoBehaviour
    {
        [Header("Parts (each turns about its own origin: the hip or the shoulder)")]
        public Transform legLeft;
        public Transform legRight;
        public Transform armLeft;
        public Transform armRight;

        [Header("Walking")]
        [Tooltip("How far the legs swing at full speed, in degrees.")]
        public float legSwing = 38f;
        [Tooltip("How far the arms sway at full speed, in degrees.")]
        public float armSway = 7f;
        [Tooltip("Radians of swing per metre walked.")]
        public float stride = 2.6f;
        [Tooltip("Speed at which the swing is at its fullest, in metres per second.")]
        public float fullSpeed = 6f;

        [Header("Shooting")]
        [Tooltip("How far the shooting arm jumps up with each shot, in degrees.")]
        public float recoil = 14f;
        public float recoilRecovery = 9f;

        /// <summary>How hard the legs are swinging right now: 0 standing, 1 flat out. For tests.</summary>
        public float Stride01 { get; private set; }
        /// <summary>1 on the frame of a shot, falling back to 0.</summary>
        public float Kick { get; private set; }

        PlayerController _player;
        Vector3 _last;
        Vector3 _axis = Vector3.right;
        float _phase;
        int _shots;
        Quaternion _legLeftHome, _legRightHome, _armLeftHome, _armRightHome;

        void Awake()
        {
            _player = GetComponent<PlayerController>();
            if (legLeft != null) _legLeftHome = legLeft.localRotation;
            if (legRight != null) _legRightHome = legRight.localRotation;
            if (armLeft != null) _armLeftHome = armLeft.localRotation;
            if (armRight != null) _armRightHome = armRight.localRotation;
        }

        void OnEnable()
        {
            _last = transform.position;
        }

        void LateUpdate()
        {
            if (Time.deltaTime <= 0f) return;

            Vector3 moved = transform.position - _last;
            _last = transform.position;
            moved.y = 0f;
            float speed = moved.magnitude / Time.deltaTime;

            // A teleport is not a step, and a roll tucks the legs in.
            bool walking = speed < 30f && (_player == null || !_player.IsDodging);
            float wanted = walking ? Mathf.Clamp01(speed / fullSpeed) : 0f;
            Stride01 = Mathf.MoveTowards(Stride01, wanted, 6f * Time.deltaTime);
            if (walking && speed > 0.2f)
            {
                _phase += moved.magnitude * stride;
                // Swing across the direction of travel, as seen from the body.
                Vector3 local = transform.InverseTransformDirection(moved.normalized);
                _axis = Vector3.Slerp(_axis, Vector3.Cross(Vector3.up, local).normalized, 12f * Time.deltaTime);
            }

            float swing = Mathf.Sin(_phase) * Stride01;
            if (legLeft != null) legLeft.localRotation = Quaternion.AngleAxis(swing * legSwing, _axis) * _legLeftHome;
            if (legRight != null) legRight.localRotation = Quaternion.AngleAxis(-swing * legSwing, _axis) * _legRightHome;

            // A new shot since last frame: the arm jumps.
            if (_player != null && _player.weapon != null && _player.weapon.ShotsFired != _shots)
            {
                _shots = _player.weapon.ShotsFired;
                Kick = 1f;
            }
            Kick = Mathf.MoveTowards(Kick, 0f, recoilRecovery * Time.deltaTime);

            if (armLeft != null) armLeft.localRotation = Quaternion.AngleAxis(-swing * armSway, Vector3.right) * _armLeftHome;
            if (armRight != null) armRight.localRotation = Quaternion.AngleAxis(swing * armSway - Kick * recoil, Vector3.right) * _armRightHome;
        }
    }
}
