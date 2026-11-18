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
        public float LastDamageTime { get; private set; }

        /// <summary>Scales incoming damage; 0.5 = takes half. Set by mutations, reset when the object is re-enabled.</summary>
        [NonSerialized] public float damageMultiplier = 1f;
        /// <summary>Given the direction a hit is travelling, returns true to stop it completely (a shell, a shield).</summary>
        [NonSerialized] public Func<Vector3, bool> ShouldBlock;
        /// <summary>Given the direction a hit is travelling, returns a whole-number multiplier (a weak point returns 2).</summary>
        [NonSerialized] public Func<Vector3, int> DamageScaler;

        /// <summary>Amount actually lost (can be 0 for an armored hit), and the direction the hit was travelling.</summary>
        public event Action<int, Vector3> Damaged;
        /// <summary>A hit was stopped by ShouldBlock.</summary>
        public event Action<Vector3> Blocked;
        public event Action Died;

        float _invulnerableUntil;
        float _carry;

        void OnEnable()
        {
            Current = maxHealth;
            _invulnerableUntil = 0f;
            _carry = 0f;
            damageMultiplier = 1f;
            ShouldBlock = null;
            DamageScaler = null;
            LastDamageTime = -999f;
        }

        public void SetInvulnerable(float seconds)
        {
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + seconds);
        }

        public void ClearInvulnerability()
        {
            _invulnerableUntil = 0f;
        }

        /// <summary>Returns true if the hit connected (even if armor or a shell soaked it up).</summary>
        public bool TakeDamage(int amount, Vector3 hitDirection)
        {
            if (IsDead || Invulnerable || amount <= 0) return false;

            if (ShouldBlock != null && ShouldBlock(hitDirection))
            {
                Blocked?.Invoke(hitDirection);
                return true;
            }

            // Half damage on a 1-damage shot means every second shot counts, so fractions are carried over.
            if (DamageScaler != null) amount *= Mathf.Max(1, DamageScaler(hitDirection));
            _carry += amount * damageMultiplier;
            int whole = Mathf.FloorToInt(_carry + 0.0001f);
            _carry -= whole;

            LastDamageTime = Time.time;
            Current = Mathf.Max(0, Current - whole);
            if (hitInvulnerability > 0f) SetInvulnerable(hitInvulnerability);
            Damaged?.Invoke(whole, hitDirection);
            if (Current == 0) Died?.Invoke();
            return true;
        }

        /// <summary>Raises (or lowers) the health cap, optionally filling up to it.</summary>
        public void SetMaxHealth(int max, bool healFull)
        {
            maxHealth = Mathf.Max(1, max);
            Current = healFull ? maxHealth : Mathf.Min(Current, maxHealth);
        }

        /// <summary>Sets health directly, e.g. when a saved run is loaded.</summary>
        public void Restore(int current)
        {
            Current = Mathf.Clamp(current, 1, maxHealth);
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(maxHealth, Current + amount);
        }

        /// <summary>Dies now, ignoring armor, shells and invulnerability.</summary>
        public void Kill()
        {
            if (IsDead) return;
            Current = 0;
            Died?.Invoke();
        }
    }
}
