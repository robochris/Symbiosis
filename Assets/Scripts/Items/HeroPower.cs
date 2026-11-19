using System;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The one active ability. The demo's is Overcharge: fill the meter by killing, then set it off. It starts with a
    /// surge that hurts and throws back everything close, then for its length gives double damage, piercing, faster
    /// shots and faster feet, with the view opened wide and the Void's eyes blazing.
    /// </summary>
    public class HeroPower : MonoBehaviour
    {
        public string displayName = "Overcharge";
        [Tooltip("Kills needed to fill the meter from empty.")]
        public float killsToFill = 25f;
        [Tooltip("Seconds it lasts.")]
        public float duration = 15f;
        [Tooltip("Lab upgrade: 1.2 = fills 20% faster.")]
        public float fillMultiplier = 1f;
        [Tooltip("The eyes; they grow while the power is on.")]
        public Transform[] eyes;

        [Header("The surge when it is set off")]
        [Tooltip("Enemies this close are hit and thrown back.")]
        public float surgeRadius = 7f;
        public int surgeDamage = 2;
        public Material surgeMaterial;

        [Header("While it lasts")]
        [Tooltip("1.2 = walks 20% faster.")]
        public float moveSpeedMultiplier = 1.2f;
        [Tooltip("Degrees the view opens up by.")]
        public float extraFieldOfView = 10f;

        /// <summary>0 = empty, 1 = ready.</summary>
        public float Meter { get; private set; }
        public bool IsReady => Meter >= 1f && !IsActive;
        public bool IsActive => Time.time < _activeUntil;
        /// <summary>While active: 1 just after it starts, falling to 0 as it runs out.</summary>
        public float Remaining01 => IsActive ? (_activeUntil - Time.time) / duration : 0f;
        public event Action Activated;

        /// <summary>Enemies caught by the last surge.</summary>
        public int LastSurgeHits { get; private set; }

        float _activeUntil;
        Vector3[] _eyeScales;
        Camera _camera;
        float _plainFieldOfView;
        Transform _ring;
        float _ringAge;

        void Awake()
        {
            _eyeScales = new Vector3[eyes != null ? eyes.Length : 0];
            for (int i = 0; i < _eyeScales.Length; i++) _eyeScales[i] = eyes[i].localScale;
        }

        void OnEnable()
        {
            Enemy.Killed += OnKilled;
        }

        void OnDisable()
        {
            Enemy.Killed -= OnKilled;
        }

        void OnKilled(Enemy enemy)
        {
            // The meter doesn't fill while the power is running.
            if (!IsActive) AddCharge(1f / killsToFill);
        }

        public void AddCharge(float amount)
        {
            Meter = Mathf.Clamp01(Meter + amount * fillMultiplier);
        }

        public void SetMeter(float value)
        {
            Meter = Mathf.Clamp01(value);
        }

        public bool TryActivate()
        {
            if (!IsReady) return false;
            Meter = 0f;
            _activeUntil = Time.time + duration;
            Activated?.Invoke();
            Sfx.Play(SfxId.PowerOn);
            Surge();
            return true;
        }

        // Everything close is hit and thrown straight back, bosses included (they just don't move).
        void Surge()
        {
            LastSurgeHits = 0;
            foreach (var enemy in Enemy.Active.ToArray())
            {
                if (enemy.Health.IsDead || !enemy.Targetable) continue;
                Vector3 away = enemy.transform.position - transform.position;
                away.y = 0f;
                if (away.magnitude > surgeRadius) continue;
                enemy.Health.TakeDamage(surgeDamage, away.sqrMagnitude > 0.01f ? away.normalized : transform.forward);
                LastSurgeHits++;
            }
            if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.45f);

            if (_ring == null)
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "Overcharge surge";
                var collider = ring.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
                if (surgeMaterial != null) ring.GetComponent<Renderer>().sharedMaterial = surgeMaterial;
                _ring = ring.transform;
            }
            _ring.position = new Vector3(transform.position.x, 0.06f, transform.position.z);
            _ring.gameObject.SetActive(true);
            _ringAge = 0f;
        }

        void OnDestroy()
        {
            if (_ring != null) Destroy(_ring.gameObject);
            if (_camera != null && _plainFieldOfView > 0f) _camera.fieldOfView = _plainFieldOfView;
        }

        void Update()
        {
            // The surge: a disc that races out to its full reach and is gone.
            if (_ring != null && _ring.gameObject.activeSelf)
            {
                _ringAge += Time.deltaTime;
                float size = Mathf.Lerp(1f, surgeRadius * 2f, _ringAge / 0.25f);
                _ring.localScale = new Vector3(size, 0.01f, size);
                if (_ringAge >= 0.3f) _ring.gameObject.SetActive(false);
            }

            // The view opens up while the power runs, and eases back after.
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera != null) _plainFieldOfView = _camera.fieldOfView;
            }
            if (_camera != null)
            {
                float wanted = _plainFieldOfView + (IsActive ? extraFieldOfView : 0f);
                _camera.fieldOfView = Mathf.MoveTowards(_camera.fieldOfView, wanted, 40f * Time.deltaTime);
            }

            if (_eyeScales == null) return;
            float blaze = IsActive ? 1.9f + 0.3f * Mathf.Sin(Time.time * 18f) : 1f;
            for (int i = 0; i < _eyeScales.Length; i++)
            {
                if (eyes[i] != null) eyes[i].localScale = _eyeScales[i] * blaze;
            }
        }
    }
}
