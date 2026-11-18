using System;
using UnityEngine;

namespace Shrunk
{
    public enum Team { Player, Enemy }

    /// <summary>Hit points for anything that can be damaged. Resets every time the object is enabled, so pooled objects come back fresh.</summary>
    public class Health : MonoBehaviour
    {
        public Team team = Team.Enemy;
        public int maxHealth = 3;
        [Tooltip("Seconds of invulnerability after taking a hit.")]
        public float hitInvulnerability = 0f;

        public int Current { get; private set; }
        public bool IsDead => Current <= 0;
        public bool Invulnerable => Time.time < _invulnerableUntil;

        /// <summary>Amount, and the direction the hit was travelling.</summary>
        public event Action<int, Vector3> Damaged;
        public event Action Died;

        float _invulnerableUntil;

        void OnEnable()
        {
            Current = maxHealth;
            _invulnerableUntil = 0f;
        }

        public void SetInvulnerable(float seconds)
        {
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + seconds);
        }

        /// <summary>Returns true if the damage landed.</summary>
        public bool TakeDamage(int amount, Vector3 hitDirection)
        {
            if (IsDead || Invulnerable || amount <= 0) return false;

            Current = Mathf.Max(0, Current - amount);
            if (hitInvulnerability > 0f) SetInvulnerable(hitInvulnerability);
            Damaged?.Invoke(amount, hitDirection);
            if (Current == 0) Died?.Invoke();
            return true;
        }
    }
}
