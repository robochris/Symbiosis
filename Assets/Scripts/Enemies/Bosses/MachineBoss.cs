using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The machine: the portal itself, switched on with all four of its parts back in. It does not move. It throws
    /// rings of shots with gaps to stand in, turning a little between rings; now and then it pulls the player in
    /// toward itself with the floor round it about to go off, so the pull has to be walked against; and at two thirds
    /// and one third of its health it lets something through from one of the other worlds. Beating it does not break
    /// it. It restarts, and everything starts again a cycle harder.
    /// </summary>
    public class MachineBoss : BossEnemy
    {
        [Header("Rings")]
        public Bullet projectilePrefab;
        public float volleyInterval = 4f;
        [Tooltip("Shots in a ring in phase 1; two more each phase.")]
        public int spokes = 6;
        [Tooltip("Rings in a volley, in each phase.")]
        public int[] rings = { 1, 2, 3 };
        public float ringGap = 0.45f;
        public float projectileSpeed = 6.5f;
        public float projectileRange = 30f;
        [Tooltip("Degrees it turns in a second. The rings turn with it.")]
        public float spin = 14f;

        [Header("Pull")]
        public float pullInterval = 12f;
        [Tooltip("Seconds the pull lasts, and the fuse on the floor round it.")]
        public float pullTime = 2.4f;
        public float pullSpeed = 3.2f;
        public float blastRadius = 7f;

        [Header("Phases")]
        [Range(0f, 1f)] public float phase2At = 0.66f;
        [Range(0f, 1f)] public float phase3At = 0.33f;
        public float[] phaseSpeed = { 1f, 1.15f, 1.3f };
        [Tooltip("What it lets through from the other worlds at each phase change.")]
        public Enemy[] visitors;
        public int visitorsPerPhase = 2;

        public int Rings { get; private set; }
        public int Pulls { get; private set; }
        public int VisitorsLetThrough { get; private set; }
        public bool Pulling => Time.time < _pullUntil;

        static readonly Dictionary<Bullet, Pool<Bullet>> Pools = new Dictionary<Bullet, Pool<Bullet>>();

        float _nextVolley, _nextRing, _nextPull, _pullUntil, _angle;
        int _ringsLeft;
        bool _started;

        protected override void OnEnable()
        {
            base.OnEnable();
            Rings = Pulls = VisitorsLetThrough = 0;
            _ringsLeft = 0;
            _pullUntil = 0f;
            _started = false;
        }

        protected override void Think()
        {
            Move(Vector3.zero);
            transform.Rotate(0f, spin * SpeedScale * Time.deltaTime, 0f);
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth)) return;

            if (!_started)
            {
                _started = true;
                _nextVolley = Time.time + 1.5f;
                _nextPull = Time.time + pullInterval * 0.7f;
            }

            float health = Health01;
            if ((Phase == 1 && health <= phase2At) || (Phase == 2 && health <= phase3At)) NextPhase();

            // The pull: toward the middle, while the floor round the machine counts down.
            if (Pulling)
            {
                var player = PlayerController.Instance;
                Vector3 toward = transform.position - targetPosition;
                toward.y = 0f;
                if (player != null && toward.sqrMagnitude > 1f) player.Push(toward.normalized * (pullSpeed * SpeedScale));
            }
            else if (Time.time >= _nextPull && _ringsLeft == 0)
            {
                _nextPull = Time.time + pullInterval / SpeedScale;
                _pullUntil = Time.time + pullTime;
                _nextVolley = Mathf.Max(_nextVolley, _pullUntil + 0.6f);
                GroundHazard.SpawnBlast(new Vector3(transform.position.x, 0f, transform.position.z), blastRadius, pullTime, telegraphMaterial);
                Sfx.Play(SfxId.BossAppear);
                Pulls++;
            }

            if (_ringsLeft == 0 && !Pulling && Time.time >= _nextVolley)
            {
                _nextVolley = Time.time + volleyInterval / SpeedScale;
                _ringsLeft = rings != null && rings.Length >= Phase ? rings[Phase - 1] : 2;
                _nextRing = Time.time;
            }

            if (_ringsLeft > 0 && Time.time >= _nextRing)
            {
                _ringsLeft--;
                _nextRing = Time.time + ringGap;
                FireRing();
            }
        }

        void NextPhase()
        {
            Phase++;
            SpeedMultiplier = phaseSpeed != null && phaseSpeed.Length >= Phase ? phaseSpeed[Phase - 1] : 1f;
            Sfx.Play(SfxId.BossAppear);
            Fx.Burst(transform.position + Vector3.up * 2f, new Color(0.45f, 0.95f, 1f), 30, 8f, 0.25f, 0.6f);
            for (int i = 0; visitors != null && visitors.Length > 0 && i < visitorsPerPhase; i++)
            {
                Enemy visitor = visitors[Random.Range(0, visitors.Length)];
                if (visitor == null) continue;
                Vector3 out_ = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * (Controller.radius + 4f);
                RequestMinion(visitor, transform.position + out_, true);
                VisitorsLetThrough++;
            }
        }

        // Evenly round, each ring half a step on from the last, so the gap to stand in keeps moving.
        void FireRing()
        {
            if (projectilePrefab == null) return;
            if (!Pools.TryGetValue(projectilePrefab, out var pool))
            {
                pool = new Pool<Bullet>(projectilePrefab);
                Pools.Add(projectilePrefab, pool);
            }

            int count = spokes + (Phase - 1) * 2;
            float step = 360f / count;
            _angle += step * 0.5f;
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, transform.eulerAngles.y + _angle + i * step, 0f) * Vector3.forward;
                Vector3 origin = transform.position + direction * (Controller.radius + 0.4f) + Vector3.up * 0.7f;
                pool.Get().Launch(origin, direction, projectileSpeed * SpeedScale, new Shot { damage = 1, owner = Health }, projectileRange, IsConfused ? Team.Player : Team.Enemy, pool);
            }
            Rings++;
            Sfx.Play(SfxId.Blast, 0.5f);
        }
    }
}
