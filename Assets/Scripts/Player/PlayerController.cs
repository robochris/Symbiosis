using UnityEngine;

namespace Shrunk
{
    /// <summary>The Void: twin-stick movement, facing, shooting and the dodge roll.</summary>
    [RequireComponent(typeof(CharacterController), typeof(Health), typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement")]
        public float moveSpeed = 6f;
        [Tooltip("Degrees per second the body turns toward where you aim or move.")]
        public float turnSpeed = 1080f;

        [Header("Dodge")]
        public float dodgeSpeed = 16f;
        public float dodgeDuration = 0.22f;
        public float dodgeCooldown = 1f;
        [Tooltip("Seconds of invulnerability from the start of the roll.")]
        public float dodgeInvulnerability = 0.3f;

        [Header("Parts")]
        public Weapon weapon;
        [Tooltip("The body mesh; spun during the roll.")]
        public Transform visual;

        public Health Health { get; private set; }
        public PlayerInputReader Input { get; private set; }
        public bool IsDodging => _dodgeTimeLeft > 0f;
        /// <summary>0 right after a dodge, 1 when it is ready again.</summary>
        public float DodgeReady01 => dodgeCooldown <= 0f ? 1f : Mathf.Clamp01(1f - (_nextDodge - Time.time) / dodgeCooldown);

        CharacterController _controller;
        Vector3 _dodgeDirection;
        float _dodgeTimeLeft;
        float _nextDodge;

        void Awake()
        {
            Instance = this;
            _controller = GetComponent<CharacterController>();
            Health = GetComponent<Health>();
            Input = GetComponent<PlayerInputReader>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Health.IsDead) return;

            InputFrame input = Input.Read();
            Vector3 velocity;

            if (IsDodging)
            {
                _dodgeTimeLeft -= Time.deltaTime;
                velocity = _dodgeDirection * dodgeSpeed;
                SpinVisual(1f - Mathf.Clamp01(_dodgeTimeLeft / dodgeDuration));
            }
            else
            {
                velocity = input.move * moveSpeed;

                if (input.dodge && Time.time >= _nextDodge)
                {
                    _dodgeDirection = input.move.sqrMagnitude > 0.04f ? input.move.normalized : transform.forward;
                    _dodgeTimeLeft = dodgeDuration;
                    _nextDodge = Time.time + dodgeCooldown;
                    Health.SetInvulnerable(dodgeInvulnerability);
                    velocity = _dodgeDirection * dodgeSpeed;
                }
                else
                {
                    // Face where you aim; if you aren't aiming, face where you walk.
                    Vector3 facing = input.aim.sqrMagnitude > 0.001f ? input.aim : input.move;
                    if (facing.sqrMagnitude > 0.001f)
                    {
                        var target = Quaternion.LookRotation(facing.normalized, Vector3.up);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
                    }

                    if (weapon != null) weapon.Tick(input.fire, input.aim);
                }
            }

            velocity.y = -5f; // keep the controller pressed to the floor
            _controller.Move(velocity * Time.deltaTime);
        }

        /// <summary>Moves the player instantly, e.g. to the start of a new floor.</summary>
        public void Teleport(Vector3 position)
        {
            _controller.enabled = false;
            transform.position = position;
            _controller.enabled = true;
            _dodgeTimeLeft = 0f;
            if (visual != null) visual.localRotation = Quaternion.identity;
        }

        void SpinVisual(float progress)
        {
            if (visual == null) return;
            if (progress >= 1f) { visual.localRotation = Quaternion.identity; return; }

            // Roll forward around the axis that is sideways to the dodge direction.
            Vector3 localDirection = transform.InverseTransformDirection(_dodgeDirection);
            Vector3 axis = Vector3.Cross(Vector3.up, localDirection).normalized;
            visual.localRotation = Quaternion.AngleAxis(progress * 360f, axis);
        }
    }
}
