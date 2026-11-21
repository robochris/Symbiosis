using UnityEngine;

namespace Shrunk
{
    /// <summary>The Void: movement, facing, shooting, the dodge and setting off the hero power.</summary>
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
        [Tooltip("Lab upgrade: 1.15 = the dodge recharges 15% faster.")]
        public float dodgeRechargeMultiplier = 1f;

        [Header("Parts")]
        public Weapon weapon;
        [Tooltip("The body mesh; spun during the roll.")]
        public Transform visual;

        public Health Health { get; private set; }
        public PlayerInputReader Input { get; private set; }
        public PlayerItems Items { get; private set; }
        public HeroPower Power { get; private set; }
        public bool IsDodging => _dodgeTimeLeft > 0f;
        /// <summary>Mid-dodge with Wing Buds: off the ground, so ground hazards can't touch the player.</summary>
        public bool IsHovering => IsDodging && _hovering;
        public int DodgeCharges => _charges;
        /// <summary>Soaked by a dew drop (or anything else that slows): walking is slower until it wears off. Dodging is not.</summary>
        public bool IsSlowed => Time.time < _slowUntil;
        /// <summary>1 when a dodge is available, otherwise how far the next one has recharged.</summary>
        public float DodgeReady01 => _charges > 0 ? 1f : Mathf.Clamp01(1f - (_rechargeAt - Time.time) / Mathf.Max(0.01f, _lastCooldown));

        CharacterController _controller;
        Vector3 _dodgeDirection;
        Vector3 _visualHome;
        float _dodgeTimeLeft;
        float _dodgeLength = 1f;
        float _rechargeAt;
        float _lastCooldown = 1f;
        int _charges = 1;
        bool _hovering;
        float _slowUntil;
        float _slowTo = 1f;
        GameObject _soaked;
        Vector3 _push;

        /// <summary>
        /// Shoves the player along for this frame, on top of their own walking or dodging: a boss's suction, a wind.
        /// Call it every frame the shove lasts.
        /// </summary>
        public void Push(Vector3 velocity)
        {
            velocity.y = 0f;
            _push += velocity;
        }

        /// <summary>Slows walking to this share of normal speed for a while. A second one restarts the clock.</summary>
        /// <param name="look">If given, a puddle of this material shows round the player's feet while it lasts.</param>
        public void Slow(float speedShare, float seconds, Material look = null)
        {
            _slowTo = Mathf.Clamp(speedShare, 0.1f, 1f);
            _slowUntil = Time.time + seconds;
            if (look == null) return;

            if (_soaked == null)
            {
                _soaked = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _soaked.name = "Soaked";
                Destroy(_soaked.GetComponent<Collider>());
                _soaked.GetComponent<Collider>().enabled = false;
                _soaked.transform.SetParent(transform, false);
                _soaked.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                _soaked.transform.localScale = new Vector3(1.5f, 0.015f, 1.5f);
            }
            _soaked.GetComponent<Renderer>().sharedMaterial = look;
            _soaked.SetActive(true);
        }

