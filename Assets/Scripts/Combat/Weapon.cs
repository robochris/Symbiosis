using UnityEngine;

namespace Shrunk
{
    /// <summary>The weapons the lab issues. Chosen at the equipment cupboard and kept between runs.</summary>
    public enum WeaponKind { Pipette, Sprinkler, Syringe, SprayBottle, StapleGun }

    /// <summary>What the cupboard says about a weapon, and how it differs from the plain one.</summary>
    public class WeaponInfo
    {
        public WeaponKind kind;
        public string name;
        public string does;
        /// <summary>How to get it, as the player reads it. Empty for the one everybody starts with.</summary>
        public string needs;
        /// <summary>Shots per pull of the trigger, fanned out.</summary>
        public int pellets = 1;
        public float spread;
        public float intervalMultiplier = 1f, rangeMultiplier = 1f, speedMultiplier = 1f;
        public int damageMultiplier = 1, pierce;
        /// <summary>How its shots look: scale, and stretch along the way they are going.</summary>
        public float shotSize = 1f, shotLength = 1f;
        /// <summary>Shots per pull, one after another down the same line, this far apart in seconds.</summary>
        public int burst = 1;
        public float burstGap = 0.07f;
    }

    public static class Weapons
    {
        public static readonly WeaponInfo[] All =
        {
            new WeaponInfo { kind = WeaponKind.Pipette, name = "Pipette", does = "Standard issue. Quick, straight, and reaches across a room.", needs = "" },
            new WeaponInfo { kind = WeaponKind.Sprinkler, name = "Sprinkler", does = "Three shots in a fan, slower, and they do not travel far. For getting close, which is discouraged.",
                needs = "Issued after your first run.", pellets = 3, spread = 11f, shotSize = 0.75f, intervalMultiplier = 2.1f, rangeMultiplier = 0.55f },
            new WeaponInfo { kind = WeaponKind.Syringe, name = "Syringe", does = "One slow, heavy shot: triple damage, goes through the first thing it hits, and reaches further.",
                needs = "Issued once the Ant Queen has been dealt with.", intervalMultiplier = 3f, rangeMultiplier = 1.35f, speedMultiplier = 1.6f, damageMultiplier = 3, pierce = 1, shotSize = 1.15f, shotLength = 2.6f },
            new WeaponInfo { kind = WeaponKind.SprayBottle, name = "Spray Bottle", does = "Nearly twice as fast, and it gives out at half the distance. For people who would rather not aim.",
                needs = "Issued once the Librarian has been dealt with.", intervalMultiplier = 0.55f, rangeMultiplier = 0.5f, speedMultiplier = 0.85f, shotSize = 0.8f },
            new WeaponInfo { kind = WeaponKind.StapleGun, name = "Staple Gun", does = "Three staples in a quick burst, then a pause. Every one goes where the first one went.",
                needs = "Issued once the Glitch has been dealt with.", burst = 3, burstGap = 0.07f, intervalMultiplier = 2.4f, speedMultiplier = 1.25f, rangeMultiplier = 0.9f, shotSize = 0.7f, shotLength = 1.8f },
        };

        public static WeaponInfo Info(WeaponKind kind)
        {
            foreach (var info in All) if (info.kind == kind) return info;
            return All[0];
        }

        public static bool Unlocked(WeaponKind kind)
        {
            if (Achievements.EverythingUnlocked) return true;
            switch (kind)
            {
                case WeaponKind.Sprinkler: return Achievements.Has(AchievementId.MothersDay) || SaveSystem.Data.meta.deaths + SaveSystem.Data.meta.extractions > 0;
                case WeaponKind.Syringe: return Achievements.Has(AchievementId.Regicide);
                case WeaponKind.SprayBottle: return Specimens.Has("TheLibrarian");
                case WeaponKind.StapleGun: return Specimens.Has("TheGlitch");
                default: return true;
            }
        }

        /// <summary>The weapon picked at the cupboard, or the Pipette if that one is not issued.</summary>
        public static WeaponKind Chosen
        {
            get
            {
                var kind = (WeaponKind)Mathf.Clamp(SaveSystem.Data.meta.weapon, 0, All.Length - 1);
                return Unlocked(kind) ? kind : WeaponKind.Pipette;
            }
        }
    }

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

