using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Encyclopedia, and (with the pages switched off) the Bookend before it. Topple: it turns to the player, marks
    /// the strip of floor it is about to land on, and falls flat along it; then it lies there a moment, open to being
    /// hit, and gets up. Pages: a fan of loose pages thrown at the player, a few times over. At two thirds and one
    /// third it speeds up and sends for friends.
    /// </summary>
    public class BookBoss : BossEnemy
    {
        public enum Act { Idle, Lean, Down, Rise, Pages }

        [Header("Topple")]
        public float leanTime = 1.15f;
        public float bookLength = 9f;
        public float bookWidth = 3.4f;
        public float markRadius = 1.2f;
        public float downTime = 1.6f;
        public float riseTime = 0.6f;
        [Tooltip("Seconds ahead of a moving player the topple is aimed.")]
        public float toppleLead = 0.45f;

        [Header("Pages")]
        public bool canThrowPages = true;
        public Bullet projectilePrefab;
        public int pagesPerFan = 5;
        public float pageSpread = 14f;
        public int[] fansPerVolley = { 2, 3, 4 };
        public float fanGap = 0.45f;
        public float projectileSpeed = 9f;
        public float projectileRange = 22f;

        [Header("Moving")]
        public float moveSpeed = 2.2f;
        public float turnSpeed = 220f;
        public float restBetween = 1.5f;

        [Header("Phases")]
        [Range(0f, 1f)] public float phase2At = 0.66f;
        [Range(0f, 1f)] public float phase3At = 0.33f;
        public float[] phaseSpeed = { 1f, 1.15f, 1.3f };
        public Enemy[] friends;
        public int friendsPerPhase = 2;

        public Act Current { get; private set; }
        public int Topples { get; private set; }
        public int Fans { get; private set; }

        static readonly Dictionary<Bullet, Pool<Bullet>> Pools = new Dictionary<Bullet, Pool<Bullet>>();
        float _timer;
        int _turn, _fansLeft;
        Vector3 _fallDirection;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = Act.Idle;
            _timer = 1.5f;
            Topples = Fans = _turn = 0;
            Tilt(0f);
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                Move(Vector3.zero);
                return;
            }

            float health = Health01;
            if (Current == Act.Idle && ((Phase == 1 && health <= phase2At) || (Phase == 2 && health <= phase3At))) NextPhase();

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            _timer -= Time.deltaTime * SpeedScale;

            switch (Current)
            {
                case Act.Lean:
                    Move(Vector3.zero);
                    // A wobble that grows as it goes over.
                    Tilt(Mathf.Sin(Time.time * 20f) * 6f * (1f - Mathf.Clamp01(_timer / leanTime)));
                    if (_timer > 0f) break;
                    Current = Act.Down;
                    _timer = downTime;
                    Tilt(85f);
                    if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.4f);
                    Sfx.Play(SfxId.BossAppear, 0.5f);
                    break;

                case Act.Down:
                    Move(Vector3.zero);
                    if (_timer > 0f) break;
                    Current = Act.Rise;
                    _timer = riseTime;
                    break;

                case Act.Rise:
                    Move(Vector3.zero);
                    Tilt(85f * Mathf.Clamp01(_timer / riseTime));
                    if (_timer > 0f) break;
                    Tilt(0f);
                    Rest();
                    break;

                case Act.Pages:
                    Face(toTarget, turnSpeed);
                    Move(Vector3.zero);
                    if (_timer > 0f) break;
                    ThrowFan(toTarget);
                    _fansLeft--;
                    _timer = fanGap;
                    if (_fansLeft <= 0) Rest();
                    break;

                default:
                    Face(toTarget, turnSpeed);
                    Move(toTarget.magnitude > 6f ? (SteerToward(targetPosition) - transform.position).normalized * (moveSpeed * SpeedScale) : Vector3.zero);
                    if (_timer > 0f) break;
                    _turn++;
                    if (canThrowPages && projectilePrefab != null && _turn % 2 == 0) StartPages();
                    else StartTopple(targetPosition);
                    break;
            }
        }

        // Turns to where the player is heading and marks the strip it will land on: its own length, out in front.
        void StartTopple(Vector3 targetPosition)
        {
            Vector3 aim = Ahead(targetPosition, toppleLead) - transform.position;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
            _fallDirection = aim.normalized;
            transform.rotation = Quaternion.LookRotation(_fallDirection);
            Vector3 across = Vector3.Cross(Vector3.up, _fallDirection);
            float fuse = leanTime / Mathf.Max(0.1f, SpeedScale);
            for (float d = 1.2f; d <= bookLength; d += markRadius * 1.5f)
            {
                for (int row = -1; row <= 1; row += 2)
                {
                    Vector3 at = transform.position + _fallDirection * d + across * (row * bookWidth * 0.25f);
                    var blast = GroundHazard.SpawnBlast(new Vector3(at.x, 0f, at.z), markRadius, fuse, telegraphMaterial);
                    blast.source = this;
                }
            }
            Topples++;
            Current = Act.Lean;
            _timer = leanTime;
        }

        void StartPages()
        {
            Current = Act.Pages;
            _fansLeft = fansPerVolley != null && fansPerVolley.Length >= Phase ? fansPerVolley[Phase - 1] : 2;
            _timer = 0.5f;
        }

        void ThrowFan(Vector3 toTarget)
        {
            if (projectilePrefab == null) return;
            if (!Pools.TryGetValue(projectilePrefab, out var pool))
            {
                pool = new Pool<Bullet>(projectilePrefab);
                Pools.Add(projectilePrefab, pool);
            }
            Vector3 forward = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : transform.forward;
            Vector3 origin = transform.position + Vector3.up * 1.2f + forward * 1.4f;
            // Alternate fans are offset by half a gap, so standing still in one gap is not safe twice.
            float offset = Fans % 2 == 0 ? 0f : pageSpread * 0.5f;
            for (int i = 0; i < pagesPerFan; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, (i - (pagesPerFan - 1) * 0.5f) * pageSpread + offset, 0f) * forward;
                pool.Get().Launch(origin, direction, projectileSpeed, new Shot { damage = 1, owner = Health, size = 1.3f }, projectileRange, IsConfused ? Team.Player : Team.Enemy, pool);
            }
            Fans++;
            Sfx.Play(SfxId.Shoot, 0.5f);
        }

        // Leans the model forward by this many degrees, about its feet.
        void Tilt(float degrees)
        {
            if (visual != null) visual.localRotation = Quaternion.Euler(degrees, 0f, 0f);
        }

        void Rest()
        {
            Current = Act.Idle;
            _timer = restBetween;
        }

        void NextPhase()
        {
            Phase++;
            SpeedMultiplier = phaseSpeed != null && phaseSpeed.Length >= Phase ? phaseSpeed[Phase - 1] : 1f;
            Sfx.Play(SfxId.BossAppear);
            for (int i = 0; friends != null && friends.Length > 0 && i < friendsPerPhase; i++)
            {
                Enemy friend = friends[Random.Range(0, friends.Length)];
                float side = i % 2 == 0 ? -1f : 1f;
                if (friend != null) RequestMinion(friend, transform.position + transform.right * (side * 3.5f), false);
            }
        }
    }
}
