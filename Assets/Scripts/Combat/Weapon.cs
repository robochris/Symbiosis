using UnityEngine;

namespace Shrunk
{
    /// <summary>Fires pooled bullets while the trigger is held, bending each shot toward the nearest enemy in the aim cone. Items change what each shot carries.</summary>
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
        PlayerItems _items;
        Health _health;
        float _nextShot;

        void Awake()
        {
            _pool = new Pool<Bullet>(bulletPrefab);
            _items = GetComponent<PlayerItems>();
            _health = GetComponent<Health>();
        }

        PlayerStats Stats => _items != null ? _items.Stats : PlayerStats.Default;

        /// <summary>Call once a frame. Aim direction is flat (no Y) and normalized.</summary>
        public void Tick(bool triggerHeld, Vector3 aimDirection)
        {
            if (!triggerHeld || Time.time < _nextShot || aimDirection.sqrMagnitude < 0.001f) return;

            PlayerStats stats = Stats;
            _nextShot = Time.time + fireInterval * stats.fireIntervalMultiplier;

            var shot = new Shot
            {
                damage = damage * stats.damageMultiplier,
                pierce = stats.pierce,
                split = stats.split,
                acid = stats.acid,
                burn = stats.burn,
                crit = Random.value < stats.critChance,
                owner = _health
            };

            Vector3 origin = muzzle != null ? muzzle.position : transform.position;
            Vector3 direction = ApplyAimAssist(origin, aimDirection.normalized, aimAssistAngle + stats.aimAssistBonusAngle);
            _pool.Get().Launch(origin, direction, bulletSpeed, shot, range, Team.Player, _pool);
            ShotsFired++;
            Sfx.Play(SfxId.Shoot, 0.6f);
        }

        /// <summary>True when an enemy is inside the aim cone and in range. Used for auto-fire on touch.</summary>
        public bool HasTarget(Vector3 aimDirection)
        {
            if (aimDirection.sqrMagnitude < 0.001f) return false;
            Vector3 origin = muzzle != null ? muzzle.position : transform.position;
            if (FindTarget(origin, aimDirection.normalized, aimAssistAngle + Stats.aimAssistBonusAngle, out _) != null) return true;

            // Larva pods count too, but only when aimed at squarely: shots are never bent toward them.
            foreach (var pod in LarvaPod.All)
            {
                Vector3 to = pod.transform.position - origin;
                to.y = 0f;
                if (to.magnitude <= range && Vector3.Angle(aimDirection, to) < 7f) return true;
            }
            return false;
        }

        Vector3 ApplyAimAssist(Vector3 origin, Vector3 direction, float coneAngle)
        {
            if (aimAssistStrength <= 0f) return direction;
            return FindTarget(origin, direction, coneAngle, out Vector3 toTarget) == null
                ? direction
                : Vector3.Slerp(direction, toTarget, aimAssistStrength).normalized;
        }

        // The enemy closest to the centre of the aim cone, if any.
        Enemy FindTarget(Vector3 origin, Vector3 direction, float coneAngle, out Vector3 bestDirection)
        {
            Enemy best = null;
            float bestAngle = coneAngle;
            bestDirection = direction;

            foreach (var enemy in Enemy.Active)
            {
                if (enemy.Health.IsDead || !enemy.Targetable) continue;
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

            return best;
        }
    }
}