        void Awake()
        {
            Instance = this;
            _controller = GetComponent<CharacterController>();
            Health = GetComponent<Health>();
            Input = GetComponent<PlayerInputReader>();
            Items = GetComponent<PlayerItems>();
            Power = GetComponent<HeroPower>();
            if (visual != null) _visualHome = visual.localPosition;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (_soaked != null && _soaked.activeSelf && !IsSlowed) _soaked.SetActive(false);
            if (Health.IsDead) return;

            InputFrame input = Input.Read();
            PlayerStats stats = Items != null ? Items.Stats : PlayerStats.Default;
            RechargeDodge(stats);

            if (input.power && Power != null) Power.TryActivate();
            if (input.use) Interact.Press();

            Vector3 velocity;

            if (IsDodging)
            {
                _dodgeTimeLeft -= Time.deltaTime;
                velocity = _dodgeDirection * (_hovering ? dodgeSpeed * 0.7f : dodgeSpeed);
                AnimateDodge(1f - Mathf.Clamp01(_dodgeTimeLeft / _dodgeLength));
            }
            else
            {
                velocity = input.move * (moveSpeed * stats.moveSpeedMultiplier * (IsSlowed ? _slowTo : 1f)
                    * (Power != null && Power.IsOvercharged ? Power.moveSpeedMultiplier : 1f));

                if (input.dodge && _charges > 0)
                {
                    StartDodge(input, stats);
                    velocity = _dodgeDirection * dodgeSpeed;
                }
                else
                {
                    // Shoulder and first person: face exactly where you look.
                    // Top-down: face where you aim; if you aren't aiming, face where you walk.
                    Vector3 facing = input.aim.sqrMagnitude > 0.001f ? input.aim : input.move;
                    if (input.faceYaw.HasValue)
                    {
                        transform.rotation = Quaternion.Euler(0f, input.faceYaw.Value, 0f);
                    }
                    else if (facing.sqrMagnitude > 0.001f)
                    {
                        var target = Quaternion.LookRotation(facing.normalized, Vector3.up);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
                    }

                    if (weapon != null) weapon.Tick(input.fire, input.aim, input.aimDistance);
                }
            }

            velocity += _push;
            _push = Vector3.zero;
            velocity.y = -5f; // keep the controller pressed to the floor
            _controller.Move(velocity * Time.deltaTime);
        }

        float Cooldown(PlayerStats stats) => dodgeCooldown * stats.dodgeCooldownMultiplier / Mathf.Max(0.1f, dodgeRechargeMultiplier);

        // Dodges come back one at a time. Extra Legs with Wing Buds gives a second one.
        void RechargeDodge(PlayerStats stats)
        {
            if (_charges >= stats.dodgeCharges || Time.time < _rechargeAt) return;
            _charges++;
            if (_charges < stats.dodgeCharges)
            {
                _lastCooldown = Cooldown(stats);
                _rechargeAt = Time.time + _lastCooldown;
            }
        }

        void StartDodge(InputFrame input, PlayerStats stats)
        {
            // The recharge clock only starts when a full set of dodges is first broken into.
            if (_charges >= stats.dodgeCharges)
            {
                _lastCooldown = Cooldown(stats);
                _rechargeAt = Time.time + _lastCooldown;
            }
            _charges--;

            _dodgeDirection = input.move.sqrMagnitude > 0.04f ? input.move.normalized : transform.forward;
            _hovering = stats.hoverDodge;
            _dodgeLength = _hovering ? dodgeDuration * 2f : dodgeDuration;
            _dodgeTimeLeft = _dodgeLength;
            Health.SetInvulnerable(_hovering ? _dodgeLength : dodgeInvulnerability);
            // Birthday Candle: where the dodge started is on fire now.
            if (stats.fireDodge > 0 && GameAssets.Instance != null)
            {
                GroundHazard.SpawnFriendlyPuddle(transform.position, stats.fireDodge + 1, true, GameAssets.Instance.fire);
            }
            Sfx.Play(SfxId.Dodge, 0.7f);
        }

        /// <summary>Moves the player instantly, e.g. to the start of a new floor.</summary>
        public void Teleport(Vector3 position)
        {
            _controller.enabled = false;
            transform.position = position;
            _controller.enabled = true;
            _dodgeTimeLeft = 0f;
            if (visual != null)
            {
                visual.localRotation = Quaternion.identity;
                visual.localPosition = _visualHome;
            }
        }

        void AnimateDodge(float progress)
        {
            if (visual == null) return;
            if (progress >= 1f)
            {
                visual.localRotation = Quaternion.identity;
                visual.localPosition = _visualHome;
                return;
            }

            if (_hovering)
            {
                // Wing Buds: lift off the ground in an arc instead of rolling.
                visual.localPosition = _visualHome + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * 0.9f);
                return;
            }

            // Roll forward around the axis that is sideways to the dodge direction.
            Vector3 localDirection = transform.InverseTransformDirection(_dodgeDirection);
            Vector3 axis = Vector3.Cross(Vector3.up, localDirection).normalized;
            visual.localRotation = Quaternion.AngleAxis(progress * 360f, axis);
        }
    }
}
