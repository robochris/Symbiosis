using UnityEngine;

namespace Shrunk
{
    /// <summary>Fires pooled bullets while the trigger is held, bending each shot toward the nearest enemy in the aim cone.</summary>
    public class Weapon : MonoBehaviour
    {
        public Bullet bulletPrefab;
        public Transform muzzle;

        [Header("Shot")]
        public float fireInterval = 0.18f;
        public float bulletSpeed = 22f;
        public int damage = 1;
        public float range = 18f;

        [Header("Aim assist")]
        [Tooltip("Half-angle of the cone, in degrees, that aim assist searches.")]
        public float aimAssistAngle = 20f;
        [Range(0f, 1f), Tooltip("0 = off, 1 = shots go straight at the target.")]
        public float aimAssistStrength = 1f;

        public int ShotsFired { get; private set; }

        Pool<Bullet> _pool;
        float _nextShot;

        void Awake()
        {
            _pool = new Pool<Bullet>(bulletPrefab);
        }

        /// <summary>Call once a frame. Aim direction is flat (no Y) and normalized.</summary>
        public void Tick(bool triggerHeld, Vector3 aimDirection)
        {
            if (!triggerHeld || Time.time < _nextShot || aimDirection.sqrMagnitude < 0.001f) return;
            _nextShot = Time.time + fireInterval;

            Vector3 origin = muzzle != null ? muzzle.position : transform.position;
            Vector3 direction = ApplyAimAssist(origin, aimDirection.normalized);
            _pool.Get().Launch(origin, direction, bulletSpeed, damage, range, Team.Player, _pool);
            ShotsFired++;
        }

        Vector3 ApplyAimAssist(Vector3 origin, Vector3 direction)
        {
            if (aimAssistStrength <= 0f) return direction;

            Enemy best = null;
            float bestAngle = aimAssistAngle;
            Vector3 bestDirection = direction;

            foreach (var enemy in Enemy.Active)
            {
                if (enemy.Health.IsDead) continue;
                Vector3 to = enemy.transform.position - origin;
                to.y = 0f;
                float distance = to.magnitude;
                if (distance > range || distance < 0.01f) continue;

                float angle = Vector3.Angle(direction, to);
                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    best = enemy;
                    bestDirection = to / distance;
                }
            }

            return best == null ? direction : Vector3.Slerp(direction, bestDirection, aimAssistStrength).normalized;
        }
    }
}