        /// <summary>Which of the lab's weapons this is. The player's is set from the save when the scene opens.</summary>
        [System.NonSerialized] public WeaponKind kind;

        /// <summary>How far a shot from the weapon being carried goes.</summary>
        public float Reach => range * Weapons.Info(kind).rangeMultiplier;

        public int ShotsFired { get; private set; }
        public int ChargedShots { get; private set; }
        int _shotsSinceLightning;
        bool _lightningNext;
        int _shotsSinceCharge;

        Pool<Bullet> _pool;
        PlayerItems _items;
        Health _health;
        float _nextShot;

        void Awake()
        {
            _pool = new Pool<Bullet>(bulletPrefab);
            _items = GetComponent<PlayerItems>();
            _health = GetComponent<Health>();
            if (_items != null) kind = Weapons.Chosen;
        }

        PlayerStats Stats => _items != null ? _items.Stats : PlayerStats.Default;

        /// <summary>Call once a frame. Aim direction is flat (no Y) and normalized.</summary>
        /// <param name="aimDistance">How far off the aimed-at point is, if known. The shot leaves the hand, off to one side, and is angled to pass through that point.</param>
        public void Tick(bool triggerHeld, Vector3 aimDirection, float aimDistance = 0f)
        {
            // The rest of a burst goes whether the trigger is still held or not, down the line the first one took.
            if (_burstLeft > 0 && Time.time >= _burstAt)
            {
                _burstLeft--;
                _burstAt = Time.time + Weapons.Info(kind).burstGap;
                Volley(_burstDirection, Stats, Weapons.Info(kind), false);
            }
            if (!triggerHeld || Time.time < _nextShot || aimDirection.sqrMagnitude < 0.001f) return;

            PlayerStats stats = Stats;
            WeaponInfo info = Weapons.Info(kind);
            _nextShot = Time.time + fireInterval * stats.fireIntervalMultiplier * info.intervalMultiplier;

            // Bottled Lightning: every so many shots, one brings the weather with it.
            _shotsSinceLightning++;
            bool lightning = stats.lightningEvery > 0 && _shotsSinceLightning >= stats.lightningEvery;
            if (lightning) _shotsSinceLightning = 0;
            _lightningNext = lightning;

            // Static Sock: every so many shots, one is charged.
            _shotsSinceCharge++;
            bool charged = stats.chargedEvery > 0 && _shotsSinceCharge >= stats.chargedEvery;
            if (charged)
            {
                _shotsSinceCharge = 0;
                ChargedShots++;
            }

            Vector3 origin = muzzle != null ? muzzle.position : transform.position;
            Vector3 direction = ApplyAimAssist(origin, Converge(origin, aimDirection.normalized, aimDistance), aimAssistAngle + stats.aimAssistBonusAngle);
            direction = Pitch(origin, direction, (aimAssistAngle + stats.aimAssistBonusAngle) * 1.5f);
            Volley(direction, stats, info, charged);
            if (info.burst > 1)
            {
                _burstLeft = info.burst - 1;
                _burstAt = Time.time + info.burstGap;
                _burstDirection = direction;
            }
        }

        int _burstLeft;
        float _burstAt;
        Vector3 _burstDirection;

        // One pull's worth of shots: the Sprinkler throws a fan; the middle shot is the aimed one.
        void Volley(Vector3 direction, PlayerStats stats, WeaponInfo info, bool charged)
        {
            Vector3 origin = muzzle != null ? muzzle.position : transform.position;
            for (int i = 0; i < info.pellets; i++)
            {
                var shot = new Shot
                {
                    damage = (damage + stats.damageBonus) * stats.damageMultiplier * (charged ? 2 : 1) * info.damageMultiplier,
                    pierce = stats.pierce + (charged ? 2 : 0) + info.pierce,
                    split = stats.split,
                    acid = stats.acid,
                    burn = stats.burn,
                    chill = stats.chill,
                    bounce = stats.bounce,
                    crit = Random.value < stats.critChance,
                    size = info.shotSize,
                    length = info.shotLength,
                    owner = _health,
                    farBonus = stats.farBonus,
                    returns = stats.returns,
                    lightning = _lightningNext && i == 0
                };
                float turn = (i - (info.pellets - 1) * 0.5f) * info.spread;
                _pool.Get().Launch(origin, Quaternion.Euler(0f, turn, 0f) * direction, bulletSpeed * info.speedMultiplier, shot, Reach, Team.Player, _pool);
            }
            _lightningNext = false;
            ShotsFired++;
            Sfx.Play(SfxId.Shoot, 0.6f);
        }

