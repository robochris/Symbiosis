using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The seventh kind: the one that takes its time. It finds a spot with a clear line, stands still, and paints a
    /// strip on the floor toward the player that grows for a little over a second. It follows them for most of that,
    /// then holds, then fires one very fast shot along the strip. The shot is too quick to dodge; the strip is not.
    /// </summary>
    public class SniperEnemy : Enemy
    {
        public enum State { Position, Aim, Rest }

        public Bullet projectilePrefab;
        public Transform muzzle;
        public Material telegraphMaterial;

        public float moveSpeed = 2.4f;
        public float turnSpeed = 400f;
        [Tooltip("The distance from the player it likes to shoot from.")]
        public float keepRange = 12f;
        [Tooltip("Seconds the strip is on the floor before the shot.")]
        public float aimTime = 1.25f;
        [Tooltip("For this long at the end of the aim it has stopped following: the time to step off the strip.")]
        public float holdTime = 0.4f;
        public float restTime = 2.4f;
        public float projectileSpeed = 34f;
        public float range = 26f;
        public float stripWidth = 0.7f;

        public State Current { get; private set; }
        public int Shots { get; private set; }

        static readonly Dictionary<Bullet, Pool<Bullet>> Pools = new Dictionary<Bullet, Pool<Bullet>>();
        float _timer;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = State.Position;
            _timer = Random.Range(0.6f, 1.6f);
            Shots = 0;
        }

        protected override void OnDisable()
        {
            HideChargeLine();
            base.OnDisable();
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                HideChargeLine();
                Move(Vector3.zero);
                return;
            }

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            bool sees = CanSee(targetPosition);

            switch (Current)
            {
                case State.Aim:
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime * SpeedScale;
                    // Follows until the last moment, then holds: that is the moment to step aside.
                    if (_timer > holdTime) Face(toTarget, turnSpeed);
                    ShowChargeLine(1f - Mathf.Clamp01(_timer / aimTime), range, stripWidth * BaseScale, telegraphMaterial);
                    if (_timer <= 0f)
                    {
                        Fire();
                        HideChargeLine();
                        Current = State.Rest;
                        _timer = restTime;
                    }
                    break;

                case State.Rest:
                    Move(Vector3.zero);
                    Face(toTarget, turnSpeed * 0.5f);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer <= 0f) Current = State.Position;
                    break;

                default:
                    // Somewhere with a clear line, at arm's length. Then it settles.
                    Vector3 heading;
                    if (!sees) heading = SteerToward(targetPosition) - transform.position;
                    else heading = toTarget * Mathf.Sign(distance - keepRange);
                    heading.y = 0f;
                    bool placed = sees && Mathf.Abs(distance - keepRange) < 3f;
                    Face(sees ? toTarget : heading, turnSpeed);
                    Move(placed || heading.sqrMagnitude < 0.01f ? Vector3.zero : heading.normalized * (moveSpeed * SpeedScale));
                    _timer -= Time.deltaTime * SpeedScale;
                    if (sees && distance <= range && _timer <= 0f)
                    {
                        Current = State.Aim;
                        _timer = aimTime;
                    }
                    break;
            }
        }

        void Fire()
        {
            Shots++;
            if (projectilePrefab == null) return;
            if (!Pools.TryGetValue(projectilePrefab, out var pool))
            {
                pool = new Pool<Bullet>(projectilePrefab);
                Pools.Add(projectilePrefab, pool);
            }
            Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * 0.7f;
            Vector3 direction = transform.forward;
            direction.y = 0f;
            pool.Get().Launch(origin, direction.normalized, projectileSpeed, new Shot { damage = 1, owner = Health, length = 2.2f }, range, IsConfused ? Team.Player : Team.Enemy, pool);
            Sfx.Play(SfxId.Shoot, 0.9f);
        }
    }
}
