using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Goldfish, and (with the dash switched off and the shell switched on) the Snail before it.
    /// Dash: a strip painted toward the player, a pause, then a rush along it that hurts whoever is in the way.
    /// Bubbles: a fan of big slow bubbles, easy to see and easy to walk between. Shell (the Snail): it pulls in and
    /// cannot be hurt for a moment, leaving slime about that slows. At two thirds and one third it speeds up and sends
    /// for friends.
    /// </summary>
    public class FishBoss : BossEnemy
    {
        public enum Act { Swim, DashWindup, Dash, Bubbles, Shell }

        [Header("Moving")]
        public float moveSpeed = 2.6f;
        public float turnSpeed = 260f;
        public float keepRange = 8f;

        [Header("Dash")]
        public bool canDash = true;
        public float dashWindup = 1f;
        public float dashSpeed = 18f;
        public float dashTime = 0.6f;
        public float dashWidth = 2.2f;
        public float dashReach = 1.8f;

        [Header("Bubbles")]
        public Bullet bubblePrefab;
        [Tooltip("Bubbles in a fan, in each phase.")]
        public int[] bubblesPerFan = { 4, 5, 7 };
        public float bubbleSpread = 14f;
        public float bubbleSpeed = 4.5f;
        public float bubbleRange = 18f;

        [Header("Shell")]
        public bool canShell;
        public float shellTime = 2.5f;
        public int slimePerShell = 3;
        public Material slimeMaterial;

        [Header("Rhythm")]
        public float restBetween = 1.4f;

        [Header("Phases")]
        [Range(0f, 1f)] public float phase2At = 0.66f;
        [Range(0f, 1f)] public float phase3At = 0.33f;
        public float[] phaseSpeed = { 1f, 1.15f, 1.3f };
        public Enemy[] friends;
        public int friendsPerPhase = 2;

        public Act Current { get; private set; }
        public int Dashes { get; private set; }
        public int Fans { get; private set; }
        public int Shells { get; private set; }
        /// <summary>Times a dash caught the player.</summary>
        public int DashHits { get; private set; }
        public bool Shelled => Current == Act.Shell;

        static readonly Dictionary<Bullet, Pool<Bullet>> Pools = new Dictionary<Bullet, Pool<Bullet>>();
        float _timer;
        int _turn;
        bool _hit;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = Act.Swim;
            _timer = 1.5f;
            Dashes = Fans = Shells = DashHits = 0;
            _turn = 0;
        }

        protected override void OnDisable()
        {
            HideChargeLine();
            if (Health != null) Health.ClearInvulnerability();
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

            float health = Health01;
            if (Current != Act.Dash && ((Phase == 1 && health <= phase2At) || (Phase == 2 && health <= phase3At))) NextPhase();

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            _timer -= Time.deltaTime * (Current == Act.Dash ? 1f : SpeedScale);

            switch (Current)
            {
                case Act.DashWindup:
                    Move(Vector3.zero);
                    if (_timer > dashWindup * 0.3f) Face(toTarget, turnSpeed * 1.5f);
                    ShowChargeLine(1f - Mathf.Clamp01(_timer / dashWindup), dashSpeed * dashTime, dashWidth, telegraphMaterial);
                    if (_timer > 0f) break;
                    HideChargeLine();
                    Current = Act.Dash;
                    _timer = dashTime;
                    _hit = false;
                    Dashes++;
                    break;

                case Act.Dash:
                    Move(transform.forward * dashSpeed);
                    if (!_hit && targetHealth != null && toTarget.magnitude <= dashReach)
                    {
                        _hit = targetHealth.TakeDamage(1, transform.forward);
                        if (_hit && targetHealth.GetComponent<PlayerController>() != null) DashHits++;
                    }
                    if (_timer <= 0f) Rest();
                    break;

                case Act.Shell:
                    Move(Vector3.zero);
                    SetVisualScale(0.8f);
                    if (_timer > 0f) break;
                    SetVisualScale(1f);
                    Health.ClearInvulnerability();
                    Rest();
                    break;

                default:
                    Swim(targetPosition, toTarget);
                    if (_timer <= 0f) NextMove(toTarget);
                    break;
            }
        }

        void NextMove(Vector3 toTarget)
        {
            _turn++;
            if (_turn % 2 == 1 && canDash)
            {
                Current = Act.DashWindup;
                _timer = dashWindup;
                return;
            }
            if (_turn % 3 == 2 && canShell)
            {
                // Pulls in, and leaves a trail of slime round about on the way.
                Current = Act.Shell;
                _timer = shellTime;
                Shells++;
                Health.SetInvulnerable(shellTime);
                for (int i = 0; i < slimePerShell + Phase - 1; i++)
                {
                    Vector2 scatter = Random.insideUnitCircle * 5f;
                    GroundHazard.SpawnStickyPatch(transform.position + new Vector3(scatter.x, 0f, scatter.y), slimeMaterial);
                }
                Sfx.Play(SfxId.Blocked);
                return;
            }
            Face(toTarget, 720f);
            BlowBubbles();
            Rest();
        }

        // A fan of big slow bubbles: the spaces between them are the way through.
        void BlowBubbles()
        {
            Fans++;
            if (bubblePrefab == null) return;
            if (!Pools.TryGetValue(bubblePrefab, out var pool))
            {
                pool = new Pool<Bullet>(bubblePrefab);
                Pools.Add(bubblePrefab, pool);
            }
            int count = bubblesPerFan != null && bubblesPerFan.Length >= Phase ? bubblesPerFan[Phase - 1] : 5;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            for (int i = 0; i < count; i++)
            {
                float turn = (i - (count - 1) * 0.5f) * bubbleSpread;
                Vector3 direction = Quaternion.Euler(0f, turn, 0f) * forward.normalized;
                pool.Get().Launch(transform.position + Vector3.up * 0.8f + direction * 1.5f, direction, bubbleSpeed, new Shot { damage = 1, owner = Health, size = 1.8f }, bubbleRange, IsConfused ? Team.Player : Team.Enemy, pool);
            }
            Sfx.Play(SfxId.Shoot, 0.5f);
        }

        void Rest()
        {
            Current = Act.Swim;
            _timer = restBetween;
        }

        void Swim(Vector3 targetPosition, Vector3 toTarget)
        {
            float distance = toTarget.magnitude;
            Vector3 heading;
            if (!CanSee(targetPosition))
            {
                heading = SteerToward(targetPosition) - transform.position;
                heading.y = 0f;
            }
            else
            {
                Vector3 radial = distance > 0.01f ? toTarget / distance : transform.forward;
                heading = Quaternion.Euler(0f, 90f, 0f) * radial * 0.7f + radial * Mathf.Clamp((distance - keepRange) / 3f, -1f, 1f);
            }
            Face(toTarget, turnSpeed);
            Move(heading.sqrMagnitude < 0.01f ? Vector3.zero : heading.normalized * (moveSpeed * SpeedScale));
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
