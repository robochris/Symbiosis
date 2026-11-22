using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Everything a shot carries. Items add to these numbers, so effects stack and combine on their own:
    /// a piercing shot that also splits and carries acid does all three.
    /// </summary>
    public class Shot
    {
        public int damage = 1;
        /// <summary>How many more enemies it can pass through.</summary>
        public int pierce;
        /// <summary>Stacks of split: on hitting an enemy it throws off 3 smaller shots (5 with two stacks, and so on).</summary>
        public int split;
        /// <summary>Leaves an acid puddle where it lands.</summary>
        public int acid;
        /// <summary>Sets what it hits on fire.</summary>
        public int burn;
        /// <summary>Chills what it hits, slowing it for a while.</summary>
        public int chill;
        /// <summary>How many more times it comes back off a wall instead of stopping there.</summary>
        public int bounce;
        public bool crit;
        /// <summary>Split-off shots keep every effect except splitting again.</summary>
        public bool isChild;
        /// <summary>How big it looks and how long it is drawn: the Sprinkler's are small, the Syringe's long.</summary>
        public float size = 1f, length = 1f;
        /// <summary>Who fired it; it never hits them.</summary>
        public Health owner;
        /// <summary>Magnifying Glass: this much more damage once the shot has flown far.</summary>
        public int farBonus;
        /// <summary>Yo-yo: how many more times it turns round at the end of its flight.</summary>
        public int returns;
        /// <summary>Bottled Lightning: where this one hits, lightning comes down.</summary>
        public bool lightning;

        public Shot Child() => new Shot
        {
            damage = damage, pierce = pierce, split = 0, acid = acid, burn = burn, chill = chill, bounce = bounce, crit = crit, isChild = true, owner = owner, farBonus = farBonus
        };
    }

    /// <summary>A pooled shot. Moves in a straight line and sweeps a sphere each frame so fast shots can't skip through thin targets.</summary>
    public class Bullet : MonoBehaviour
    {
        /// <summary>How far a shot has to fly before the Magnifying Glass's bonus counts.</summary>
        public const float FarDistance = 7f;
        /// <summary>Times any shot has turned round on a yo-yo, for tests.</summary>
        public static int Returns;
        /// <summary>Bottled Lightning: how far round the hit it reaches, and how hard.</summary>
        public const float LightningRadius = 2.5f;
        public const int LightningDamage = 2;
        /// <summary>Times lightning has come down, for tests.</summary>
        public static int Strikes;
        public float radius = 0.15f;

        static readonly RaycastHit[] Hits = new RaycastHit[16];

        readonly List<Health> _alreadyHit = new List<Health>();
        Shot _shot;
        public Shot Shot => _shot;
        Vector3 _direction;
        float _speed;
        float _range;
        float _travelled;
        Team _team;
        /// <summary>Whose shot it is.</summary>
        public Team Team => _team;
        Pool<Bullet> _pool;

        public void Launch(Vector3 position, Vector3 direction, float speed, Shot shot, float range, Team team, Pool<Bullet> pool, Health alreadyHit = null)
        {
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            float size = (shot.isChild ? 0.7f : 1f) * shot.size;
            transform.localScale = new Vector3(size, size, size * shot.length);
            _direction = direction;
            _speed = speed;
            _shot = shot;
            _range = range;
            _team = team;
            _pool = pool;
            _travelled = 0f;
            _alreadyHit.Clear();
            if (alreadyHit != null) _alreadyHit.Add(alreadyHit);
            gameObject.SetActive(true);
        }

        void Update()
        {
            float step = _speed * (_team == Team.Enemy ? Enemy.WorldSlow : 1f) * Time.deltaTime;
            int count = Physics.SphereCastNonAlloc(transform.position, radius, _direction, Hits, step, ~0, QueryTriggerInteraction.Ignore);

            bool hitSomething = false;
            float nearest = float.MaxValue;
            Health nearestHealth = null;
            Vector3 nearestPoint = transform.position;
            Vector3 nearestNormal = -_direction;

            for (int i = 0; i < count; i++)
            {
                var health = Hits[i].collider.GetComponentInParent<Health>();
                if (health != null)
                {
                    // Pass through friends, corpses, the shooter, and anything this shot has already been through.
                    if (health.team == _team || health.IsDead || health == _shot.owner || _alreadyHit.Contains(health)) continue;
                }
                if (Hits[i].distance < nearest)
                {
                    nearest = Hits[i].distance;
                    nearestHealth = health;
                    // A cast that starts inside something reports distance 0 and no point.
                    nearestPoint = Hits[i].distance > 0f ? Hits[i].point : transform.position;
                    nearestNormal = Hits[i].distance > 0f ? Hits[i].normal : -_direction;
                    hitSomething = true;
                }
            }

            if (hitSomething)
            {
                Fx.Burst(nearestPoint, Colour, nearestHealth != null ? 5 : 3, 3f, 0.12f, 0.3f);
                bool keepFlying = nearestHealth != null && HitCharacter(nearestHealth, nearestPoint);
                if (nearestHealth == null && _shot.acid > 0) LeavePuddle(nearestPoint);

                // Rubber Band: off the wall and back into the room, flat along the floor.
                if (nearestHealth == null && _shot.bounce > 0)
                {
                    Vector3 back = Vector3.Reflect(_direction, nearestNormal);
                    back.y = 0f;
                    if (back.sqrMagnitude > 0.01f)
                    {
                        _shot.bounce--;
                        Bounces++;
                        _direction = back.normalized;
                        transform.SetPositionAndRotation(new Vector3(nearestPoint.x, transform.position.y, nearestPoint.z) + _direction * (radius + 0.05f), Quaternion.LookRotation(_direction));
                        _alreadyHit.Clear();
                        return;
                    }
                }

                // Yo-yo: a wall sends it back too, the way it came.
                if (nearestHealth == null && _shot.returns > 0)
                {
                    _shot.returns--;
                    Returns++;
                    _direction = -_direction;
                    transform.SetPositionAndRotation(new Vector3(nearestPoint.x, transform.position.y, nearestPoint.z) + _direction * (radius + 0.05f), Quaternion.LookRotation(_direction));
                    _travelled = 0f;
                    _alreadyHit.Clear();
                    return;
                }

                if (!keepFlying)
                {
                    Despawn();
                    return;
                }
            }

            transform.position += _direction * step;
            _travelled += step;
            if (_travelled >= _range)
            {
                // Yo-yo: back the way it came, as far again, through everything it passed the first time.
                if (_shot.returns > 0)
                {
                    _shot.returns--;
                    _direction = -_direction;
                    transform.rotation = Quaternion.LookRotation(_direction);
                    _travelled = 0f;
                    _alreadyHit.Clear();
                    Returns++;
                    return;
                }
                Despawn();
            }
        }

        // Order of effects: damage, then element, then split, then pierce. Returns true if the shot carries on.
        bool HitCharacter(Health health, Vector3 point)
        {
            int far = _shot.farBonus > 0 && _travelled >= FarDistance ? _shot.farBonus : 0;
            // From behind: one more. (The player's shots only.)
            int behind = 0;
            if (_team == Team.Player)
            {
                var target = health.GetComponent<Enemy>();
                if (target != null && Vector3.Dot(target.transform.forward, _direction) > 0.5f) { behind = 1; Backstabs++; }
            }
            health.TakeDamage((_shot.damage + far + behind) * (_shot.crit ? 2 : 1), _direction);
            if (_shot.crit && _team == Team.Player) Fx.Burst(point + Vector3.up * 0.5f, new Color(1f, 0.95f, 0.4f), 12, 5f, 0.1f, 0.3f);
            if (_shot.lightning)
            {
                // Down it comes, on whatever was hit and everything round it.
                _shot.lightning = false;
                Strikes++;
                Fx.Burst(point + Vector3.up * 2f, new Color(1f, 1f, 0.55f), 30, 10f, 0.2f, 0.4f);
                Sfx.Play(SfxId.Blast, 0.7f);
                foreach (var enemy in Enemy.Active.ToArray())
                {
                    if (enemy.Health.IsDead || enemy.Health == health) continue;
                    Vector3 to = enemy.transform.position - point;
                    to.y = 0f;
                    if (to.magnitude <= LightningRadius) enemy.Health.TakeDamage(LightningDamage, to.sqrMagnitude > 0.01f ? to.normalized : _direction);
                }
                health.TakeDamage(LightningDamage, _direction);
            }

            if (_shot.burn > 0)
            {
                var enemy = health.GetComponent<Enemy>();
                if (enemy != null) enemy.Ignite(_shot.burn);
            }
            if (_shot.chill > 0)
            {
                var enemy = health.GetComponent<Enemy>();
                if (enemy != null) enemy.Chill(1.5f + 0.75f * _shot.chill);
            }
            if (_shot.acid > 0) LeavePuddle(point);

            if (_shot.split > 0 && !_shot.isChild && _pool != null)
            {
                int children = 1 + 2 * _shot.split;
                float spread = 70f + 20f * (_shot.split - 1);
                for (int i = 0; i < children; i++)
                {
                    float angle = -spread * 0.5f + spread * i / (children - 1);
                    Vector3 direction = Quaternion.Euler(0f, angle, 0f) * _direction;
                    _pool.Get().Launch(transform.position, direction, _speed, _shot.Child(), 9f, _team, _pool, health);
                }
            }

            if (_shot.pierce <= 0) return false;
            _shot.pierce--;
            _alreadyHit.Add(health);
            return true;
        }

        Color? _colour;
        Color Colour => _colour ?? (_colour = Fx.ColourOf(this)).Value;

        void LeavePuddle(Vector3 point)
        {
            var assets = GameAssets.Instance;
            if (assets == null) return;
            bool burning = _shot.burn > 0;
            GroundHazard.SpawnFriendlyPuddle(point, _shot.acid, burning, burning ? assets.fire : assets.playerAcid);
        }

        void OnEnable() => Live.Add(this);
        void OnDisable() => Live.Remove(this);

        /// <summary>How many times shots have come back off a wall, for tests.</summary>
        public static int Bounces;

        /// <summary>Every shot in the air.</summary>
        public static readonly List<Bullet> Live = new List<Bullet>();
        /// <summary>Hits from behind, for tests.</summary>
        public static int Backstabs;

        /// <summary>A parry: the shot turns round and is the player's now.</summary>
        public void Reflect()
        {
            _team = Team.Player;
            _direction = -_direction;
            _travelled = 0f;
            _alreadyHit.Clear();
            if (_shot != null) _shot.owner = null;
            transform.rotation = Quaternion.LookRotation(_direction);
        }

        /// <summary>Takes every enemy shot out of the air. Returns how many there were.</summary>
        public static int ClearEnemyShots()
        {
            int cleared = 0;
            foreach (var bullet in Live.ToArray())
            {
                if (bullet._team != Team.Enemy) continue;
                Fx.Burst(bullet.transform.position, bullet.Colour, 3, 3f, 0.12f, 0.3f);
                bullet.Despawn();
                cleared++;
            }
            return cleared;
        }

        void Despawn()
        {
            if (_pool != null) _pool.Release(this);
            else gameObject.SetActive(false);
        }
    }
}
