using System;
using UnityEngine;

namespace Shrunk
{
    /// <summary>Which active ability the Void carries. Chosen at the rack in the lab.</summary>
    public enum PowerKind { Overcharge, FreezeFrame, SecondWind, BadMemo, Decoy, Hop, Stomp, Hoover, Tiny }

    /// <summary>What the lab's rack says about a power, and what it takes to be issued one.</summary>
    public class PowerInfo
    {
        public PowerKind kind;
        public string name;
        public string does;
        /// <summary>How to get it, as the player reads it. Empty for the one everybody starts with.</summary>
        public string needs;
        public float killsToFill;
        public float duration;
    }

    /// <summary>
    /// The active ability: fill the meter by killing, then set it off. Three to choose from, one carried at a time.
    /// Overcharge is attack: a surge that hurts and throws back everything close, then double damage, piercing,
    /// faster shots and faster feet. Freeze Frame is control: everything that is not you, and every shot that is not
    /// yours, crawls. Second Wind is defence: a point of health back, every enemy shot swept out of the air, and a
    /// few seconds in which nothing can hurt you.
    /// </summary>
    public class HeroPower : MonoBehaviour
    {
        public static readonly PowerInfo[] All =
        {
            new PowerInfo { kind = PowerKind.Overcharge, name = "Overcharge", does = "A surge that throws everything back, then double damage and piercing shots for 15 seconds.", needs = "", killsToFill = 25f, duration = 15f },
            new PowerInfo { kind = PowerKind.FreezeFrame, name = "Freeze Frame", does = "For 6 seconds every enemy and every enemy shot crawls. You don't.", needs = "Issued after your first extraction.", killsToFill = 20f, duration = 6f },
            new PowerInfo { kind = PowerKind.SecondWind, name = "Second Wind", does = "One health back, every enemy shot swept away, and 4 seconds in which nothing can hurt you.", needs = "Issued to interns who have fallen three times.", killsToFill = 18f, duration = 4f },
            new PowerInfo { kind = PowerKind.BadMemo, name = "Bad Memo", does = "A memo goes round. For 6 seconds everything in the room that is not a boss turns on its neighbours.", needs = "Issued to interns who have done three jobs off the noticeboard.", killsToFill = 22f, duration = 6f },
            new PowerInfo { kind = PowerKind.Decoy, name = "Decoy", does = "A cardboard you, left where you stand. For 7 seconds everything goes for it instead, bosses included, until it falls over.", needs = "Issued to interns who keep a Daily Experiment streak of 3 days.", killsToFill = 20f, duration = 7f },
            new PowerInfo { kind = PowerKind.Hop, name = "Hop", does = "Straight to where you are aiming, up to seven paces, and a second in which nothing can touch you. Fills fast.", needs = "Issued to interns who have found a secret room.", killsToFill = 8f, duration = 1f },
            new PowerInfo { kind = PowerKind.Stomp, name = "Stomp", does = "A stamp that knocks everything in a wide ring flat: three damage, and thrown well back.", needs = "Issued to interns who have beaten five bosses.", killsToFill = 18f, duration = 0.5f },
            new PowerInfo { kind = PowerKind.Hoover, name = "Hoover", does = "Everything in front of you is pulled into one heap, five paces ahead. Then make the most of it.", needs = "Issued once the Robot Vacuum is in a jar.", killsToFill = 16f, duration = 0.6f },
            new PowerInfo { kind = PowerKind.Tiny, name = "Tiny", does = "Smaller still for 8 seconds: much faster on your feet, and the dodge comes back quicker.", needs = "Issued to interns who have reached floor 10.", killsToFill = 18f, duration = 8f },
        };

        public const float FreezeSlow = 0.3f;

        public static PowerInfo Info(PowerKind kind)
        {
            foreach (var info in All) if (info.kind == kind) return info;
            return All[0];
        }