        /// <summary>True when an enemy is inside the aim cone and in range. Used for auto-fire on touch.</summary>
        public bool HasTarget(Vector3 aimDirection)
        {
            if (aimDirection.sqrMagnitude < 0.001f) return false;
            Vector3 origin = muzzle != null ? muzzle.position : transform.position;
            float range = Reach;
            if (FindTarget(origin, aimDirection.normalized, aimAssistAngle + Stats.aimAssistBonusAngle, out _)) return true;

            // Larva pods count too, but only when aimed at squarely: shots are never bent toward them.
            foreach (var pod in LarvaPod.All)
            {
                Vector3 to = pod.transform.position - origin;
                to.y = 0f;
                if (to.magnitude <= range && Vector3.Angle(aimDirection, to) < 7f) return true;
            }
            return false;
        }

        [Tooltip("With nothing known under the crosshair, shots from the hand cross the body's aim line this far out.")]
        public float convergeDistance = 14f;

        // The hand is off to one side of the body. A shot fired straight would always land that far to the side of
        // what is aimed at, so it is turned in a little to pass through the aimed-at point.
        Vector3 Converge(Vector3 origin, Vector3 aimDirection, float aimDistance)
        {
            Vector3 target = transform.position + aimDirection * (aimDistance > 2f ? aimDistance : convergeDistance);
            Vector3 to = target - origin;
            to.y = 0f;
            return to.sqrMagnitude > 0.01f ? to.normalized : aimDirection;
        }

        /// <summary>
        /// Up and down: the aim is only ever left and right, so a shot at something on a ledge above (or below) is
        /// tilted to meet it. Only for a target on another level: on the flat, shots fly flat as they always have.
        /// </summary>
        Vector3 Pitch(Vector3 origin, Vector3 flat, float coneAngle)
        {
            Collider best = null;
            float bestAngle = Mathf.Max(coneAngle, 12f);
            float bestDistance = 0f;
            foreach (var enemy in Enemy.Active)
            {
                if (enemy.Health.IsDead || !enemy.Targetable) continue;
                if (Mathf.Abs(enemy.transform.position.y - transform.position.y) < 0.8f) continue;
                Vector3 to = enemy.transform.position - origin;
                to.y = 0f;
                float distance = to.magnitude;
                if (distance > Reach || distance < 0.5f) continue;
                float angle = Vector3.Angle(flat, to);
                if (angle >= bestAngle) continue;
                var body = enemy.GetComponent<Collider>();
                if (body == null) continue;
                bestAngle = angle;
                best = body;
                bestDistance = distance;
            }
            if (best == null) return flat;
            float rise = best.bounds.center.y - origin.y;
            Vector3 level = flat;
            level.y = 0f;
            return (level.normalized * bestDistance + Vector3.up * rise).normalized;
        }

        Vector3 ApplyAimAssist(Vector3 origin, Vector3 direction, float coneAngle)
        {
            if (aimAssistStrength <= 0f) return direction;
            return !FindTarget(origin, direction, coneAngle, out Vector3 toTarget)
                ? direction
                : Vector3.Slerp(direction, toTarget, aimAssistStrength).normalized;
        }

        // Whatever is closest to the centre of the aim cone, if anything: an enemy, or a target on the range.
        bool FindTarget(Vector3 origin, Vector3 direction, float coneAngle, out Vector3 bestDirection)
        {
            bool found = false;
            float bestAngle = coneAngle;
            Vector3 best = direction;

            foreach (var enemy in Enemy.Active)
            {
                if (enemy.Health.IsDead || !enemy.Targetable) continue;
                Consider(enemy.transform.position);
            }
            foreach (var target in RangeTarget.All)
            {
                if (target.Aimable) Consider(target.transform.position);
            }
            bestDirection = best;
            return found;

            void Consider(Vector3 position)
            {
                Vector3 to = position - origin;
                to.y = 0f;
                float distance = to.magnitude;
                if (distance > Reach || distance < 0.01f) return;
                float angle = Vector3.Angle(direction, to);
                if (angle >= bestAngle) return;
                bestAngle = angle;
                found = true;
                best = to / distance;
            }
        }
    }
}
