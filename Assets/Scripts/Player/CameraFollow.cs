using UnityEngine;

namespace Shrunk
{
    /// <summary>Pulled-back third-person camera: behind and above the player at a fixed angle, so most of the room stays in view.</summary>
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance { get; private set; }

        public Transform target;
        [Tooltip("How far back the camera sits.")]
        public float distance = 14f;
        [Tooltip("Tilt in degrees: 90 is straight down, lower is closer to the shoulder.")]
        [Range(25f, 90f)] public float pitch = 58f;
        public float yaw = 0f;
        public float smoothTime = 0.1f;
        public float shakeDecay = 6f;

        Vector3 _focus;
        Vector3 _velocity;
        float _shake;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            if (target != null) _focus = target.position;
            Apply();
        }

        public void Shake(float amount)
        {
            _shake = Mathf.Max(_shake, amount);
        }

        void LateUpdate()
        {
            if (target != null) _focus = Vector3.SmoothDamp(_focus, target.position, ref _velocity, smoothTime);
            _shake = Mathf.MoveTowards(_shake, 0f, shakeDecay * Time.deltaTime * Mathf.Max(_shake, 0.1f));
            Apply();
        }

        void Apply()
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 position = _focus - rotation * Vector3.forward * distance;
            if (_shake > 0.001f) position += (Vector3)Random.insideUnitCircle * _shake;
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