        /// <summary>Whether the lab has issued this one yet.</summary>
        public static bool Unlocked(PowerKind kind)
        {
            if (Achievements.EverythingUnlocked) return true;
            MetaSave meta = SaveSystem.Data.meta;
            switch (kind)
            {
                case PowerKind.FreezeFrame: return meta.extractions >= 1;
                case PowerKind.SecondWind: return meta.deaths >= 3;
                case PowerKind.BadMemo: return meta.jobsDone >= 3;
                case PowerKind.Decoy: return meta.dailyBestStreak >= 3;
                case PowerKind.Hop: return meta.secretsFound >= 1;
                case PowerKind.Stomp: return meta.bossesBeaten >= 5;
                case PowerKind.Hoover: return Specimens.Has("RobotVacuum");
                case PowerKind.Tiny: return meta.deepestFloor >= 10;
                default: return true;
            }
        }

        /// <summary>The one the save file says to carry, or Overcharge if that one has not been issued.</summary>
        public static PowerKind Chosen
        {
            get
            {
                var kind = (PowerKind)Mathf.Clamp(SaveSystem.Data.meta.power, 0, All.Length - 1);
                return Unlocked(kind) ? kind : PowerKind.Overcharge;
            }
        }

        /// <summary>Which one this is. Set from the save when the scene starts.</summary>
        public PowerKind Kind { get; private set; } = PowerKind.Overcharge;
        /// <summary>True while Overcharge itself is running: the one that changes what a shot does.</summary>
        public bool IsOvercharged => IsActive && Kind == PowerKind.Overcharge;
        public bool IsTiny => IsActive && Kind == PowerKind.Tiny;
        public bool IsFreezing => IsActive && Kind == PowerKind.FreezeFrame;

        /// <summary>Carries a different power. The meter is kept as a share, so swapping in the lab loses nothing.</summary>
        public void SetKind(PowerKind kind)
        {
            if (IsActive) End();
            Kind = kind;
            PowerInfo info = Info(kind);
            displayName = info.name;
            killsToFill = info.killsToFill;
            duration = info.duration;
        }

        void Start() => SetKind(Chosen);

        // Whatever the power changed about the world is put back.
        void End()
        {
            _activeUntil = 0f;
            if (_slowing)
            {
                _slowing = false;
                Enemy.WorldSlow = 1f;
            }
        }

        bool _slowing;

        public string displayName = "Overcharge";
        [Tooltip("Kills needed to fill the meter from empty.")]
        public float killsToFill = 25f;
        [Tooltip("Seconds it lasts.")]
        public float duration = 15f;
        [Tooltip("Lab upgrade: 1.2 = fills 20% faster.")]
        public float fillMultiplier = 1f;
        [Tooltip("The eyes; they grow while the power is on.")]
        public Transform[] eyes;

        [Header("The surge when it is set off")]
        [Tooltip("Enemies this close are hit and thrown back.")]
        public float surgeRadius = 7f;
        public int surgeDamage = 2;
        public Material surgeMaterial;

        [Header("While it lasts")]
        [Tooltip("1.2 = walks 20% faster.")]
        public float moveSpeedMultiplier = 1.2f;
        [Tooltip("Degrees the view opens up by.")]
        public float extraFieldOfView = 10f;

        /// <summary>0 = empty, 1 = ready.</summary>
        public float Meter { get; private set; }
        public bool IsReady => Meter >= 1f && !IsActive;
        public bool IsActive => Time.time < _activeUntil;
        /// <summary>While active: 1 just after it starts, falling to 0 as it runs out.</summary>
        public float Remaining01 => IsActive ? (_activeUntil - Time.time) / duration : 0f;
        public event Action Activated;

        /// <summary>Enemies caught by the last surge.</summary>
        public int LastSurgeHits { get; private set; }
        /// <summary>How hard the surge throws what it catches: well past a shot's knockback.</summary>
        public const float SurgeShove = 8f;

        float _activeUntil;
        Vector3[] _eyeScales;
        Camera _camera;
        float _plainFieldOfView;
        Transform _ring;
        float _ringAge;

        void Awake()
        {
            _eyeScales = new Vector3[eyes != null ? eyes.Length : 0];
            for (int i = 0; i < _eyeScales.Length; i++) _eyeScales[i] = eyes[i].localScale;
        }

        void OnEnable()
        {
            Enemy.Killed += OnKilled;
        }

        void OnDisable()
        {
            Enemy.Killed -= OnKilled;
            End();
        }

        void OnKilled(Enemy enemy)
        {
            // The meter doesn't fill while the power is running.
            if (IsActive) return;
            var items = GetComponent<PlayerItems>();
            AddCharge(1f / killsToFill * (items != null ? items.Stats.powerFillMultiplier : 1f));
        }

