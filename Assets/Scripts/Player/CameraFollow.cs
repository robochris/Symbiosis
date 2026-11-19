using System;
using UnityEngine;

namespace Shrunk
{
    public enum CameraMode
    {
        /// <summary>Behind and beside the character, turning with them.</summary>
        Shoulder,
        /// <summary>Through the character's eyes.</summary>
        FirstPerson,
        /// <summary>Pulled back and above at a fixed angle, so most of the room stays in view.</summary>
        TopDown,
        /// <summary>Behind the character like Shoulder, but further back and higher: the character is smaller and more of the room shows.</summary>
        Wide,
        /// <summary>Shoulder while exploring, easing back to Wide when a fight starts and further still for a boss.</summary>
        Auto
    }

    /// <summary>The game camera. Shoulder and first person turn with the player's look input; top-down stays fixed.</summary>
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance { get; private set; }

        public Transform target;
        public CameraMode mode = CameraMode.Auto;

        [Header("Shoulder")]
        [Tooltip("How far behind the character the camera sits.")]
        public float shoulderDistance = 5f;
        public float shoulderHeight = 1.9f;
        [Tooltip("Sideways offset, so the character doesn't cover the crosshair.")]
        public float shoulderSide = 0.9f;

        [Header("Wide (behind, further back and higher)")]
        public float wideDistance = 8f;
        public float wideHeight = 2f;
        [Tooltip("How far the view tilts down, in degrees.")]
        public float widePitch = 24f;

        [Header("Auto (Shoulder when quiet, Wide in a fight)")]
        [Tooltip("Seconds the view takes to ease out when a fight starts, and back in when it ends.")]
        public float autoEaseTime = 0.45f;
        [Tooltip("Extra metres back for a boss, on top of Wide.")]
        public float autoBossDistance = 3f;
        [Tooltip("Extra tilt down for a boss, in degrees.")]
        public float autoBossPitch = 5f;

        [Header("Squeezed against a wall")]
        [Tooltip("When a wall pushes the camera this close, the character is hidden so they don't fill the screen.")]
        public float hideBodyDistance = 1.1f;
        [Tooltip("Metres per second the camera backs out again once a wall is out of the way. Coming in is instant.")]
        public float backOutSpeed = 9f;
        [Tooltip("With a wall close behind, the camera also rises by up to this many metres and tips down, to look over the character's head instead of into their back.")]
        public float squeezeLift = 1.3f;
        public float squeezeTilt = 14f;

        /// <summary>Things standing in a room (pebbles, roots, pods) are on this layer: shots hit them, the camera looks past them.</summary>
        public const int DressingLayer = 2;

        [Header("First person")]
        public float eyeHeight = 1.55f;

        [Header("Looking (shoulder and first person)")]
        public float startPitch = 14f;
        [Tooltip("On: the view only turns left and right and stays at the start pitch. Off: you can also look up and down.")]
        public bool lockVertical = true;
        [Tooltip("How far you can look up (negative) and down, in degrees.")]
        public float minPitch = -25f;
        public float maxPitch = 55f;

        [Header("Top-down")]
        public float distance = 14f;
        [Tooltip("Tilt in degrees: 90 is straight down.")]
        [Range(25f, 90f)] public float pitch = 58f;
        public float yaw = 0f;

        [Header("Feel")]
        public float smoothTime = 0.1f;
        public float lookSmoothTime = 0.03f;
        public float shakeDecay = 6f;

        /// <summary>Direction the player is looking, in degrees. The character faces this way in look modes.</summary>
        public float LookYaw { get; private set; }
        public float LookPitch { get; private set; }
        public bool IsLookMode => mode != CameraMode.TopDown;
        /// <summary>How far the behind-the-character views are pulled back: 0 = Shoulder, 1 = Wide, 2 = boss. Auto eases between them.</summary>
        public float Pull { get; private set; }
        /// <summary>True while the character is hidden: first person, or the camera is squeezed right up against them.</summary>
        public bool BodyHidden => mode == CameraMode.FirstPerson || _squeezed;

