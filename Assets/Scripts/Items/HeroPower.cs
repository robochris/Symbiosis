using System;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The one active ability. The demo's is Overcharge: fill the meter by killing, then set it off for a burst of
    /// double damage, piercing, faster shots, with the Void's eyes blazing.
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

        /// <summary>0 = empty, 1 = ready.</summary>
        public float Meter { get; private set; }
        public bool IsReady => Meter >= 1f && !IsActive;
        public bool IsActive => Time.time < _activeUntil;
        /// <summary>While active: 1 just after it starts, falling to 0 as it runs out.</summary>
        public float Remaining01 => IsActive ? (_activeUntil - Time.time) / duration : 0f;
        public event Action Activated;

        float _activeUntil;
        Vector3[] _eyeScales;

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
            return true;
        }

        void Update()
        {
            if (_eyeScales == null) return;
            float blaze = IsActive ? 1.9f + 0.3f * Mathf.Sin(Time.time * 18f) : 1f;
            for (int i = 0; i < _eyeScales.Length; i++)
            {
                if (eyes[i] != null) eyes[i].localScale = _eyeScales[i] * blaze;
            }
        }
    }
}
