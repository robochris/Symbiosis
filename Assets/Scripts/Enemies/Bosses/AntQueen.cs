using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Floor 10 boss. Phase 1: sits on her throne, spits acid in a fan and calls two Soldiers, while egg clusters around
    /// the chamber hatch one at a time. Phase 2 (below 60%): leaves the throne and charges in straight lines, leaving an
    /// acid trail. Phase 3 (below 25%): a royal scream hatches every egg that is left, and she speeds up.
    /// Shooting the eggs first makes the fight easier. Killing her with every egg intact is counted, for a secret later.
    /// </summary>
    public class AntQueen : BossEnemy
    {
        public enum State { Throne, Rest, Windup, Charge, Scream }

        [Header("Eggs")]
        public EggEnemy eggPrefab;
        public int eggCount = 5;
        [Tooltip("Seconds between an egg hatching by itself.")]
        public float hatchInterval = 15f;
        [Tooltip("How far from her the egg clusters sit, when no wall is nearer.")]
        public float eggRing = 11f;

        [Header("Soldiers")]
        public Enemy soldierPrefab;
        public int soldiersCalled = 2;

        [Header("Acid fan")]
        public Bullet projectilePrefab;
        public Transform muzzle;
        public float fanInterval = 3.4f;
        [Tooltip("Degrees between the three shots.")]
        public float fanSpread = 18f;
        public float projectileSpeed = 8.5f;
        public float projectileRange = 26f;
        [Range(0f, 1f), Tooltip("How much the middle shot of the fan aims ahead of a moving target. 0 = straight at them, 1 = exactly where they will be.")]
        public float fanLead = 0.4f;
        public float turnSpeed = 160f;

        [Header("Charge (phase 2 and 3)")]
        [Range(0f, 1f)] public float phase2At = 0.6f;
        [Range(0f, 1f)] public float phase3At = 0.25f;
        public float restTime = 1.4f;
        public float chargeWindup = 0.9f;
        public float chargeSpeed = 15f;
        [Tooltip("Seconds a charge lasts if she hits nothing: 1.7 s at 15 m/s crosses most of a 30 m arena.")]
        public float chargeTime = 1.7f;
        public float hitReach = 3.6f;
        public int chargeDamage = 1;
        [Tooltip("Metres between acid pools left along the charge.")]
        public float trailSpacing = 2.6f;
        public Material acidMaterial;

        [Header("Scream (phase 3)")]
        public float screamTime = 1f;
        public float enragedSpeed = 1.4f;

        [Header("Rewards")]
        [Range(0f, 1f)] public float jellyChance = 0.35f;

        public State Current { get; private set; }
        public int EggsPlaced { get; private set; }
        public int EggsHatched { get; private set; }
        /// <summary>Eggs neither hatched nor shot. If this equals EggsPlaced when she dies, every egg was left intact.</summary>
        public int EggsRemaining => EggEnemy.All.Count;
        public int EggsDestroyed => Mathf.Max(0, EggsPlaced - EggsHatched - EggsRemaining);
        public int FansFired { get; private set; }
        public int Charges { get; private set; }
        public bool Screamed { get; private set; }

        static readonly Dictionary<Bullet, Pool<Bullet>> Pools = new Dictionary<Bullet, Pool<Bullet>>();

        float _timer;
        float _nextFan;
        float _nextHatch;
        float _sinceTrail;
        bool _started;
        bool _hit;
        Vector3 _lastTargetPosition;
        Vector3 _targetVelocity;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = State.Throne;
            _started = false;
            Screamed = false;
            EggsPlaced = EggsHatched = FansFired = Charges = 0;
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                Move(Vector3.zero);
                return;
            }

            if (!_started)
            {
                Begin();
                _lastTargetPosition = targetPosition;
            }

            // Keep a smoothed idea of how the target is moving, so the fan can aim ahead of a player who just runs in circles.
            if (Time.deltaTime > 0f)
            {
                Vector3 velocity = (targetPosition - _lastTargetPosition) / Time.deltaTime;
                velocity.y = 0f;
                _targetVelocity = Vector3.Lerp(_targetVelocity, velocity, 0.15f);
                _lastTargetPosition = targetPosition;
            }

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            // Eggs hatch one at a time on a clock, in every phase.
            if (Time.time >= _nextHatch)
            {
                _nextHatch = Time.time + hatchInterval;
                HatchOne();
            }

            float health = Health01;
            if (!Screamed && health <= phase3At)
            {
                Phase = 3;
                Screamed = true;
                Current = State.Scream;
                _timer = screamTime;
            }
            else if (Phase == 1 && health <= phase2At)
            {
                Phase = 2;
                Current = State.Rest;
                _timer = restTime;
            }

            switch (Current)
            {
                case State.Throne:
                    Face(toTarget, turnSpeed);
                    Move(Vector3.zero);
                    TryFan(toTarget);
                    break;

                case State.Scream:
                    // Rears up and swells; when it ends, every egg left hatches at once.
                    SetVisualScale(1f + 0.25f * Mathf.Sin((1f - _timer / screamTime) * Mathf.PI));
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f)
                    {
                        SetVisualScale(1f);
                        while (EggEnemy.All.Count > 0 && HatchOne()) { }
                        SpeedMultiplier *= enragedSpeed;
                        Current = State.Rest;
                        _timer = restTime * 0.5f;
                    }
                    break;

                case State.Rest:
                    Face(toTarget, turnSpeed);
                    Move(Vector3.zero);
                    TryFan(toTarget);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer <= 0f && CanSee(targetPosition))
                    {
                        Current = State.Windup;
                        _timer = chargeWindup;
                    }
                    break;

                case State.Windup:
                    // Lowers her head and takes aim: the telegraph. The direction locks when it ends.
                    Face(toTarget, turnSpeed * 1.5f);
                    SetVisualScale(Mathf.Lerp(1.2f, 1f, _timer / chargeWindup));
                    ShowChargeLine(1f - _timer / chargeWindup, chargeSpeed * SpeedScale * chargeTime, 3.2f, telegraphMaterial);
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer <= 0f)
                    {
                        HideChargeLine();
                        SetVisualScale(1f);
                        Charges++;
                        _hit = false;
                        _sinceTrail = trailSpacing;
                        Current = State.Charge;
                        _timer = chargeTime;
                    }
                    break;

                case State.Charge:
                    float step = chargeSpeed * SpeedScale * Time.deltaTime;
                    Move(transform.forward * (chargeSpeed * SpeedScale));
                    _sinceTrail += step;
                    if (_sinceTrail >= trailSpacing)
                    {
                        _sinceTrail = 0f;
                        GroundHazard.SpawnAcidPool(transform.position, acidMaterial);
                    }
                    if (!_hit && distance <= hitReach + ReachBonus(targetHealth)) _hit = targetHealth.TakeDamage(chargeDamage, transform.forward);

                    _timer -= Time.deltaTime;
                    bool crashed = _timer < chargeTime - 0.15f && (Controller.collisionFlags & CollisionFlags.Sides) != 0;
                    if (crashed || _timer <= 0f)
                    {
                        Current = State.Rest;
                        _timer = restTime;
                    }
                    break;
            }
        }

        void Begin()
        {
            _started = true;
            _nextFan = Time.time + 1.5f;
            _nextHatch = Time.time + hatchInterval;

            // Egg clusters go against the walls, evenly round the chamber.
            if (eggPrefab != null)
            {
                Vector3 origin = transform.position + Vector3.up * 0.7f;
                for (int i = 0; i < eggCount; i++)
                {
                    Vector3 direction = Quaternion.Euler(0f, 360f * i / eggCount + 30f, 0f) * Vector3.forward;
                    float reach = eggRing;
                    if (Physics.Raycast(origin, direction, out var hit, eggRing + 2f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<Health>() == null)
                    {
                        reach = Mathf.Max(3f, hit.distance - 1.6f);
                    }
                    RequestMinion(eggPrefab, transform.position + direction * reach, false);
                    EggsPlaced++;
                }
            }

            // Her two guards, one at each side.
            if (soldierPrefab != null)
            {
                for (int i = 0; i < soldiersCalled; i++)
                {
                    float side = i % 2 == 0 ? -1f : 1f;
                    RequestMinion(soldierPrefab, transform.position + transform.right * (side * 4.5f) + transform.forward * 2f, false);
                }
            }
        }

        bool HatchOne()
        {
            if (EggEnemy.All.Count == 0) return false;
            EggEnemy egg = EggEnemy.All[Random.Range(0, EggEnemy.All.Count)];
            int before = EggEnemy.All.Count;
            egg.Hatch();
            if (EggEnemy.All.Count == before) return false; // could not hatch; don't loop forever
            EggsHatched++;
            return true;
        }

        void TryFan(Vector3 toTarget)
        {
            if (Time.time < _nextFan || projectilePrefab == null || toTarget.sqrMagnitude < 0.01f) return;
            if (Vector3.Angle(transform.forward, toTarget) > 25f) return; // still turning to face
            _nextFan = Time.time + fanInterval / SpeedScale;
            FansFired++;

            if (!Pools.TryGetValue(projectilePrefab, out var pool))
            {
                pool = new Pool<Bullet>(projectilePrefab);
                Pools.Add(projectilePrefab, pool);
            }

            Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * 0.7f;

            // The middle shot goes where the target is heading; the outer two bracket it. Changing direction beats it.
            float flightTime = toTarget.magnitude / projectileSpeed;
            Vector3 aimAt = toTarget + _targetVelocity * (flightTime * fanLead);
            Vector3 forward = aimAt.sqrMagnitude > 0.01f ? aimAt.normalized : toTarget.normalized;
            for (int i = -1; i <= 1; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, fanSpread * i, 0f) * forward;
                pool.Get().Launch(origin, direction, projectileSpeed, new Shot { damage = 1, owner = Health }, projectileRange, Team.Enemy, pool);
            }
        }

        public override void DropRewards()
        {
            base.DropRewards();

            var assets = GameAssets.Instance;
            if (assets == null || assets.items == null || !Chance(jellyChance)) return;
            var jelly = assets.items.Get(ItemType.QueensJelly);
            if (jelly != null) Pickup.Item(jelly, transform.position + Vector3.forward * 2f, 0);
        }
    }
}
