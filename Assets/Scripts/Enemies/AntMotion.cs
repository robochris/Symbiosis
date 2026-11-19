using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Makes an ant's legs scuttle as it covers ground and its wings beat while it is alive. Three legs move
    /// together and the other three against them, the way a real ant walks.
    /// </summary>
    public class AntMotion : MonoBehaviour
    {
        [Tooltip("Left legs front to back, then right legs front to back.")]
        public Transform[] legs;
        public Transform[] wings;
        [Tooltip("How far each leg swings forward and back, in degrees.")]
        public float legSwing = 24f;
        [Tooltip("Radians of swing per metre walked. Bigger ants take longer steps, so set this lower for them.")]
        public float stride = 7f;
        [Tooltip("Wing beats per second.")]
        public float wingRate = 14f;
        public float wingSwing = 22f;

        /// <summary>How hard the legs are going: 0 still, 1 flat out. For tests.</summary>
        public float Stride01 { get; private set; }

        Quaternion[] _legHome, _wingHome;
        Vector3 _last;
        float _phase;

        void Awake()
        {
            _legHome = new Quaternion[legs != null ? legs.Length : 0];
            for (int i = 0; i < _legHome.Length; i++) if (legs[i] != null) _legHome[i] = legs[i].localRotation;
            _wingHome = new Quaternion[wings != null ? wings.Length : 0];
            for (int i = 0; i < _wingHome.Length; i++) if (wings[i] != null) _wingHome[i] = wings[i].localRotation;
        }

        void OnEnable()
        {
            _last = transform.position;
            Stride01 = 0f;
        }

        void LateUpdate()
        {
            if (Time.deltaTime <= 0f) return;

            Vector3 moved = transform.position - _last;
            _last = transform.position;
            moved.y = 0f;
            float speed = moved.magnitude / Time.deltaTime;
            bool walking = speed > 0.3f && speed < 40f;
            Stride01 = Mathf.MoveTowards(Stride01, walking ? 1f : 0f, 8f * Time.deltaTime);
            if (walking) _phase += moved.magnitude * stride;

            for (int i = 0; i < _legHome.Length; i++)
            {
                if (legs[i] == null) continue;
                // Front and back on one side move with the middle one on the other.
                int half = _legHome.Length / 2;
                bool left = i < half;
                bool withFirst = ((left ? i : i - half) % 2 == 0) == left;
                float swing = Mathf.Sin(_phase + (withFirst ? 0f : Mathf.PI)) * legSwing * Stride01;
                legs[i].localRotation = Quaternion.AngleAxis(left ? swing : -swing, Vector3.up) * _legHome[i];
            }

            for (int i = 0; i < _wingHome.Length; i++)
            {
                if (wings[i] == null) continue;
                float beat = Mathf.Sin(Time.time * wingRate * Mathf.PI * 2f) * wingSwing;
                wings[i].localRotation = Quaternion.AngleAxis(i % 2 == 0 ? beat : -beat, Vector3.forward) * _wingHome[i];
            }
        }
    }
}