        public void AddCharge(float amount)
        {
            Meter = Mathf.Clamp01(Meter + amount * fillMultiplier);
        }

        public void SetMeter(float value)
        {
            Meter = Mathf.Clamp01(value);
        }

        public bool TryActivate()
        {
            if (!IsReady) return false;
            Meter = 0f;
            _activeUntil = Time.time + duration;
            Activated?.Invoke();
            Sfx.Play(SfxId.PowerOn);

            switch (Kind)
            {
                case PowerKind.FreezeFrame:
                    _slowing = true;
                    Enemy.WorldSlow = FreezeSlow;
                    if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.25f);
                    Fx.Burst(transform.position + Vector3.up * 0.9f, new Color(0.6f, 0.85f, 1f), 40, 12f, 0.2f, 0.7f);
                    break;

                case PowerKind.SecondWind:
                    var health = GetComponent<Health>();
                    if (health != null)
                    {
                        health.Heal(1);
                        health.SetInvulnerable(duration);
                    }
                    LastShotsCleared = Bullet.ClearEnemyShots();
                    Surge();
                    break;

                case PowerKind.Decoy:
                    Decoy.Place(transform.position, transform.rotation, duration);
                    Fx.Burst(transform.position + Vector3.up * 0.9f, new Color(0.85f, 0.7f, 0.45f), 24, 6f, 0.2f, 0.5f);
                    break;

                case PowerKind.Hop:
                {
                    var player = GetComponent<PlayerController>();
                    // The way the player faces, which is the way they aim.
                    Vector3 aim = transform.forward;
                    aim.y = 0f;
                    Vector3 to = transform.position + aim.normalized * 7f;
                    // As far as the floor goes, and not through a wall.
                    if (Physics.Raycast(transform.position + Vector3.up * 0.8f, aim.normalized, out var wall, 7f, ~0, QueryTriggerInteraction.Ignore))
                        to = transform.position + aim.normalized * Mathf.Max(0f, wall.distance - 0.6f);
                    if (UnityEngine.AI.NavMesh.SamplePosition(to, out var land, 2f, UnityEngine.AI.NavMesh.AllAreas)) to = land.position;
                    Fx.Burst(transform.position + Vector3.up * 0.8f, new Color(0.5f, 0.9f, 1f), 18, 5f, 0.15f, 0.35f);
                    if (player != null) player.Teleport(to + Vector3.up * 0.05f);
                    GetComponent<Health>()?.SetInvulnerable(duration);
                    Fx.Burst(to + Vector3.up * 0.8f, new Color(0.5f, 0.9f, 1f), 18, 5f, 0.15f, 0.35f);
                    break;
                }

                case PowerKind.Stomp:
                    LastSurgeHits = 0;
                    foreach (var enemy in Enemy.Active.ToArray())
                    {
                        if (enemy.Health.IsDead || !enemy.Targetable) continue;
                        Vector3 away = enemy.transform.position - transform.position;
                        away.y = 0f;
                        if (away.magnitude > surgeRadius * 1.8f) continue;
                        Vector3 outward = away.sqrMagnitude > 0.01f ? away.normalized : transform.forward;
                        enemy.Health.TakeDamage(3, outward);
                        enemy.Shove(outward * SurgeShove * 1.5f);
                        LastSurgeHits++;
                    }
                    if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.7f);
                    Fx.Burst(transform.position + Vector3.up * 0.2f, new Color(0.85f, 0.75f, 0.55f), 50, 14f, 0.25f, 0.6f);
                    break;

                case PowerKind.Hoover:
                {
                    // Everything in front, within a wide cone, is dragged to one spot ahead.
                    Vector3 heap = transform.position + transform.forward * 5f;
                    LastSurgeHits = 0;
                    foreach (var enemy in Enemy.Active.ToArray())
                    {
                        if (enemy.Health.IsDead || !enemy.Targetable || enemy is BossEnemy) continue;
                        Vector3 to = enemy.transform.position - transform.position;
                        to.y = 0f;
                        if (to.magnitude > 16f || Vector3.Angle(transform.forward, to) > 70f) continue;
                        Vector3 pull = heap - enemy.transform.position;
                        pull.y = 0f;
                        enemy.Shove(pull * 2.2f);
                        LastSurgeHits++;
                    }
                    Fx.Burst(heap + Vector3.up * 0.6f, new Color(0.7f, 0.7f, 0.75f), 30, 3f, 0.2f, 0.6f);
                    Sfx.Play(SfxId.Dodge, 1f);
                    break;
                }

