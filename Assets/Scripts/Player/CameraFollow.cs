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
        Wide
    }

    /// <summary>The game camera. Shoulder and first person turn with the player's look input; top-down stays fixed.</summary>
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance { get; private set; }

        public Transform target;
        public CameraMode mode = CameraMode.Shoulder;

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

        /// <summary>The order the camera button steps through.</summary>
        public static readonly CameraMode[] Order = { CameraMode.Shoulder, CameraMode.Wide, CameraMode.FirstPerson, CameraMode.TopDown };

        public static CameraMode After(CameraMode mode)
        {
            int index = Array.IndexOf(Order, mode);
            return Order[(index + 1) % Order.Length];
        }

        public static string NameOf(CameraMode mode)
        {
            switch (mode)
            {
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

        /// <summary>Shoulder → wide → first person → top-down → shoulder.</summary>
        public void CycleMode()
        {
            SetMode(After(mode));
        }

        public void SetMode(CameraMode newMode)
        {
            mode = newMode;
            if (target != null) LookYaw = target.eulerAngles.y;
            ApplyMode();
            Snap();
        }

        /// <summary>Call after adding or removing parts of the character, so first person hides them too.</summary>
        public void RefreshTargetRenderers()
        {
            if (target == null) return;
            _targetRenderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (var r in _targetRenderers) if (r != null) r.enabled = mode != CameraMode.FirstPerson;
        }

        void ApplyMode()
        {
            // In first person the body would fill the screen, so hide it.
            if (_targetRenderers != null)
            {
                foreach (var r in _targetRenderers) if (r != null) r.enabled = mode != CameraMode.FirstPerson;
            }
            ModeChanged?.Invoke(mode);
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
            float skip = mode == CameraMode.Shoulder ? shoulderDistance : mode == CameraMode.Wide ? wideDistance : 0f;
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
            Apply();
        }

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
                    rotation = Quaternion.Euler(LookPitch, LookYaw, 0f);
                    Vector3 pivot = _focus + Vector3.up * shoulderHeight;
                    Vector3 offset = rotation * new Vector3(shoulderSide, 0f, -shoulderDistance);
                    position = pivot + offset.normalized * ClearDistance(pivot, offset.normalized, offset.magnitude);
                    break;

                case CameraMode.Wide:
                    // Same idea as Shoulder, centred: from this high the character sits below the crosshair anyway.
                    rotation = Quaternion.Euler(LookPitch + widePitch - startPitch, LookYaw, 0f);
                    Vector3 widePivot = _focus + Vector3.up * wideHeight;
                    Vector3 back = rotation * Vector3.back;
                    position = widePivot + back * ClearDistance(widePivot, back, wideDistance);
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
            int count = Physics.SphereCastNonAlloc(from, 0.3f, direction, Hits, wanted, ~0, QueryTriggerInteraction.Ignore);
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
