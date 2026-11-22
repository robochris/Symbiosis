using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Shooter (Spitter ant): keeps its distance, backs off if the player closes in, and spits slow acid shots.
    /// Break line of sight or get in its face to stop it.
    /// </summary>
    public class ShooterEnemy : Enemy
    {
        [Header("Positioning")]
        public float moveSpeed = 3.2f;
        public float turnSpeed = 360f;
        [Tooltip("Backs away when the player is closer than this.")]
        public float minRange = 6f;
        [Tooltip("Moves in when the player is farther than this, or out of sight.")]
        public float maxRange = 10f;

        [Header("Spit")]
        public Bullet projectilePrefab;
        public Transform muzzle;
        public float fireInterval = 2.2f;
        [Tooltip("Swells for this long before each shot: the telegraph.")]
        public float windupTime = 0.45f;
        public float projectileSpeed = 9f;
        public float projectileRange = 22f;
        public int damage = 1;

        public int ShotsFired { get; private set; }

        // Every spitter shares one pool per projectile prefab.
        static readonly Dictionary<Bullet, Pool<Bullet>> Pools = new Dictionary<Bullet, Pool<Bullet>>();

        float _nextShot;
        float _windup;
        bool _winding;

        protected override void OnEnable()
        {
            base.OnEnable();
            _winding = false;
            _nextShot = Time.time + 1f;
            ShotsFired = 0;
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                SetVisualScale(1f);
                Move(Vector3.zero);
                return;
            }

            Vector3 toPlayer = targetPosition - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            bool sees = CanSee(targetPosition);

            if (_winding)
            {
                Face(toPlayer, turnSpeed);
                SetVisualScale(Mathf.Lerp(1.3f, 1f, _windup / windupTime));
                Move(Vector3.zero);
                _windup -= Time.deltaTime * SpeedScale;
                if (_windup <= 0f) Fire(toPlayer);
                return;
            }

            SetVisualScale(1f);
            if (distance < minRange)
            {
                // Back away while still facing the player.
                Face(toPlayer, turnSpeed);
                Move(-toPlayer.normalized * (moveSpeed * SpeedScale));
            }
            else if (distance > maxRange || !sees)
            {
                Face(SteerToward(targetPosition) - transform.position, turnSpeed);
                Move(transform.forward * (moveSpeed * SpeedScale));
            }
            else
            {
                Face(toPlayer, turnSpeed);
                Move(Vector3.zero);
            }

            if (Time.time >= _nextShot && sees && distance <= maxRange + 2f)
            {
                _winding = true;
                _windup = windupTime;
            }
        }

        void Fire(Vector3 toPlayer)
        {
            _winding = false;
            _nextShot = Time.time + fireInterval / SpeedScale;
            if (projectilePrefab == null || toPlayer.sqrMagnitude < 0.01f) return;

            if (!Pools.TryGetValue(projectilePrefab, out var pool))
            {
                pool = new Pool<Bullet>(projectilePrefab);
                Pools.Add(projectilePrefab, pool);
            }

            Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * 0.7f;
            // A confused spitter's shots hurt other enemies.
            var shot = new Shot { damage = damage, owner = Health };
            // A player on another level is aimed at chest high, so the shot clears the ledge's edge.
            Vector3 aim = Mathf.Abs(toPlayer.y) > 0.8f ? transform.position + toPlayer + Vector3.up * 0.9f - origin : toPlayer;
            pool.Get().Launch(origin, aim.normalized, projectileSpeed, shot, projectileRange, IsConfused ? Team.Player : Team.Enemy, pool);
            ShotsFired++;
        }
    }
}