                case PowerKind.Tiny:
                    Fx.Burst(transform.position + Vector3.up * 0.6f, new Color(0.9f, 0.6f, 1f), 24, 6f, 0.15f, 0.4f);
                    break;

                case PowerKind.BadMemo:
                    // Everybody but the boss reads it, and takes it out on whoever is nearest.
                    LastMemoRead = 0;
                    foreach (var enemy in Enemy.Active.ToArray())
                    {
                        if (enemy.Health.IsDead || enemy is BossEnemy) continue;
                        enemy.Confuse(duration);
                        LastMemoRead++;
                    }
                    if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.2f);
                    Fx.Burst(transform.position + Vector3.up * 0.9f, new Color(1f, 0.96f, 0.8f), 40, 10f, 0.2f, 0.7f);
                    break;

                default:
                    Surge();
                    break;
            }
            return true;
        }

        /// <summary>How many read the last Bad Memo.</summary>
        public int LastMemoRead { get; private set; }

        /// <summary>Enemy shots the last Second Wind swept away.</summary>
        public int LastShotsCleared { get; private set; }

        // Everything close is hit and thrown straight back, bosses included (they just don't move).
        void Surge()
        {
            LastSurgeHits = 0;
            foreach (var enemy in Enemy.Active.ToArray())
            {
                if (enemy.Health.IsDead || !enemy.Targetable) continue;
                Vector3 away = enemy.transform.position - transform.position;
                away.y = 0f;
                if (away.magnitude > surgeRadius) continue;
                Vector3 outward = away.sqrMagnitude > 0.01f ? away.normalized : transform.forward;
                enemy.Health.TakeDamage(surgeDamage, outward);
                enemy.Shove(outward * SurgeShove);
                LastSurgeHits++;
            }
            if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.45f);
            Fx.Burst(transform.position + Vector3.up * 0.9f, surgeMaterial != null ? surgeMaterial.GetColor("_BaseColor") : Color.cyan, 36, 9f, 0.2f, 0.5f);

            if (_ring == null)
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "Overcharge surge";
                var collider = ring.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
                if (surgeMaterial != null) ring.GetComponent<Renderer>().sharedMaterial = surgeMaterial;
                _ring = ring.transform;
            }
            _ring.position = new Vector3(transform.position.x, 0.06f, transform.position.z);
            _ring.gameObject.SetActive(true);
            _ringAge = 0f;
        }

        void OnDestroy()
        {
            if (_ring != null) Destroy(_ring.gameObject);
            if (_camera != null && _plainFieldOfView > 0f) _camera.fieldOfView = _plainFieldOfView;
        }

        void Update()
        {
            if (_slowing && !IsActive) End();

            // The surge: a disc that races out to its full reach and is gone.
            if (_ring != null && _ring.gameObject.activeSelf)
            {
                _ringAge += Time.deltaTime;
                float size = Mathf.Lerp(1f, surgeRadius * 2f, _ringAge / 0.25f);
                _ring.localScale = new Vector3(size, 0.01f, size);
                if (_ringAge >= 0.3f) _ring.gameObject.SetActive(false);
            }

            // The view opens up while the power runs, and eases back after.
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera != null) _plainFieldOfView = _camera.fieldOfView;
            }
            if (_camera != null)
            {
                float wanted = _plainFieldOfView + (IsActive ? extraFieldOfView : 0f);
                _camera.fieldOfView = Mathf.MoveTowards(_camera.fieldOfView, wanted, 40f * Time.deltaTime);
            }

            if (_eyeScales == null) return;
            float blaze = IsActive ? 1.9f + 0.3f * Mathf.Sin(Time.time * 18f) : 1f;
            for (int i = 0; i < _eyeScales.Length; i++)
            {
                if (eyes[i] != null) eyes[i].localScale = _eyeScales[i] * blaze;
            }
        }
    }
}