        /// <summary>The order the camera button steps through.</summary>
        public static readonly CameraMode[] Order = { CameraMode.Auto, CameraMode.Shoulder, CameraMode.Wide, CameraMode.FirstPerson, CameraMode.TopDown };

        public static CameraMode After(CameraMode mode)
        {
            int index = Array.IndexOf(Order, mode);
            return Order[(index + 1) % Order.Length];
        }

        public static string NameOf(CameraMode mode)
        {
            switch (mode)
            {
                case CameraMode.Auto: return "Auto";
                case CameraMode.Shoulder: return "Shoulder";
                case CameraMode.Wide: return "Wide";
                case CameraMode.FirstPerson: return "First person";
                default: return "Top-down";
            }
        }
        public event Action<CameraMode> ModeChanged;

        static readonly RaycastHit[] Hits = new RaycastHit[16];

        Vector3 _focus;
        Vector3 _velocity;
        float _shake;
        float _pullVelocity;
        float _rigDistance;
        bool _squeezed;
        bool _snap = true;
        Renderer[] _targetRenderers;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            LookPitch = startPitch;
            if (target != null)
            {
                _focus = target.position;
                LookYaw = target.eulerAngles.y;
                _targetRenderers = target.GetComponentsInChildren<Renderer>(true);
            }
            ApplyMode();
            Apply();
        }

        /// <summary>Auto → shoulder → wide → first person → top-down → auto.</summary>
        public void CycleMode()
        {
            SetMode(After(mode));
        }

        public void SetMode(CameraMode newMode)
        {
            mode = newMode;
            if (target != null) LookYaw = target.eulerAngles.y;
            Pull = WantedPull();
            _pullVelocity = 0f;
            _squeezed = false;
            ApplyMode();
            Snap();
        }

        /// <summary>Call after adding or removing parts of the character, so first person hides them too.</summary>
        public void RefreshTargetRenderers()
        {
            if (target == null) return;
            _targetRenderers = target.GetComponentsInChildren<Renderer>(true);
            ShowBody();
        }

        void ApplyMode()
        {
            ShowBody();
            ModeChanged?.Invoke(mode);
        }

        // In first person, or jammed against a wall, the body would fill the screen, so it is hidden.
        void ShowBody()
        {
            if (_targetRenderers == null) return;
            bool show = !BodyHidden;
            foreach (var r in _targetRenderers) if (r != null) r.enabled = show;
        }

        // Where the behind-the-character views should sit right now.
        float WantedPull()
        {
            switch (mode)
            {
                case CameraMode.Wide: return 1f;
                case CameraMode.Auto:
                    var floors = FloorManager.Instance;
                    if (floors == null || !floors.IsLocked) return 0f;
                    return floors.Boss != null ? 2f : 1f;
                default: return 0f;
            }
        }

        /// <summary>Turns the view. Positive yaw is right, positive pitch is up.</summary>
        public void AddLook(float yawDegrees, float pitchDegrees)
        {
            LookYaw = Mathf.Repeat(LookYaw + yawDegrees, 360f);
            LookPitch = lockVertical ? startPitch : Mathf.Clamp(LookPitch - pitchDegrees, minPitch, maxPitch);
        }

        /// <summary>The world point under the crosshair: where shots should go in look modes.</summary>
        public Vector3 AimPoint(float maxDistance)
        {
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;

            // Ignore anything between the camera and the character.
            float skip = IsBehind ? _rigDistance : 0f;
            int count = Physics.RaycastNonAlloc(origin, forward, Hits, maxDistance, ~0, QueryTriggerInteraction.Ignore);
            float nearest = maxDistance;
            for (int i = 0; i < count; i++)
            {
                if (Hits[i].distance < skip || Hits[i].distance >= nearest) continue;
                if (target != null && Hits[i].collider.transform.IsChildOf(target)) continue;
                nearest = Hits[i].distance;
            }
            return origin + forward * nearest;
        }

        /// <summary>Jumps straight to the target with no glide, e.g. after a teleport.</summary>
        public void Snap()
        {
            if (target != null) _focus = target.position;
            _velocity = Vector3.zero;
            _snap = true;
            Apply();
        }

        public void Shake(float amount)
        {
            _shake = Mathf.Max(_shake, amount);
        }

        void LateUpdate()
        {
            if (target != null)
            {
                float smooth = IsLookMode ? lookSmoothTime : smoothTime;
                _focus = Vector3.SmoothDamp(_focus, target.position, ref _velocity, smooth);
            }
            _shake = Mathf.MoveTowards(_shake, 0f, shakeDecay * Time.deltaTime * Mathf.Max(_shake, 0.1f));
            Pull = Mathf.SmoothDamp(Pull, WantedPull(), ref _pullVelocity, autoEaseTime);
            Apply();
        }

        bool IsBehind => mode == CameraMode.Shoulder || mode == CameraMode.Wide || mode == CameraMode.Auto;

        void Apply()
        {
            Quaternion rotation;
            Vector3 position;

            switch (mode)
            {
                case CameraMode.FirstPerson:
                    rotation = Quaternion.Euler(LookPitch, LookYaw, 0f);
                    position = _focus + Vector3.up * eyeHeight + Quaternion.Euler(0f, LookYaw, 0f) * Vector3.forward * 0.15f;
                    break;

                case CameraMode.Shoulder:
                case CameraMode.Wide:
                case CameraMode.Auto:
                    // One rig for all three. Pull 0 is Shoulder: close, level, off to one side so the character doesn't
                    // cover the crosshair. Pull 1 is Wide: further back, tilted down and centred, since from that high the
                    // character sits below the crosshair anyway. Past 1 is the boss view: further and steeper again.
                    float wide = Mathf.Clamp01(Pull), boss = Mathf.Clamp01(Pull - 1f);
                    rotation = Quaternion.Euler(LookPitch + (widePitch - startPitch) * wide + autoBossPitch * boss, LookYaw, 0f);
                    Vector3 pivot = _focus + Vector3.up * Mathf.Lerp(shoulderHeight, wideHeight, wide);
                    Vector3 offset = rotation * new Vector3(shoulderSide * (1f - wide), 0f,
                        -(Mathf.Lerp(shoulderDistance, wideDistance, wide) + autoBossDistance * boss));
                    // In past a wall at once, so the view never ends up inside it; back out gently, so it doesn't lurch.
                    float clear = ClearDistance(pivot, offset.normalized, offset.magnitude);
                    _rigDistance = clear < _rigDistance || _snap ? clear : Mathf.MoveTowards(_rigDistance, clear, backOutSpeed * Time.deltaTime);
                    _snap = false;
                    float squeeze = 1f - Mathf.Clamp01(_rigDistance / offset.magnitude);
                    position = pivot + offset.normalized * _rigDistance + Vector3.up * (squeezeLift * squeeze);
                    rotation *= Quaternion.Euler(squeezeTilt * squeeze, 0f, 0f);

                    // Backed into a corner the camera ends up inside the character: hide them until there is room again.
                    bool squeezed = _squeezed ? _rigDistance < hideBodyDistance + 0.3f : _rigDistance < hideBodyDistance;
                    if (squeezed != _squeezed)
                    {
                        _squeezed = squeezed;
                        ShowBody();
                    }
                    break;

                default:
                    rotation = Quaternion.Euler(pitch, yaw, 0f);
                    position = _focus - rotation * Vector3.forward * distance;
                    break;
            }

            if (_shake > 0.001f) position += rotation * (Vector3)UnityEngine.Random.insideUnitCircle * _shake;
            transform.SetPositionAndRotation(position, rotation);
        }

        // Pulls the camera in front of any wall between it and the character, so it never ends up outside the room.
        float ClearDistance(Vector3 from, Vector3 direction, float wanted)
        {
            int count = Physics.SphereCastNonAlloc(from, 0.3f, direction, Hits, wanted, ~(1 << DressingLayer), QueryTriggerInteraction.Ignore);
            float nearest = wanted;
            for (int i = 0; i < count; i++)
            {
                if (Hits[i].distance >= nearest) continue;
                if (Hits[i].collider.GetComponentInParent<Health>() != null) continue; // characters don't block the camera
                nearest = Hits[i].distance;
            }
            return Mathf.Max(nearest, 0.4f);
        }
    }
}
